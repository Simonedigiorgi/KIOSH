using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Durata predefinita")]
    public float defaultDurationSeconds = 300f;

    [Header("Reentry")]
    public float reentryDurationSeconds = 30f;   // tempo per rientrare in stanza
    public float reentryDelayBeforeStart = 2f;   // attesa prima che parta il reentry

    [Header("Door (opzionale)")]
    public RoomDoor bedroomDoor;                 // se non assegnata la cerco a runtime

    [Header("Eventi")]
    public UnityEvent onTimerStarted;
    public UnityEvent onTimerCompleted;
    public UnityEvent onReentryStarted;
    public UnityEvent onReentryCompleted;

    // Stato timer principale
    private bool running;
    private float remaining;

    // Stato reentry
    private bool reentryActive;
    private float reentryRemaining;

    // Stato player/zona
    private bool playerInsideRoom = false;

    // Stato consegne
    private bool allDeliveriesCompleted = false;

    public bool IsRunning => running;
    public float RemainingSeconds => remaining;

    public bool IsReentryActive => reentryActive;
    public float ReentryRemainingSeconds => reentryRemaining;

    public bool IsPlayerInsideRoom => playerInsideRoom;

    public static event Action OnTimerStartedGlobal;
    public static event Action OnTimerCompletedGlobal;
    public static event Action OnReentryStartedGlobal;
    public static event Action OnReentryCompletedGlobal;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;
    }

    void OnDisable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;
    }

    private void HandleAllDeliveriesCompleted()
    {
        allDeliveriesCompleted = true;
    }

    void Update()
    {
        if (running)
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                CompleteMainTimer();
        }
        else if (reentryActive)
        {
            reentryRemaining -= Time.deltaTime;
            if (reentryRemaining <= 0f)
                CompleteReentry();
        }
    }

    // ====== Timer principale ======
    public void StartTimer() => StartTimer(defaultDurationSeconds);

    public void StartTimer(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        remaining = seconds;
        running = seconds > 0f;

        if (running)
        {
            Debug.Log("[TimerManager] Timer avviato (" + FormatTime(remaining) + ")");
            OnTimerStartedGlobal?.Invoke();
            onTimerStarted?.Invoke();
        }
    }

    public void StopTimer()
    {
        if (!running) return;
        running = false;
        Debug.Log("[TimerManager] Timer fermato manualmente");
    }

    private void CompleteMainTimer()
    {
        running = false;
        remaining = 0f;

        Debug.Log("[TimerManager] Timer principale completato");

        OnTimerCompletedGlobal?.Invoke();
        onTimerCompleted?.Invoke();

        // Se TUTTE le consegne sono state fatte:
        if (allDeliveriesCompleted)
        {
            var door = bedroomDoor != null ? bedroomDoor : FindObjectOfType<RoomDoor>();
            door?.OpenDoor();
            StartCoroutine(StartReentryAfterDelay(reentryDelayBeforeStart));
        }
        // Altrimenti PlayerDeathManager gestisce la punizione.
    }

    private IEnumerator StartReentryAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartReentryCountdown(reentryDurationSeconds);
    }

    // ====== Reentry ======
    public void StartReentryCountdown() => StartReentryCountdown(reentryDurationSeconds);

    public void StartReentryCountdown(float seconds)
    {
        reentryActive = true;
        reentryRemaining = Mathf.Max(0f, seconds);

        Debug.Log("[TimerManager] Countdown di rientro avviato (" + FormatTime(reentryRemaining) + ")");

        OnReentryStartedGlobal?.Invoke();
        onReentryStarted?.Invoke();
    }

    private void CompleteReentry()
    {
        reentryActive = false;
        reentryRemaining = 0f;

        Debug.Log("[TimerManager] Countdown di rientro completato");

        OnReentryCompletedGlobal?.Invoke();
        onReentryCompleted?.Invoke();
    }

    // ====== Reset totale per reload/nuova partita ======
    public void ResetToIdle()
    {
        running = false;
        remaining = 0f;

        reentryActive = false;
        reentryRemaining = 0f;

        playerInsideRoom = false;
        allDeliveriesCompleted = false;
    }

    // ====== Player state ======
    public void SetPlayerInsideRoom(bool inside)
    {
        playerInsideRoom = inside;
        Debug.Log("[TimerManager] Player inside room = " + inside);
    }

    // ====== Utility ======
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        var ts = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:00}:{1:00}:{2:000}", (int)ts.TotalMinutes, ts.Seconds, ts.Milliseconds);
    }
}
