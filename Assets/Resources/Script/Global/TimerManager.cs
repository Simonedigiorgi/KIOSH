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

    // Cache della porta
    private RoomDoor roomDoor;

    // API
    public bool IsRunning => running;
    public float RemainingSeconds => remaining;
    public bool IsPlayerInsideRoom => playerInsideRoom;
    public bool DeliveriesCompleted => deliveriesCompleted;

    // Eventi globali
    public static event Action OnTimerStartedGlobal;
    public static event Action OnTimerCompletedGlobal;
    public static event Action OnTaskCompletedGlobal;
    public static event Action OnDayCompletedGlobal;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CacheRoomDoor();
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
        {
            roomDoor?.OpenDoor();
            OnTimerStartedGlobal?.Invoke();
        }
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
        roomDoor?.OpenDoor();
        OnTaskCompletedGlobal?.Invoke();
    }

    // ===== Player state =====
    public void SetPlayerInsideRoom(bool inside)
    {
        playerInsideRoom = inside;

        // ✅ Se il player entra in camera dopo aver completato il task
        if (inside && deliveriesCompleted && running)
        {
            roomDoor?.CloseDoor();

            running = false;
            OnDayCompletedGlobal?.Invoke();
        }
    }

    public void ResetToIdle()
    {
        running = false;
        remaining = 0f;
        deliveriesCompleted = false;
    }

    // ===== Utility =====
    private void CacheRoomDoor()
    {
        var go = GameObject.FindWithTag("RoomDoor");
        roomDoor = go ? go.GetComponent<RoomDoor>() : null;

        if (roomDoor == null)
            Debug.LogWarning("[TimerManager] Nessuna RoomDoor trovata con tag 'RoomDoor'.");
    }

    public static string FormatTime(float seconds)
    {
        if (seconds <= 0f) return "00:00:000";
        int totalMs = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
        var ts = TimeSpan.FromMilliseconds(totalMs);
        return string.Format("{0:00}:{1:00}:{2:000}", (int)ts.TotalMinutes, ts.Seconds, ts.Milliseconds);
    }
}
