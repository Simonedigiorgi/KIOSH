using TMPro;
using UnityEngine;

public class TimerDisplayUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;

    [Header("Testo pre-reentry")]
    [Tooltip("Testo mostrato tra congelamento e avvio reentry.")]
    public string preReentryText = "Il timer di rientro si avvierà presto";
    public float freezeToMessageDelay = 1.5f;

    [Header("Testo esito giornata")]
    public string dayCompleteText = "Giornata completata";
    public string dayFailedText = "Giornata fallita";
    public float resultMessageDuration = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip timerLoopClip;
    public AudioClip timerStopClip;

    private bool lockText = false;
    private float lockUntil = 0f;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        TimerManager.OnTimerStartedGlobal += OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal += OnTimerCompleted;
        TimerManager.OnReentryStartedGlobal += OnReentryStarted;
        TimerManager.OnReentryCompletedGlobal += OnReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += OnAllDeliveriesCompleted;

        if (label) label.text = "Timer: 00:00:000";
    }

    void OnDisable()
    {
        TimerManager.OnTimerStartedGlobal -= OnTimerStarted;
        TimerManager.OnTimerCompletedGlobal -= OnTimerCompleted;
        TimerManager.OnReentryStartedGlobal -= OnReentryStarted;
        TimerManager.OnReentryCompletedGlobal -= OnReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= OnAllDeliveriesCompleted;
    }

    void Update()
    {
        var tm = TimerManager.Instance;
        if (!label || tm == null) return;

        if (lockText && Time.time < lockUntil) return;
        lockText = false;

        if (tm.IsReentryActive)
        {
            label.text = "Rientro: " + TimerManager.FormatTime(tm.ReentryRemainingSeconds);
            return;
        }

        if (tm.IsRunning)
        {
            label.text = "Timer: " + TimerManager.FormatTime(tm.RemainingSeconds);
            return;
        }
    }

    private void OnTimerStarted()
    {
        if (audioSource && timerLoopClip)
        {
            audioSource.loop = true;
            audioSource.clip = timerLoopClip;
            audioSource.Play();
        }
    }

    private void OnTimerCompleted()
    {
        StopLoopPlayStop();
        if (label) label.text = "Timer: 00:00:000";
    }

    private void OnAllDeliveriesCompleted()
    {
        var tm = TimerManager.Instance;
        if (!tm || !label) return;

        label.text = "Timer: " + TimerManager.FormatTime(tm.RemainingSeconds);

        StopLoopPlayStop();

        if (freezeToMessageDelay > 0f)
        {
            lockText = true;
            lockUntil = Time.time + freezeToMessageDelay;
            Invoke(nameof(ShowPreReentryMessage), freezeToMessageDelay);
        }
        else
        {
            ShowPreReentryMessage();
        }
    }

    private void ShowPreReentryMessage()
    {
        if (!label) return;

        label.text = preReentryText;

        var tm = TimerManager.Instance;
        if (tm)
        {
            lockText = true;
            lockUntil = Time.time + tm.reentryDelayOnAllDelivered;

            Invoke(nameof(StartReentryFromUI), tm.reentryDelayOnAllDelivered);
        }
    }
    private void StartReentryFromUI()
    {
        TimerManager.Instance?.StartReentryCountdown();
    }

    private void OnReentryStarted()
    {
        lockText = false;
        if (audioSource && timerLoopClip)
        {
            audioSource.loop = true;
            audioSource.clip = timerLoopClip;
            audioSource.Play();
        }
    }
    private void OnReentryCompleted()
    {
        StopLoopPlayStop();

        var tm = TimerManager.Instance;
        if (!label || tm == null) return;

        if (tm.IsPlayerInsideRoom)
        {
            label.text = dayCompleteText;
        }
        else
        {
            label.text = dayFailedText;
        }

        if (resultMessageDuration > 0f)
        {
            lockText = true;
            lockUntil = Time.time + resultMessageDuration;
        }
    }

    private void StopLoopPlayStop()
    {
        if (audioSource)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.loop = false;
            if (timerStopClip) audioSource.PlayOneShot(timerStopClip);
        }
    }
}
