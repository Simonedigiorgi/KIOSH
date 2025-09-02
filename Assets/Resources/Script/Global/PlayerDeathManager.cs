using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathManager : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource playerAudioSource;
    public AudioClip beep1Clip;
    public AudioClip beep2Clip;

    [Header("Blackout & Reload")]
    public float blackoutDelayAfterBeep2 = 0f;
    public float reloadDelay = 2f;

    private bool allDelivered = false;

    void OnEnable()
    {
        TimerManager.OnTimerCompletedGlobal += HandleTimerCompleted;
        TimerManager.OnReentryCompletedGlobal += HandleReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;
    }

    void OnDisable()
    {
        TimerManager.OnTimerCompletedGlobal -= HandleTimerCompleted;
        TimerManager.OnReentryCompletedGlobal -= HandleReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;
    }

    private void HandleAllDeliveriesCompleted()
    {
        // Segna che il player ha consegnato tutto.
        // Il resto (apertura porta + avvio reentry dopo delay) lo gestisce TimerManager.
        allDelivered = true;
        Debug.Log("[PlayerDeathManager] Tutte le consegne completate (TimerManager gestisce delay e reentry).");
    }

    private void HandleTimerCompleted()
    {
        if (allDelivered)
        {
            // Niente qui: TimerManager ha già aperto la porta e avvierà
            // il reentry dopo reentryDelayBeforeStart.
            return;
        }

        // Consegne non completate → punizione immediata
        StartCoroutine(PunishmentSequence());
    }

    private void HandleReentryCompleted()
    {
        // Se non sei in stanza alla fine del reentry → punizione
        var tm = TimerManager.Instance;
        if (tm != null && !tm.IsPlayerInsideRoom)
            StartCoroutine(PunishmentSequence());
    }

    private IEnumerator PunishmentSequence()
    {
        if (playerAudioSource && beep1Clip)
        {
            playerAudioSource.PlayOneShot(beep1Clip);
            yield return new WaitForSeconds(beep1Clip.length);
        }

        if (playerAudioSource && beep2Clip)
        {
            playerAudioSource.PlayOneShot(beep2Clip);
            yield return new WaitForSeconds(beep2Clip.length + blackoutDelayAfterBeep2);
        }

        HUDManager.Instance?.ShowBlackout();

        yield return new WaitForSeconds(reloadDelay);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
