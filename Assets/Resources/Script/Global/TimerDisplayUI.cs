using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class TimerDisplayUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text line;

    [Header("Labels")]
    public string mainPrefix = "Timer: ";
    public string reentryPrefix = "Rientro: ";
    public string idleText = "Timer: 00:00:000";

    void Awake()
    {
        if (!line) line = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        Subscribe(true);
        ForceRefresh();
    }

    void OnDisable()
    {
        Subscribe(false);
    }

    void Update()
    {
        // Manteniamo il testo aggiornato anche senza eventi (es. cambio scena)
        Refresh();
    }

    void Subscribe(bool on)
    {
        if (on)
        {
            TimerManager.OnTimerStartedGlobal += ForceRefresh;
            TimerManager.OnTimerCompletedGlobal += ForceRefresh;
            TimerManager.OnReentryStartedGlobal += ForceRefresh;
            TimerManager.OnReentryCompletedGlobal += ForceRefresh;
        }
        else
        {
            TimerManager.OnTimerStartedGlobal -= ForceRefresh;
            TimerManager.OnTimerCompletedGlobal -= ForceRefresh;
            TimerManager.OnReentryStartedGlobal -= ForceRefresh;
            TimerManager.OnReentryCompletedGlobal -= ForceRefresh;
        }
    }

    void ForceRefresh() => Refresh();

    void Refresh()
    {
        if (!line) return;

        var tm = TimerManager.Instance;
        if (tm == null)
        {
            line.text = idleText;
            return;
        }

        if (tm.IsReentryActive)
            line.text = reentryPrefix + TimerManager.FormatTime(tm.ReentryRemainingSeconds);
        else if (tm.IsRunning)
            line.text = mainPrefix + TimerManager.FormatTime(tm.RemainingSeconds);
        else
            // prima dell’avvio o appena scaduto (in attesa di reentry)
            line.text = idleText;
    }
}
