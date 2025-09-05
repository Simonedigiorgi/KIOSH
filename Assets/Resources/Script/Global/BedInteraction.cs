using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class BedInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sleepPoint;             // Punto dove il player viene teletrasportato
    [SerializeField] private PlayableDirector wakeUpTimeline;  // Timeline del risveglio

    [Header("Config")]
    [SerializeField] private float fadeDuration = 2f;   // Tempo del blackout (fade in)
    [SerializeField] private float sleepDuration = 3f;  // Quanto dura il "sonno" fittizio

    private bool isUsed = false;

    public void UseBed(PlayerController player)
    {
        var tm = TimerManager.Instance;
        if (tm == null || !tm.DayCompleted)   // ✅ Usa solo il flag DayCompleted
        {
            Debug.Log("[BedInteraction] Non puoi dormire: giornata non conclusa.");
            return;
        }

        if (isUsed) return;
        isUsed = true;

        // Blocca i controlli
        if (player != null) player.SetControlsEnabled(false);

        StartCoroutine(SleepSequence(player));
    }

    private IEnumerator SleepSequence(PlayerController player)
    {
        // 1. Fade in del nero
        if (HUDManager.Instance != null)
            yield return HUDManager.Instance.StartCoroutine(HUDManager.Instance.FadeBlackout(fadeDuration));
        else
            yield return new WaitForSeconds(fadeDuration);

        // 2. Teletrasporto del player mentre lo schermo è nero
        if (player != null && sleepPoint != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = sleepPoint.position;
            player.transform.rotation = sleepPoint.rotation;

            // ✅ Resetta la camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.localRotation = Quaternion.identity; // resetta rotazione (0,0,0)
            }

            // Se vuoi resettare anche il pitch del PlayerController
            player.ResetCameraRotation();

            if (cc != null) cc.enabled = true;
        }

        // 3. Attesa del "sonno"
        yield return new WaitForSeconds(sleepDuration);

        // 4. Spegni subito il pannello nero
        if (HUDManager.Instance?.blackoutPanel != null)
            HUDManager.Instance.blackoutPanel.SetActive(false);

        // 5. Avvia la timeline di risveglio
        if (wakeUpTimeline != null)
        {
            wakeUpTimeline.Play();
            Debug.Log("[BedInteraction] Giorno terminato → avvio risveglio.");
        }

        // 6. Riattiva i controlli
        if (player != null)
            player.SetControlsEnabled(true);
    }
}
