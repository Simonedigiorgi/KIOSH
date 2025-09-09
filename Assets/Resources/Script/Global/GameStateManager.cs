using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public enum DayPhase { Morning, Night }

// ----------------- DATA -----------------

[Serializable]
public class ToggleEvent
{
    [Tooltip("Oggetto da attivare/disattivare quando l'evento si applica")]
    public GameObject target;

    [Tooltip("Stato desiderato quando l'evento si applica")]
    public bool setActive = true;

    [Tooltip("Se true, lo stato resta memorizzato e verrà riapplicato anche in fasi/giorni successivi finché non sovrascritto.")]
    public bool persistent = false;
}

[Serializable]
public class PhaseEventList
{
#if ODIN_INSPECTOR
    [BoxGroup("Events", ShowLabel = true)]
    [LabelText("Toggles")]
    [ListDrawerSettings(Expanded = true, DraggableItems = true)]
#endif
    public List<ToggleEvent> toggles = new();

#if ODIN_INSPECTOR
    [BoxGroup("Events")]
    [LabelText("Actions (UnityEvents)")]
#endif
    public UnityEvent actions;   // azioni “on enter phase” (indipendenti dai toggle)
}

[Serializable]
public class DayConfig
{
    [Tooltip("Questo giorno ha anche la fase Notte?")]
    public bool hasNight = true;

#if ODIN_INSPECTOR
    [FoldoutGroup("Morning"), LabelWidth(90)]
#endif
    public PhaseEventList morning = new();

#if ODIN_INSPECTOR
    [FoldoutGroup("Night"), LabelWidth(90)]
#endif
    public PhaseEventList night = new();
}

// ----------------- MANAGER -----------------

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Config iniziale")]
    [Tooltip("Indice 0..6 (0 = Giorno 1, 6 = Giorno 7)")]
    [SerializeField] private int startDayIndex = 0;                 // 0..6
    [SerializeField] private DayPhase startPhase = DayPhase.Morning;

    [Header("Campagna di 7 giorni")]
#if ODIN_INSPECTOR
    [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = true)]
#endif
    [SerializeField] private List<DayConfig> days = new(7);

    [Header("Eventi GLOBALI sempre attivi (mutuamente esclusivi)")]
#if ODIN_INSPECTOR
    [FoldoutGroup("Global Morning Always"), LabelWidth(140)]
#endif
    [SerializeField] private PhaseEventList globalMorningAlways = new();

#if ODIN_INSPECTOR
    [FoldoutGroup("Global Night Always"), LabelWidth(140)]
#endif
    [SerializeField] private PhaseEventList globalNightAlways = new();

    [Header("Audio (loop con fade, usa settaggi dell'AudioSource)")]
    public float defaultFadeIn = 10.0f;
    public float defaultFadeOut = 3.0f;

#if ODIN_INSPECTOR
    [LabelText("Target Volume")]
    [Tooltip("Volume di destinazione per i fade (0..1). Viene preso dall'Inspector e NON dal volume iniziale dell'AudioSource.")]
    [PropertyRange(0f, 1f)]
#else
    [Tooltip("Volume di destinazione per i fade (0..1). Viene preso dall'Inspector e NON dal volume iniziale dell'AudioSource.")]
    [Range(0f, 1f)]
#endif
    [SerializeField] private float targetVolume = 1f;

    // Stato pubblico
    public int CurrentDayIndex { get; private set; } = 0;           // 0..6
    public int CurrentDay => CurrentDayIndex + 1;                    // 1..7 (per UI)
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Morning;
    public event Action<int, DayPhase> OnPhaseChanged;

    // ---- Stato oggetti ----
    private readonly Dictionary<GameObject, bool> baseline = new();   // activeSelf allo start
    private readonly Dictionary<GameObject, bool> persisted = new();  // override persistenti

    // ---- Audio ----
    private AudioSource audioSource;     // via GetComponent (non in Inspector)
    private Coroutine audioRoutine;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSevenDays();

        audioSource = GetComponent<AudioSource>();
        // Non leggiamo più qui un "target volume" dall'AudioSource:
        // i fade useranno sempre 'targetVolume' impostato dall'Inspector.
        // (Opzionale) Se vuoi allineare l'AudioSource al target all'avvio:
        // audioSource.volume = Mathf.Clamp01(targetVolume);
    }

#if UNITY_EDITOR
    void OnValidate() => EnsureSevenDays();
#endif

    void Start()
    {
        CurrentDayIndex = Mathf.Clamp(startDayIndex, 0, 6);
        CurrentPhase = startPhase;

        BuildBaseline();
        ApplyPhase();
    }

    // ---------- Campagna ----------

    private void EnsureSevenDays()
    {
        if (days == null) days = new List<DayConfig>(7);
        while (days.Count < 7) days.Add(new DayConfig());
        while (days.Count > 7) days.RemoveAt(days.Count - 1);
    }

    private void BuildBaseline()
    {
        baseline.Clear();

        void Acc(PhaseEventList list)
        {
            if (list == null || list.toggles == null) return;
            for (int i = 0; i < list.toggles.Count; i++)
            {
                var t = list.toggles[i];
                if (!t?.target) continue;
                if (!baseline.ContainsKey(t.target))
                    baseline[t.target] = t.target.activeSelf;
            }
        }

        Acc(globalMorningAlways);
        Acc(globalNightAlways);
        for (int i = 0; i < days.Count; i++)
        {
            Acc(days[i].morning);
            Acc(days[i].night);
        }
    }

    public void AdvancePhase()
    {
        var cfg = days[Mathf.Clamp(CurrentDayIndex, 0, days.Count - 1)];

        if (CurrentPhase == DayPhase.Morning)
        {
            if (cfg.hasNight) CurrentPhase = DayPhase.Night;
            else
            {
                CurrentDayIndex = Mathf.Min(CurrentDayIndex + 1, 6);
                CurrentPhase = DayPhase.Morning;
            }
        }
        else
        {
            CurrentDayIndex = Mathf.Min(CurrentDayIndex + 1, 6);
            CurrentPhase = DayPhase.Morning;
        }

        ApplyPhase();
    }

    private void ApplyPhase()
    {
        // 0) baseline per chi NON è persistente
        foreach (var kv in baseline)
        {
            var go = kv.Key;
            if (!go) continue;
            if (persisted.ContainsKey(go)) continue;
            SafeSetActive(go, kv.Value);
        }

        // 1) globali esclusivi
        if (CurrentPhase == DayPhase.Morning)
        {
            ApplyToggles(globalMorningAlways.toggles);
            globalMorningAlways.actions?.Invoke();

            ForceDeactivate(globalNightAlways.toggles);
        }
        else
        {
            ApplyToggles(globalNightAlways.toggles);
            globalNightAlways.actions?.Invoke();

            ForceDeactivate(globalMorningAlways.toggles);
        }

        // 2) eventi del giorno corrente
        var cfg = days[Mathf.Clamp(CurrentDayIndex, 0, days.Count - 1)];
        var list = (CurrentPhase == DayPhase.Morning) ? cfg.morning : cfg.night;

        ApplyToggles(list.toggles);
        list.actions?.Invoke();

        // 3) ri-applica override persistenti
        foreach (var kv in persisted)
            SafeSetActive(kv.Key, kv.Value);

        // 4) notifica
        OnPhaseChanged?.Invoke(CurrentDay, CurrentPhase);

        // 5) logica mattino centralizzata
        if (CurrentPhase == DayPhase.Morning)
        {
            TimerManager.Instance?.ResetToIdle();
            var panels = FindObjectsByType<BulletinController>(FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++) panels[i]?.RefreshNow();
        }
    }

    private void ApplyToggles(List<ToggleEvent> list)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || !e.target) continue;

            SafeSetActive(e.target, e.setActive);
            if (e.persistent)
                persisted[e.target] = e.setActive;
        }
    }

    private void ForceDeactivate(List<ToggleEvent> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e?.target) continue;
            SafeSetActive(e.target, false);
        }
    }

    private static void SafeSetActive(GameObject go, bool active)
    {
        if (go && go.activeSelf != active) go.SetActive(active);
    }

    // ---------- AUDIO: LOOP + FADE (rispetta i settaggi dell'AudioSource) ----------

    public void StartLoop(AudioClip clip)
    {
        if (!audioSource || clip == null) return;

        float target = Mathf.Clamp01(targetVolume); // usa il valore dall’Inspector

        // stessa clip → porta solo il volume al target, senza restart
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            if (audioRoutine != null) StopCoroutine(audioRoutine);
            audioRoutine = StartCoroutine(FadeVolume(audioSource.volume, target, defaultFadeIn));
            return;
        }

        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(FadeToClip(clip, defaultFadeIn));
    }

    public void StopLoop()
    {
        if (!audioSource) return;

        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(FadeOutAndStop(defaultFadeOut));
    }

    private System.Collections.IEnumerator FadeToClip(AudioClip newClip, float fadeIn)
    {
        float targetVol = Mathf.Clamp01(targetVolume); // usa il valore dall’Inspector

        // NON tocchiamo loop: usiamo quello configurato da te
        audioSource.clip = newClip;

        // per il fade-in abbassiamo a 0 in modo temporaneo
        float restoreAfter = targetVol;
        audioSource.volume = 0f;
        audioSource.Play();

        yield return FadeVolume(0f, restoreAfter, fadeIn);
        audioRoutine = null;
    }

    private System.Collections.IEnumerator FadeOutAndStop(float fadeOut)
    {
        float from = audioSource.volume;
        yield return FadeVolume(from, 0f, fadeOut);
        audioSource.Stop();
        audioRoutine = null;
    }

    private System.Collections.IEnumerator FadeVolume(float from, float to, float duration)
    {
        if (!audioSource) yield break;

        if (duration <= 0f)
        {
            audioSource.volume = to;
            yield break;
        }

        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // indipendente dal timeScale
            float k = Mathf.Clamp01(t / duration);
            audioSource.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }
        audioSource.volume = to;
    }
}
