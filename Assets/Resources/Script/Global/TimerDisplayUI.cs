using TMPro;
using UnityEngine;

public class TimerDisplayUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip timerTickClip;   // singolo "tic"
    public AudioClip timerStopClip;
    public AudioClip taskCompleteClip; // 🔊 nuovo audio al completamento task

    // 🔒 Testi interni
    private const string waitingText = "Waiting for operator C526-2";
    private const string workText = "Do your job";
    private const string taskCompleteText = "Job complete, return to your room";
    private const string failText = "Operator C526-2 job failed";
    private const string successText = "Operator C526-2 day completed";

    private bool lockText = false;

    // Per gestire i “tic” ogni secondo
    private int lastDisplayedSecond = -1;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        TimerManager.OnTimerStartedGlobal += OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal += OnTimerCompleted;
        TimerManager.OnTaskCompletedGlobal += OnTaskCompleted;
        TimerManager.OnDayCompletedGlobal += OnDayCompleted;

        if (label) label.text = GetWaitingLabelText();
    }

    void OnDisable()
    {
        TimerManager.OnTimerStartedGlobal -= OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal -= OnTimerCompleted;
        TimerManager.OnTaskCompletedGlobal -= OnTaskCompleted;
        TimerManager.OnDayCompletedGlobal -= OnDayCompleted;
    }

    void Update()
    {
        var tm = TimerManager.Instance;
        if (!label || tm == null) return;
        if (lockText) return;

        if (tm.IsRunning)
        {
            string header = tm.DeliveriesCompleted ? taskCompleteText : workText;
            label.text = header + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);

            // 🎵 Gestione tick audio
            int currentSecond = Mathf.CeilToInt(tm.RemainingSeconds);
            if (currentSecond != lastDisplayedSecond && currentSecond > 0)
            {
                lastDisplayedSecond = currentSecond;
                PlayTick();
            }
            return;
        }

        // Timer fermo, non ancora avviato
        label.text = GetWaitingLabelText();
        lastDisplayedSecond = -1; // reset
    }

    private void OnTimerStarted()
    {
        lastDisplayedSecond = -1; // reset per sicurezza
    }

    private void OnTimerCompleted()
    {
        PlayStop();
        if (label) label.text = failText + "\n00:00:000";
        lockText = true; // resta fisso fino a reload
    }

    private void OnTaskCompleted()
    {
        var tm = TimerManager.Instance;
        if (!tm || !label) return;

        label.text = taskCompleteText + "\n" + TimerManager.FormatTime(tm.RemainingSeconds);
        lockText = false;

        // 🔊 suono task completato
        if (audioSource && taskCompleteClip)
            audioSource.PlayOneShot(taskCompleteClip);
    }

    private void OnDayCompleted()
    {
        PlayStop();
        if (label) label.text = successText;
        lockText = true;
    }

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

    /// <summary>
    /// Restituisce il testo da mostrare quando il timer non è ancora avviato.
    /// </summary>
    private string GetWaitingLabelText()
    {
        var tm = TimerManager.Instance;
        if (tm != null)
            return waitingText + "\n" + TimerManager.FormatTime(tm.defaultDurationSeconds);
        else
            return waitingText + "\n00:00:000";
    }
}
