using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform sleepPoint;
    [SerializeField] private PlayableDirector wakeUpTimeline;

    [Header("Config")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float sleepDuration = 3f;

    private bool isUsed = false;

    public void Interact(PlayerInteractor interactor)
    {
        var player = interactor.GetComponent<PlayerController>();
        if (player != null) UseBed(player);
    }

    public string GetInteractionName() => "Dormire";

    public void UseBed(PlayerController player)
    {
        var tm = TimerManager.Instance;
        var gs = GameStateManager.Instance;

        if (gs != null && gs.CurrentPhase == DayPhase.Morning)
        {
            if (tm == null || !tm.DayCompleted)
            {
                HUDManager.Instance.ShowDialog("You cannot sleep: day not completed.");
                return;
            }
        }

        if (isUsed) return;

        if (player != null) player.SetControlsEnabled(false);
        StartCoroutine(SleepSequence(player));
    }

    private IEnumerator SleepSequence(PlayerController player)
    {
        isUsed = true;

        // fade-out audio ambiente
        GameStateManager.Instance?.StopLoop();

        // Fade to black
        if (HUDManager.Instance != null)
            yield return HUDManager.Instance.StartCoroutine(HUDManager.Instance.FadeBlackout(fadeDuration));
        else
            yield return new WaitForSeconds(fadeDuration);

        // Teletrasporto
        if (player != null && sleepPoint != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = sleepPoint.position;
            player.transform.rotation = sleepPoint.rotation;
            player.ResetCameraRotation();

            if (cc != null) cc.enabled = true;
        }

        // "Dormi"
        yield return new WaitForSeconds(sleepDuration);

        // Chiudi blackout
        if (HUDManager.Instance?.blackoutPanel != null)
            HUDManager.Instance.blackoutPanel.SetActive(false);

        // Avanza fase
        GameStateManager.Instance?.AdvancePhase();

        // Timeline risveglio (per il fade-in audio usa un evento Morning con StartLoop)
        wakeUpTimeline?.Play();

        if (player != null) player.SetControlsEnabled(true);
        isUsed = false;
    }
}
