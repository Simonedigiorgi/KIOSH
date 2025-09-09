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

    [Header("Perf")]
    [Tooltip("How often the running UI refreshes (Hz). 10 = every 0.1s")]
    [Range(2f, 30f)] public float uiRefreshHz = 10f;

    private WaitForSecondsRealtime uiWait;

    private const string waitingText = "Waiting for operator C526-2";
    private const string workText = "Do your job";
    private const string taskCompleteText = "Job complete, return to the room";
    private const string failText = "Operator C526-2 job failed";
    private const string successText = "Operator C526-2 day completed";

    private bool lockText = false;
    private int lastDisplayedSecond = -1;
    private string lastHeader = "";
    private bool lastRunning = false;

    private Coroutine uiLoop;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        uiWait = new WaitForSecondsRealtime(1f / Mathf.Max(2f, uiRefreshHz));
    }

    void OnEnable()
    {
        TimerManager.OnTimerStartedGlobal += OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal += OnTimerCompleted;
        TimerManager.OnTaskCompletedGlobal += OnTaskCompleted;
        TimerManager.OnDayCompletedGlobal += OnDayCompleted;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += OnPhaseChanged;

        lastDisplayedSecond = -1;
        lastHeader = waitingText;
        lastRunning = false;

        var tm = TimerManager.Instance;
        if (tm != null && tm.IsRunning)
        {
            StartUiLoop();
        }
        else
        {
            if (label) label.text = GetWaitingLabelText();
        }
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
        if (label) label.text = failText + "\n00:00:000";
        lockText = true;
        lastRunning = false;
    }

    private void OnTaskCompleted()
    {
        var tm = TimerManager.Instance;
        if (!tm || !label) return;

        // header changes; loop will keep refreshing remaining time
        lastHeader = taskCompleteText;
        label.text = taskCompleteText + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);
        lockText = false;

        if (audioSource && taskCompleteClip)
            audioSource.PlayOneShot(taskCompleteClip);
    }

    private void OnDayCompleted()
    {
        StopUiLoop();
        PlayStop();
        if (label) label.text = successText;
        lockText = true;
        lastRunning = false;
    }

    private void OnPhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning)
        {
            TimerManager.Instance?.ResetToIdle();
            lockText = false;
            lastDisplayedSecond = -1;
            lastHeader = waitingText;
            lastRunning = false;
            StopUiLoop();
            if (label) label.text = GetWaitingLabelText();
        }
        else
        {
            // di notte: niente loop, resta il testo corrente
            StopUiLoop();
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

        while (tm != null && tm.IsRunning && !lockText)
        {
            string header = tm.DeliveriesCompleted ? taskCompleteText : workText;
            int currentSecond = Mathf.CeilToInt(tm.RemainingSeconds);

            if (currentSecond != lastDisplayedSecond || header != lastHeader || !lastRunning)
            {
                if (label) label.text = header + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);

                if (currentSecond != lastDisplayedSecond && currentSecond > 0)
                    PlayTick();

                lastDisplayedSecond = currentSecond;
                lastHeader = header;
                lastRunning = true;
            }

            yield return uiWait;
        }

        // usciti dal loop: non correndo più → se non lockato, torna a waiting
        lastRunning = false;
        if (!lockText && label) label.text = GetWaitingLabelText();
        uiLoop = null;
    }

    // ---------- Helpers ----------
    private void PlayTick()
    {
        if (audioSource && timerTickClip)
            audioSource.PlayOneShot(timerTickClip);
    }

    private void PlayStop()
    {
        if (audioSource && timerStopClip)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(timerStopClip);
        }
    }

    private string GetWaitingLabelText()
    {
        var tm = TimerManager.Instance;
        return (tm != null)
            ? waitingText + "\n" + TimerManager.FormatTime(tm.defaultDurationSeconds)
            : waitingText + "\n00:00:000";
    }
}
