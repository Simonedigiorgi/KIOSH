using TMPro;
using UnityEngine;

public class TimerDisplayUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip timerTickClip;
    public AudioClip timerStopClip;
    public AudioClip taskCompleteClip;

    private const float UI_REFRESH_HZ = 10f;
    private WaitForSecondsRealtime uiWait;

    private const string waitingText = "Waiting for operator C526-2";
    private const string workText = "Do your job";
    private const string taskCompleteText = "Job complete, return to the room";
    private const string failText = "Operator C526-2 job failed";
    private const string successText = "Operator C526-2 day completed";
    private const string nightText = "Not operational";

    private int lastDisplayedSecond = -1;
    private string lastHeader = "";
    private bool lastRunning = false;

    private Coroutine uiLoop;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        uiWait = new WaitForSecondsRealtime(1f / UI_REFRESH_HZ);
    }

    void OnEnable()
    {
        TimerManager.OnTimerStartedGlobal += OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal += OnTimerCompleted;
        TimerManager.OnTaskCompletedGlobal += OnTaskCompleted;
        TimerManager.OnDayCompletedGlobal += OnDayCompleted;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += OnPhaseChanged;

        var gs = GameStateManager.Instance;
        var tm = TimerManager.Instance;

        if (tm != null && tm.IsRunning) StartUiLoop();
        else SetIdleLabelByPhase(gs != null ? gs.CurrentPhase : DayPhase.Morning);
    }

    void OnDisable()
    {
        StopUiLoop();

        TimerManager.OnTimerStartedGlobal -= OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal -= OnTimerCompleted;
        TimerManager.OnTaskCompletedGlobal -= OnTaskCompleted;
        TimerManager.OnDayCompletedGlobal -= OnDayCompleted;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    // ------ Nuovo: forza refresh per tutte le istanze ------
    public static void ForceIdleRefresh()
    {
        var gs = GameStateManager.Instance;
        if (gs == null) return;

        var phase = gs.CurrentPhase;
        var instances = FindObjectsByType<TimerDisplayUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < instances.Length; i++)
        {
            var ui = instances[i];
            if (ui == null) continue;

            var tm = TimerManager.Instance;
            if (tm == null || !tm.IsRunning)
            {
                ui.StopUiLoop();
                ui.SetIdleLabelByPhase(phase);
            }
        }
    }

    // ---------- Events ----------
    private void OnTimerStarted()
    {
        lastDisplayedSecond = -1;
        lastRunning = true;
        StartUiLoop();
    }

    private void OnTimerCompleted()
    {
        StopUiLoop();
        PlayStop();
        if (label) label.text = failText + "\n00:00";
    }

    private void OnTaskCompleted()
    {
        var tm = TimerManager.Instance;
        if (!tm || !label) return;

        lastHeader = taskCompleteText;
        label.text = taskCompleteText + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);

        if (audioSource && taskCompleteClip) audioSource.PlayOneShot(taskCompleteClip);
    }

    private void OnDayCompleted()
    {
        StopUiLoop();
        PlayStop();
        if (label) label.text = successText;
    }

    private void OnPhaseChanged(int day, DayPhase phase)
    {
        var tm = TimerManager.Instance;
        if (tm == null || !tm.IsRunning)
        {
            StopUiLoop();
            SetIdleLabelByPhase(phase);
        }
    }

    // ---------- Loop ----------
    private void StartUiLoop()
    {
        if (uiLoop != null) return;
        uiLoop = StartCoroutine(UiRunningLoop());
    }

    private void StopUiLoop()
    {
        if (uiLoop != null)
        {
            StopCoroutine(uiLoop);
            uiLoop = null;
        }
    }

    private System.Collections.IEnumerator UiRunningLoop()
    {
        var tm = TimerManager.Instance;

        while (tm != null && tm.IsRunning)
        {
            string header = tm.DeliveriesCompleted ? taskCompleteText : workText;
            int currentSecond = Mathf.CeilToInt(tm.RemainingSeconds);

            if (currentSecond != lastDisplayedSecond || header != lastHeader || !lastRunning)
            {
                if (label) label.text = header + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);

                if (currentSecond != lastDisplayedSecond && currentSecond > 0) PlayTick();

                lastDisplayedSecond = currentSecond;
                lastHeader = header;
                lastRunning = true;
            }
            yield return uiWait;
        }

        lastRunning = false;
        var gs = GameStateManager.Instance;
        SetIdleLabelByPhase(gs != null ? gs.CurrentPhase : DayPhase.Morning);
        uiLoop = null;
    }

    // ---------- Helpers ----------
    private void PlayTick()
    {
        if (audioSource && timerTickClip) audioSource.PlayOneShot(timerTickClip);
    }

    private void PlayStop()
    {
        if (audioSource && timerStopClip)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(timerStopClip);
        }
    }

    private void SetIdleLabelByPhase(DayPhase phase)
    {
        if (!label) return;

        if (phase == DayPhase.Night) label.text = nightText;
        else label.text = GetWaitingLabelText();

        lastDisplayedSecond = -1;
        lastHeader = "";
        lastRunning = false;
    }

    private string GetWaitingLabelText()
    {
        var tm = TimerManager.Instance
                 ?? FindFirstObjectByType<TimerManager>(FindObjectsInactive.Include);

        float secs = (tm != null) ? tm.defaultDurationSeconds : 300f;
        return waitingText + "\n" + TimerManager.FormatTime(secs);
    }
}
