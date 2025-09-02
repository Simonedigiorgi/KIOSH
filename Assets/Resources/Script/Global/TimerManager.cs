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

    // ----- Stato timer principale -----
    private bool running;
    private float remaining;

    // ----- Stato reentry -----
    private bool reentryActive;
    private float reentryRemaining;
    private Coroutine reentryRoutine;

    // ----- Stato vario -----
    private bool playerInsideRoom = false;
    private bool deliveriesCompleted = false;

    // ----- API stato -----
    public bool IsRunning => running;
    public float RemainingSeconds => remaining;
    public bool IsReentryActive => reentryActive;
    public float ReentryRemainingSeconds => reentryRemaining;
    public bool IsPlayerInsideRoom => playerInsideRoom;
    public bool IsFrozenAwaitingReentry => deliveriesCompleted && !running && !reentryActive;

    // ----- Eventi statici globali -----
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
        CancelReentry();                 // nel dubbio, ripulisci il reentry

        remaining = Mathf.Max(0f, seconds);
        running = remaining > 0f;
        deliveriesCompleted = false;

        if (running)
        {
            // Apri la porta non appena il timer parte
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
        // Punizione gestita da PlayerDeathManager se necessario.
    }

    // ===== Consegne completate -> freeze + avvio reentry =====
    private void HandleAllDeliveriesCompleted()
    {
        deliveriesCompleted = true;
        if (!running) return;

        running = false; // freeze al valore corrente

        var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
        door?.OpenDoor();

        // ❌ Niente StartCoroutine qui
        // Sarà il TimerDisplayUI a decidere quando chiamare StartReentryCountdown
    }

    private IEnumerator StartReentryAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        StartReentryCountdown(reentryDurationSeconds);
    }

    // ===== Reentry =====
    public void StartReentryCountdown() => StartReentryCountdown(reentryDurationSeconds);

    public void StartReentryCountdown(float seconds)
    {
        running = false;

        reentryActive = true;
        reentryRemaining = Mathf.Max(0f, seconds);

        if (reentryRoutine != null) StopCoroutine(reentryRoutine);
        reentryRoutine = StartCoroutine(ReentryCountdownRoutine());

        OnReentryStartedGlobal?.Invoke();
        onReentryStarted?.Invoke();

        // Se sono già in camera quando parte il reentry, chiudilo subito e chiudi la porta
        if (playerInsideRoom)
        {
            var door = bedroomDoor ? bedroomDoor : FindObjectOfType<RoomDoor>();
            door?.CloseDoor();
            CompleteReentry();
        }
    }

    private IEnumerator ReentryCountdownRoutine()
    {
        float end = Time.unscaledTime + reentryRemaining;   // tempo di fine in real time
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

        // Se entro durante il reentry: chiudi la porta SUBITO e completa il reentry
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
