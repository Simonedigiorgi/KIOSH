using System;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Durata predefinita")]
    public float defaultDurationSeconds = 300f;

    private bool running;
    private float remaining;

    public bool IsRunning => running;
    public float RemainingSeconds => remaining;

    public static event Action OnTimerStartedGlobal;
    public static event Action OnTimerCompletedGlobal;

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

    void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            CompleteTimer();
        }
    }

    public void StartTimer() => StartTimer(defaultDurationSeconds);

    public void StartTimer(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        remaining = seconds;
        running = seconds > 0f;

        if (running)
        {
            Debug.Log("[TimerManager] Timer avviato");
            OnTimerStartedGlobal?.Invoke();
        }
    }

    public void StopTimer()
    {
        if (!running) return;
        running = false;
        Debug.Log("[TimerManager] Timer fermato manualmente");
    }

    private void CompleteTimer()
    {
        running = false;
        remaining = 0f;

        Debug.Log("[TimerManager] Timer completato");
        OnTimerCompletedGlobal?.Invoke();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        var ts = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:00}:{1:00}:{2:000}",
            (int)ts.TotalMinutes, ts.Seconds, ts.Milliseconds);
    }
}
