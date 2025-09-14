using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using Sirenix.OdinInspector;

public enum DayPhase { Morning, Night }

// ----------------- DATA -----------------

[Serializable]
public class PhaseEventList
{
    [BoxGroup("Events")]
    [LabelText("Actions (UnityEvents)")]
    public UnityEvent actions;
}

[Serializable]
public class DayConfig
{
    public bool hasNight = true;

    [FoldoutGroup("Morning"), LabelWidth(90)]
    public PhaseEventList morning = new();

    [FoldoutGroup("Night"), LabelWidth(90)]
    public PhaseEventList night = new();
}

// ----------------- MANAGER -----------------

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    // Cache scena
    private PlayerController player;
    private HUDManager hud;
    private TimerManager timer;
    private BulletinController[] panels;

    [Header("Config iniziale")]
    [SerializeField, Tooltip("Indice 0..6 (0 = Giorno 1, 6 = Giorno 7)")]
    private int startDayIndex = 0;
    [SerializeField] private DayPhase startPhase = DayPhase.Morning;

    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = false, ShowIndexLabels = true)]
    [SerializeField] private List<DayConfig> days = new(7);

    [FoldoutGroup("Global Morning Always"), LabelWidth(140)]
    [SerializeField] private PhaseEventList globalMorningAlways = new();

    [FoldoutGroup("Global Night Always"), LabelWidth(140)]
    [SerializeField] private PhaseEventList globalNightAlways = new();

    [Header("Audio (loop con fade)")]
    public float defaultFadeIn = 10f;
    public float defaultFadeOut = 3f;

    // ---------- Giorno 1: Intro ----------
    [FoldoutGroup("Day 1 Intro"), LabelWidth(150), LabelText("Abilita Intro Giorno 1")]
    [SerializeField] private bool runDay1Intro = true;

    [FoldoutGroup("Day 1 Intro"), LabelWidth(150), LabelText("Hold Nero (sec)")]
    [SerializeField] private float day1BlackHoldSeconds = 2f;

    [FoldoutGroup("Day 1 Intro"), LabelWidth(150), LabelText("Fade Out (sec)")]
    [SerializeField] private float day1FadeOutDuration = 1.5f;

    [FoldoutGroup("Day 1 Intro"), LabelWidth(150), LabelText("Anticipo Timeline (sec)"), PropertyRange(0f, 5f)]
    [SerializeField] private float day1TimelineLeadBeforeFade = 0.2f;

    [FoldoutGroup("Day 1 Intro"), LabelWidth(150), LabelText("Playable Director")]
    public PlayableDirector day1Playable;

    private bool didRunDay1Intro;

    // Stato pubblico
    public int CurrentDayIndex { get; private set; } = 0;   // 0..6
    public int CurrentDay => CurrentDayIndex + 1;           // 1..7
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Morning;
    public event Action<int, DayPhase> OnPhaseChanged;

    // Audio
    private AudioSource audioSource;
    private Coroutine audioRoutine;

    // ---------- Lifecycle ----------

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSevenDays();
        audioSource = GetComponent<AudioSource>();
    }

#if UNITY_EDITOR
    void OnValidate() => EnsureSevenDays();
#endif

    void Start()
    {
        // Cache scena UNA volta (Unity 6 API)
        player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        hud = HUDManager.Instance ?? FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
        timer = TimerManager.Instance ?? FindFirstObjectByType<TimerManager>(FindObjectsInactive.Include);
        panels = FindObjectsByType<BulletinController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        CurrentDayIndex = Mathf.Clamp(startDayIndex, 0, 6);
        CurrentPhase = startPhase;

        ApplyPhase();
    }

    // ---------- Campagna ----------

    private void EnsureSevenDays()
    {
        if (days == null) days = new List<DayConfig>(7);
        while (days.Count < 7) days.Add(new DayConfig());
        while (days.Count > 7) days.RemoveAt(days.Count - 1);
    }

    public void AdvancePhase()
    {
        var cfg = days[CurrentDayIndex];
        if (CurrentPhase == DayPhase.Morning)
        {
            if (cfg.hasNight) CurrentPhase = DayPhase.Night;
            else { if (CurrentDayIndex < 6) CurrentDayIndex++; CurrentPhase = DayPhase.Morning; }
        }
        else
        {
            if (CurrentDayIndex < 6) CurrentDayIndex++;
            CurrentPhase = DayPhase.Morning;
        }

        ApplyPhase();
    }

    private void ApplyPhase()
    {
        // Global actions
        if (CurrentPhase == DayPhase.Morning) globalMorningAlways.actions?.Invoke();
        else globalNightAlways.actions?.Invoke();

        // Day actions
        var dayCfg = days[CurrentDayIndex];
        var list = (CurrentPhase == DayPhase.Morning) ? dayCfg.morning : dayCfg.night;
        list.actions?.Invoke();

        // Notifica
        OnPhaseChanged?.Invoke(CurrentDay, CurrentPhase);

        // Mattino centralizzato
        if (CurrentPhase == DayPhase.Morning)
        {
            // ✅ azzera le consegne del giorno e notifica gli adapter
            DeliveryBox.ResetDailyDeliveries();

            // reset timer e refresh pannelli
            if (timer) timer.ResetToIdle();

            var n = panels?.Length ?? 0;
            for (int i = 0; i < n; i++)
            {
                var p = panels[i];
                if (p) p.RefreshNow();
            }
        }


        // Intro solo Morning Giorno 1 (una volta)
        if (!didRunDay1Intro && runDay1Intro && CurrentDayIndex == 0 && CurrentPhase == DayPhase.Morning)
        {
            didRunDay1Intro = true;
            StartCoroutine(Day1IntroRoutine());
        }
    }

    // ---------- AUDIO ----------

    public void StartLoop(AudioClip clip)
    {
        if (!clip) return;

        // target = slider volume dell'AudioSource al momento della chiamata
        float target = Mathf.Clamp01(audioSource.volume);

        // stessa clip → solo fade al target, senza restart
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            if (audioRoutine != null) StopCoroutine(audioRoutine);
            audioRoutine = StartCoroutine(FadeVolume(audioSource.volume, target, defaultFadeIn));
            return;
        }

        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(FadeToClip(clip, target, defaultFadeIn));
    }

    public void StopLoop()
    {
        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(FadeOutAndStop(defaultFadeOut));
    }

    private IEnumerator FadeToClip(AudioClip newClip, float targetVol, float fadeIn)
    {
        float restoreAfter = Mathf.Clamp01(targetVol);
        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.Play();

        yield return FadeVolume(0f, restoreAfter, fadeIn);
        audioRoutine = null;
    }

    private IEnumerator FadeOutAndStop(float fadeOut)
    {
        float from = audioSource.volume;
        yield return FadeVolume(from, 0f, fadeOut);
        audioSource.Stop();
        audioRoutine = null;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        if (duration <= 0f) { audioSource.volume = Mathf.Clamp01(to); yield break; }

        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);
        float t = 0f, invDur = 1f / duration;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t * invDur; if (k > 1f) k = 1f;
            audioSource.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }
        audioSource.volume = to;
    }

    // ---------- Giorno 1: nero subito → (lead) cutscene → fade-out ----------

    private IEnumerator Day1IntroRoutine()
    {
        if (!hud || !hud.blackoutPanel) yield break;

        // blocco SUBITO
        if (player) player.SetControlsEnabled(false);

        // nero
        hud.ShowBlackoutImmediateFull();

        // tempi
        float hold = (day1BlackHoldSeconds > 0f) ? day1BlackHoldSeconds : 0f;
        float lead = Mathf.Clamp(day1TimelineLeadBeforeFade, 0f, hold);

        // attesa prima della cutscene
        float waitBeforeCutscene = hold - lead;
        if (waitBeforeCutscene > 0f) yield return new WaitForSeconds(waitBeforeCutscene);

        // cutscene (player già bloccato)
        if (day1Playable)
        {
            day1Playable.stopped -= OnDay1PlayableStopped;
            day1Playable.stopped += OnDay1PlayableStopped;
            day1Playable.Play();
        }

        // attesa lead, poi fade-out
        if (lead > 0f) yield return new WaitForSeconds(lead);

        if (day1FadeOutDuration > 0f) yield return hud.FadeBlackoutOut(day1FadeOutDuration);
        else hud.SetBlackoutAlpha(0f);

        // se non c'è Timeline: sblocca subito
        if (!day1Playable && player) player.SetControlsEnabled(true);
    }

    private void OnDay1PlayableStopped(PlayableDirector dir)
    {
        if (player) player.SetControlsEnabled(true);
        if (day1Playable) day1Playable.stopped -= OnDay1PlayableStopped;
    }

    private void OnDisable()
    {
        if (day1Playable) day1Playable.stopped -= OnDay1PlayableStopped;
    }
}
