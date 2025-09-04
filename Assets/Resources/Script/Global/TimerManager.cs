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
    public float reentryDurationSeconds = 30f;
    [Tooltip("Ritardo tra 'consegne completate' e avvio del reentry (il TimerDisplay può mostrare messaggi in questo intervallo).")]
    public float reentryDelayOnAllDelivered = 4f;

    [Header("Door (opzionale)")]
    public RoomDoor bedroomDoor;

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
    private Coroutine reentryRoutine;

    // Stato vario
    private bool playerInsideRoom = false;
    private bool deliveriesCompleted = false;

    // API stato
    public bool IsRunning => running;
    public float RemainingSeconds => remaining;
    public bool IsReentryActive => reentryActive;
    public float ReentryRemainingSeconds => reentryRemaining;
    public bool IsPlayerInsideRoom => playerInsideRoom;
    public bool IsFrozenAwaitingReentry => deliveriesCompleted && !running && !reentryActive;

    // Eventi statici globali
    public static event Action OnTimerStartedGlobal;
    public static event Action OnTimerCompletedGlobal;
    public static event Action OnReentryStartedGlobal;
    public static event Action OnReentryCompletedGlobal;

    // ===== Lifecycle =====
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable() => DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;
    void OnDisable() => DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;

    void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            CompleteMainTimer();
        }
    }

    // ===== Timer principale =====
    public void StartTimer() => StartTimer(defaultDurationSeconds);

    public void StartTimer(float seconds)
    {
        CancelReentry(); // ripulisci il reentry

        remaining = Mathf.Max(0f, seconds);
        running = remaining > 0f;
        deliveriesCompleted = false;

        if (running)
        {
            var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
            door?.OpenDoor();

            OnTimerStartedGlobal?.Invoke();
            onTimerStarted?.Invoke();
        }
    }

    public void StopTimer()
    {
        if (!running) return;
        running = false;
    }

    private void CompleteMainTimer()
    {
        running = false;
        remaining = 0f;

        OnTimerCompletedGlobal?.Invoke();
        onTimerCompleted?.Invoke();
    }

    // ===== Consegne completate =====
    private void HandleAllDeliveriesCompleted()
    {
        deliveriesCompleted = true;
        if (!running) return;

        running = false; // freeze

        var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
        door?.OpenDoor();

        // Somma tempo rimasto + durata reentry
        float totalReentry = reentryDurationSeconds + remaining;
        // Lo memorizziamo: TimerDisplayUI lo farà partire dopo il suo delay
        reentryRemaining = totalReentry;
    }

    // ===== Reentry =====
    public void StartReentryCountdown()
    {
        StartReentryCountdown(reentryRemaining > 0f ? reentryRemaining : reentryDurationSeconds);
    }

    public void StartReentryCountdown(float seconds)
    {
        running = false;
        reentryActive = true;
        reentryRemaining = Mathf.Max(0f, seconds);

        if (reentryRoutine != null) StopCoroutine(reentryRoutine);
        reentryRoutine = StartCoroutine(ReentryCountdownRoutine());

        OnReentryStartedGlobal?.Invoke();
        onReentryStarted?.Invoke();

        if (playerInsideRoom)
        {
            var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
            door?.CloseDoor();
            CompleteReentry();
        }
    }

    private IEnumerator ReentryCountdownRoutine()
    {
        float end = Time.unscaledTime + reentryRemaining;
        while (reentryActive)
        {
            reentryRemaining = Mathf.Max(0f, end - Time.unscaledTime);
            if (reentryRemaining <= 0f)
            {
                CompleteReentry();
                yield break;
            }
            yield return null;
        }
    }

    private void CompleteReentry()
    {
        reentryActive = false;
        reentryRemaining = 0f;

        if (reentryRoutine != null)
        {
            StopCoroutine(reentryRoutine);
            reentryRoutine = null;
        }

        OnReentryCompletedGlobal?.Invoke();
        onReentryCompleted?.Invoke();
    }

    private void CancelReentry()
    {
        reentryActive = false;
        if (reentryRoutine != null)
        {
            StopCoroutine(reentryRoutine);
            reentryRoutine = null;
        }
        reentryRemaining = 0f;
    }

    // ===== Player state =====
    public void SetPlayerInsideRoom(bool inside)
    {
        playerInsideRoom = inside;

        if (inside && reentryActive)
        {
            var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
            door?.CloseDoor();
            CompleteReentry();
        }
    }

    // ===== Reset globale =====
    public void ResetToIdle()
    {
        running = false;
        remaining = 0f;
        deliveriesCompleted = false;
        CancelReentry();
    }

    // ===== Utility =====
    public static string FormatTime(float seconds)
    {
        if (seconds <= 0f) return "00:00:000";
        int totalMs = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
        var ts = TimeSpan.FromMilliseconds(totalMs);
        return string.Format("{0:00}:{1:00}:{2:000}", (int)ts.TotalMinutes, ts.Seconds, ts.Milliseconds);
    }
}
