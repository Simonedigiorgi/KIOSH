using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class BedInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sleepPoint;
    [SerializeField] private PlayableDirector wakeUpTimeline;

    [Header("Config")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float sleepDuration = 3f;

    private bool isUsed = false;

    public void UseBed(PlayerController player)
    {
        var tm = TimerManager.Instance;
        var gs = GameStateManager.Instance;

        // ✅ Se è mattina, puoi dormire solo se la giornata è conclusa
        if (gs != null && gs.CurrentPhase == DayPhase.Morning)
        {
            if (tm == null || !tm.DayCompleted)
            {
                HUDManager.Instance.ShowDialog("You cannot sleep: day not completed.");
                Debug.Log("[BedInteraction] Non puoi dormire: giornata non conclusa.");
                return;
            }
        }
        // ✅ Se è notte, permetti sempre di dormire
        else if (gs != null && gs.CurrentPhase == DayPhase.Night)
        {
            Debug.Log("[BedInteraction] È notte, il letto è sempre usabile.");
        }

        if (isUsed) return;

        if (player != null) player.SetControlsEnabled(false);

        StartCoroutine(SleepSequence(player));
    }

    private IEnumerator SleepSequence(PlayerController player)
    {
        isUsed = true; // 👈 marcato qui all’avvio della sequenza

        Debug.Log("[BedInteraction] SleepSequence avviata");

        // 1. Fade in nero
        if (HUDManager.Instance != null)
        {
            Debug.Log("[BedInteraction] Avvio fade blackout");
            yield return HUDManager.Instance.StartCoroutine(HUDManager.Instance.FadeBlackout(fadeDuration));
            Debug.Log("[BedInteraction] Fine fade blackout");
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        // 2. Teletrasporto
        Debug.Log("[BedInteraction] Teletrasporto player");
        if (player != null && sleepPoint != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = sleepPoint.position;
            player.transform.rotation = sleepPoint.rotation;

            var cam = Camera.main;
            if (cam != null)
                cam.transform.localRotation = Quaternion.identity;
            player.ResetCameraRotation();

            if (cc != null) cc.enabled = true;
        }

        // 3. Sonno fittizio
        Debug.Log("[BedInteraction] Attesa sonno");
        yield return new WaitForSeconds(sleepDuration);

        // 4. Spegni pannello nero
        Debug.Log("[BedInteraction] Spengo blackoutPanel");
        if (HUDManager.Instance?.blackoutPanel != null)
            HUDManager.Instance.blackoutPanel.SetActive(false);

        // 5. Avanza fase
        var gs = GameStateManager.Instance;
        if (gs != null)
        {
            Debug.Log($"[BedInteraction] Prima di AdvancePhase: Giorno {gs.CurrentDay}, Fase {gs.CurrentPhase}");
            gs.AdvancePhase();
            Debug.Log($"[BedInteraction] Dopo AdvancePhase: Giorno {gs.CurrentDay}, Fase {gs.CurrentPhase}");
        }

        // 6. Cutscene risveglio
        if (wakeUpTimeline != null)
        {
            wakeUpTimeline.Play();
            Debug.Log("[BedInteraction] Avvio risveglio (timeline)");
        }

        // 7. Riattiva controlli
        if (player != null)
            player.SetControlsEnabled(true);

        Debug.Log("[BedInteraction] Sequenza completata");

        isUsed = false; // 👈 reset così puoi riusare il letto al prossimo ciclo
    }
}
