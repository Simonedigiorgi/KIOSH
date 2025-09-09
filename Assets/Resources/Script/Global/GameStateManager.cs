// GameStateManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public enum DayPhase { Morning, Night }

[System.Serializable]
public class PhaseEvent
{
    public int day;
    public DayPhase phase;
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Morning;

    [Header("Config iniziale")]
    [SerializeField] private int startDay = 1;
    [SerializeField] private DayPhase startPhase = DayPhase.Morning;

    [Header("Giorni con fase notturna")]
    [SerializeField] private List<int> daysWithNight = new List<int>();

    [Header("Eventi per fasi")]
    [SerializeField] private List<PhaseEvent> phaseEvents = new List<PhaseEvent>();

    public event Action<int, DayPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CurrentDay = startDay;
        CurrentPhase = startPhase;
        Debug.Log($"[GameStateManager] Avvio: Giorno {CurrentDay}, {CurrentPhase}");
        TriggerPhaseEvents();
    }

    public void AdvancePhase()
    {
        Debug.Log($"[GameStateManager] AdvancePhase chiamato (giorno {CurrentDay}, fase {CurrentPhase})");

        if (CurrentPhase == DayPhase.Morning)
        {
            if (daysWithNight.Contains(CurrentDay))
            {
                CurrentPhase = DayPhase.Night;
                Debug.Log($"🌙 Giorno {CurrentDay} → Notte");
            }
            else
            {
                CurrentDay++;
                CurrentPhase = DayPhase.Morning;
                Debug.Log($"🌞 Giorno {CurrentDay} → Mattina");
            }
        }
        else // Night
        {
            CurrentDay++;
            CurrentPhase = DayPhase.Morning;
            Debug.Log($"🌞 Giorno {CurrentDay} → Mattina (dopo la notte)");
        }

        TriggerPhaseEvents();
    }

    private void TriggerPhaseEvents()
    {
        Debug.Log($"[GameStateManager] Attivo eventi: Giorno {CurrentDay}, {CurrentPhase}");

        foreach (var ev in phaseEvents)
        {
            if (ev.day == CurrentDay && ev.phase == CurrentPhase)
            {
                foreach (var obj in ev.objectsToEnable) if (obj) obj.SetActive(true);
                foreach (var obj in ev.objectsToDisable) if (obj) obj.SetActive(false);
                Debug.Log($"[GameStateManager] Evento eseguito: Giorno {ev.day}, {ev.phase}");
            }
            else
            {
                // se vuoi evitare toggle aggressivi, puoi rimuovere questa parte:
                foreach (var obj in ev.objectsToEnable) if (obj) obj.SetActive(false);
            }
        }

        // Notifica i listener (UI/adapter)
        OnPhaseChanged?.Invoke(CurrentDay, CurrentPhase);

        // 👇 Centralizza qui la logica del “mattino”: resetta timer e refresha i pannelli
        if (CurrentPhase == DayPhase.Morning)
        {
            TimerManager.Instance?.ResetToIdle();

            // refresh una tantum di tutte le bacheche in scena (evento raro → costo trascurabile)
            var panels = FindObjectsByType<BulletinController>(FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
                panels[i]?.RefreshNow();
        }
    }
}
