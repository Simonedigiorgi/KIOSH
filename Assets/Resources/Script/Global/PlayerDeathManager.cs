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

    void Awake()
    {
        // Auto-riaggancio al cambio scena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolvePlayerAudioSource();
    }

    void OnEnable()
    {
        TimerManager.OnTimerCompletedGlobal += HandleTimerCompleted;
        TimerManager.OnReentryCompletedGlobal += HandleReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;

        // Nel caso venga abilitato dopo il reload
        ResolvePlayerAudioSource();
    }

    void OnDisable()
    {
        TimerManager.OnTimerCompletedGlobal -= HandleTimerCompleted;
        TimerManager.OnReentryCompletedGlobal -= HandleReentryCompleted;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;
    }

    private void ResolvePlayerAudioSource()
    {
        if (playerAudioSource != null) return;

        AudioSource found = null;

        // 1) Player con tag
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            found = player.GetComponentInChildren<AudioSource>(true);

        // 2) PlayerController in scena
        if (found == null)
        {
            var pc = FindObjectOfType<PlayerController>(true);
            if (pc != null) found = pc.GetComponentInChildren<AudioSource>(true);
        }

        // 3) Fallback: AudioSource sulla camera principale
        if (found == null && Camera.main != null)
            found = Camera.main.GetComponent<AudioSource>();

        if (found != null)
        {
            playerAudioSource = found;
        }
        else
        {
            Debug.LogWarning("[PlayerDeathManager] Nessun AudioSource del player trovato in scena.");
        }
    }

    private void HandleAllDeliveriesCompleted()
    {
        allDelivered = true;
        Debug.Log("[PlayerDeathManager] Tutte le consegne completate (TimerManager gestisce delay e reentry).");
    }

    private void HandleTimerCompleted()
    {
        if (allDelivered)
        {
            // TimerManager aprira la porta e avviera il reentry dopo il delay configurato.
            return;
        }

        // Consegne non completate -> punizione immediata
        StartCoroutine(PunishmentSequence());
    }

    private void HandleReentryCompleted()
    {
        var tm = TimerManager.Instance;
        if (tm != null && !tm.IsPlayerInsideRoom)
            StartCoroutine(PunishmentSequence());
    }

    private IEnumerator PunishmentSequence()
    {
        // Assicurati di avere l'audio source anche dopo il reload
        if (playerAudioSource == null) ResolvePlayerAudioSource();

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

        // Reset stato globale prima del reload
        TimerManager.Instance?.ResetToIdle();
        DeliveryBox.TotalDelivered = 0;

        yield return new WaitForSeconds(reloadDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
