using UnityEngine;
using System;
using System.Collections.Generic;

public enum DayPhase   // ✅ definito una sola volta, visibile a tutto il file
{
    Morning,
    Night
}


[System.Serializable]   // <-- IMPORTANTE
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
                // Se il giorno prevede una notte → vai a notte
                CurrentPhase = DayPhase.Night;
                Debug.Log($"🌙 Giorno {CurrentDay} → Notte");
            }
            else
            {
                // Se non prevede notte → passa direttamente al giorno successivo
                CurrentDay++;
                CurrentPhase = DayPhase.Morning;
                Debug.Log($"🌞 Giorno {CurrentDay} → Mattina");
            }
        }
        else if (CurrentPhase == DayPhase.Night)
        {
            // ✅ Dopo qualsiasi notte → vai SEMPRE al giorno successivo
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
                foreach (var obj in ev.objectsToEnable)
                    if (obj) obj.SetActive(true);

                foreach (var obj in ev.objectsToDisable)
                    if (obj) obj.SetActive(false);

                Debug.Log($"[GameStateManager] Evento eseguito: Giorno {ev.day}, {ev.phase}");
            }
            else
            {
                foreach (var obj in ev.objectsToEnable)
                    if (obj) obj.SetActive(false);
            }
        }

        OnPhaseChanged?.Invoke(CurrentDay, CurrentPhase);
    }
}
