using UnityEngine;
using System;
using System.Collections.Generic;

public enum DayPhase { Morning, Night }

[Serializable]
public class PhaseObject
{
    public GameObject obj;
    [Tooltip("Se true, lo stato impostato viene memorizzato e rimane anche nelle fasi/giorni successivi finché non sovrascritto.")]
    public bool persist = false;
}

[Serializable]
public class PhaseSet
{
    public List<PhaseObject> enable = new List<PhaseObject>();
    public List<PhaseObject> disable = new List<PhaseObject>();
}

[Serializable]
public class DayConfig
{
    [Tooltip("Se true, questo giorno ha anche la fase Notte.")]
    public bool hasNight = true;

    [Header("Morning")]
    public PhaseSet morning = new PhaseSet();

    [Header("Night")]
    public PhaseSet night = new PhaseSet();
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Stato corrente (runtime)")]
    public int CurrentDay { get; private set; } = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Morning;

    [Header("Config iniziale")]
    [SerializeField] private int startDay = 1;
    [SerializeField] private DayPhase startPhase = DayPhase.Morning;

    [Header("Campagna di 7 giorni")]
    [SerializeField] private DayConfig[] days = new DayConfig[7];
    public const int MaxDays = 7;

    [Header("Eventi GLOBALI sempre attivi")]
    [Tooltip("Applicati ogni mattina, prima degli eventi del giorno.")]
    public PhaseSet globalMorningAlways = new PhaseSet();
    [Tooltip("Applicati ogni notte, prima degli eventi del giorno.")]
    public PhaseSet globalNightAlways = new PhaseSet();

    public event Action<int, DayPhase> OnPhaseChanged;

    // ---- Persistenza & baseline ----
    // Stato persistente per oggetto
    private readonly Dictionary<GameObject, bool> _persistentOverride = new Dictionary<GameObject, bool>();
    // Stato iniziale (snapshot) per oggetto
    private readonly Dictionary<GameObject, bool> _initialActive = new Dictionary<GameObject, bool>();
    // Oggetti toccati non-persistentemente nella fase precedente
    private readonly HashSet<GameObject> _lastNonPersistentTouched = new HashSet<GameObject>();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDaysArray();
        startDay = Mathf.Clamp(startDay, 1, MaxDays);
    }

    void Start()
    {
        CurrentDay = startDay;
        CurrentPhase = startPhase;
        Debug.Log($"[GameStateManager] Avvio: Giorno {CurrentDay}, {CurrentPhase}");
        TriggerPhaseEvents();
    }

    // =========================================================
    // Avanzamento fasi
    // =========================================================
    public void AdvancePhase()
    {
        Debug.Log($"[GameStateManager] AdvancePhase (giorno {CurrentDay}, fase {CurrentPhase})");

        if (CurrentPhase == DayPhase.Morning)
        {
            var cfg = GetDayConfig(CurrentDay);
            if (cfg != null && cfg.hasNight)
            {
                CurrentPhase = DayPhase.Night;
                Debug.Log($"🌙 Giorno {CurrentDay} → Notte");
            }
            else
            {
                CurrentDay = Mathf.Min(CurrentDay + 1, MaxDays);
                CurrentPhase = DayPhase.Morning;
                Debug.Log($"🌞 Giorno {CurrentDay} → Mattina");
            }
        }
        else // Night
        {
            CurrentDay = Mathf.Min(CurrentDay + 1, MaxDays);
            CurrentPhase = DayPhase.Morning;
            Debug.Log($"🌞 Giorno {CurrentDay} → Mattina (dopo la notte)");
        }

        TriggerPhaseEvents();
    }

    // =========================================================
    // Applicazione eventi + persistenza + baseline corretto
    // =========================================================
    private void TriggerPhaseEvents()
    {
        Debug.Log($"[GameStateManager] Trigger: Giorno {CurrentDay}, {CurrentPhase}");

        // 0) Ripristina gli oggetti toccati non-persist nella fase precedente al baseline corretto
        RestoreNonPersistentToBaseline();

        // 1) Applica gli override persistenti (baseline “memorizzato”)
        ApplyPersistentOverridesCleanupNulls();

        // 2) Applica GLOBALI della fase corrente (prima non-persist, poi persist)
        var globalSet = (CurrentPhase == DayPhase.Morning) ? globalMorningAlways : globalNightAlways;
        ApplyPhaseSet(globalSet, persistOnly: false); // non-persist
        ApplyPhaseSet(globalSet, persistOnly: true);  // persist

        // 3) Applica EVENTI DEL GIORNO (prima non-persist, poi persist)
        var cfg = GetDayConfig(CurrentDay);
        if (cfg != null)
        {
            var set = (CurrentPhase == DayPhase.Morning) ? cfg.morning : cfg.night;
            ApplyPhaseSet(set, persistOnly: false); // non-persist
            ApplyPhaseSet(set, persistOnly: true);  // persist
        }

        // 4) Notifica UI/Adapter
        OnPhaseChanged?.Invoke(CurrentDay, CurrentPhase);

        // 5) Logica globale del mattino
        if (CurrentPhase == DayPhase.Morning)
        {
            TimerManager.Instance?.ResetToIdle();

            var panels = FindObjectsByType<BulletinController>(FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
                panels[i]?.RefreshNow();
        }
    }

    private void ApplyPhaseSet(PhaseSet set, bool persistOnly)
    {
        if (set == null) return;

        // ENABLE
        if (set.enable != null)
        {
            for (int i = 0; i < set.enable.Count; i++)
            {
                var p = set.enable[i];
                if (p == null || p.obj == null) continue;
                if (p.persist != persistOnly) continue;

                CaptureInitialIfNeeded(p.obj);
                SafeSetActive(p.obj, true);

                if (p.persist)
                    _persistentOverride[p.obj] = true;
                else
                    _lastNonPersistentTouched.Add(p.obj);
            }
        }

        // DISABLE
        if (set.disable != null)
        {
            for (int i = 0; i < set.disable.Count; i++)
            {
                var p = set.disable[i];
                if (p == null || p.obj == null) continue;
                if (p.persist != persistOnly) continue;

                CaptureInitialIfNeeded(p.obj);
                SafeSetActive(p.obj, false);

                if (p.persist)
                    _persistentOverride[p.obj] = false;
                else
                    _lastNonPersistentTouched.Add(p.obj);
            }
        }
    }

    // Ripristina tutti gli oggetti toccati NON-persist nella fase precedente
    // al baseline: override persistente se esiste, altrimenti stato iniziale.
    private void RestoreNonPersistentToBaseline()
    {
        if (_lastNonPersistentTouched.Count == 0) return;

        foreach (var go in _lastNonPersistentTouched)
        {
            if (!go) continue;

            bool baseline;
            if (_persistentOverride.TryGetValue(go, out var persisted))
            {
                baseline = persisted;
            }
            else if (_initialActive.TryGetValue(go, out var initActive))
            {
                baseline = initActive;
            }
            else
            {
                // se non conosciamo lo stato iniziale, catturalo ora
                _initialActive[go] = go.activeSelf;
                baseline = go.activeSelf;
            }

            SafeSetActive(go, baseline);
        }

        _lastNonPersistentTouched.Clear();
    }

    private void ApplyPersistentOverridesCleanupNulls()
    {
        if (_persistentOverride.Count == 0) return;

        var toRemove = ListPool<GameObject>.Get();
        foreach (var kvp in _persistentOverride)
        {
            var go = kvp.Key;
            if (go == null) { toRemove.Add(go); continue; }
            SafeSetActive(go, kvp.Value);
        }
        for (int i = 0; i < toRemove.Count; i++)
            _persistentOverride.Remove(toRemove[i]);
        ListPool<GameObject>.Release(toRemove);
    }

    private void CaptureInitialIfNeeded(GameObject go)
    {
        if (!go) return;
        if (!_initialActive.ContainsKey(go))
            _initialActive[go] = go.activeSelf;
    }

    private static void SafeSetActive(GameObject go, bool active)
    {
        if (!go) return;
        if (go.activeSelf != active) go.SetActive(active);
    }

    // =========================================================
    // Helpers
    // =========================================================
    private void EnsureDaysArray()
    {
        if (days == null || days.Length != MaxDays)
        {
            var old = days;
            days = new DayConfig[MaxDays];
            for (int i = 0; i < MaxDays; i++)
                days[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new DayConfig();
        }
    }

    private DayConfig GetDayConfig(int day)
    {
        if (day < 1 || day > MaxDays) return null;
        return days[day - 1];
    }

    // Reset totale (facoltativo)
    public void ResetCampaign(int day = 1, DayPhase phase = DayPhase.Morning)
    {
        _persistentOverride.Clear();
        _initialActive.Clear();
        _lastNonPersistentTouched.Clear();

        CurrentDay = Mathf.Clamp(day, 1, MaxDays);
        CurrentPhase = phase;
        TriggerPhaseEvents();
    }
}

// -------- ListPool utility (evita allocazioni temporanee) ----------
static class ListPool<T>
{
    static readonly Stack<List<T>> pool = new Stack<List<T>>();
    public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>();
    public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
}
