using System;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Durata predefinita")]
    public float defaultDurationSeconds = 300f;

    // Stato
    private bool running;
    private float remaining;
    private bool playerInsideRoom = false;
    private bool deliveriesCompleted = false;

    private bool dayCompleted = false;
    public bool DayCompleted => dayCompleted;

    // API
    public bool IsRunning => running;
    public float RemainingSeconds => remaining;
    public bool IsPlayerInsideRoom => playerInsideRoom;
    public bool DeliveriesCompleted => deliveriesCompleted;

    // Eventi globali (broadcast dominio)
    public static event Action OnTimerStartedGlobal;
    public static event Action OnTimerCompletedGlobal;
    public static event Action OnTaskCompletedGlobal;
    public static event Action OnDayCompletedGlobal;

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

        // prima: remaining -= Time.deltaTime;
        remaining -= Time.unscaledDeltaTime;   // ▶ conta anche se timeScale ≠ 1

        if (remaining <= 0f)
        {
            remaining = 0f;
            CompleteTimer();
        }
    }

    // ===== Timer =====
    public void StartTimer() => StartTimer(defaultDurationSeconds);

    public void StartTimer(float seconds)
    {
        remaining = Mathf.Max(0f, seconds);
        running = remaining > 0f;
        deliveriesCompleted = false;

        if (running)
            OnTimerStartedGlobal?.Invoke(); // solo evento
    }

    private void CompleteTimer()
    {
        running = false;
        remaining = 0f;
        OnTimerCompletedGlobal?.Invoke();
    }

    // ===== Task completato =====
    private void HandleAllDeliveriesCompleted()
    {
        deliveriesCompleted = true;
        OnTaskCompletedGlobal?.Invoke();   // solo evento
    }

    // ===== Player state =====
    public void SetPlayerInsideRoom(bool inside)
    {
        // Evita rientri ripetuti con lo stesso stato
        if (playerInsideRoom == inside)
            return;

        playerInsideRoom = inside;

        // Emitti il "day completed" UNA volta sola
        if (inside && deliveriesCompleted && !dayCompleted)
        {
            if (running) running = false;
            dayCompleted = true;
            OnDayCompletedGlobal?.Invoke();
            Debug.Log("[TimerManager] Giorno completato: player rientrato in camera.");
        }
    }

    public void ResetToIdle()
    {
        running = false;
        remaining = 0f;
        deliveriesCompleted = false;
        dayCompleted = false;
    }

    // ===== Utility =====
    public static string FormatTime(float seconds)
    {
        int secs = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = secs / 60;
        int s = secs % 60;
        return string.Format("{0:00}:{1:00}", minutes, s);
    }
}
