using UnityEngine;

public class BulletinInteraction : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public Transform cameraTargetPosition;
    public BulletinController bulletinController;
    public CameraInteractor cameraInteractor;

    [Header("Input Gate")]
    public float reopenCooldown = 0.20f;
    private float reopenBlockUntil = 0f;

    private bool isInteracting = false;

    // ---------- IInteractable ----------
    public void Interact(PlayerInteractor interactor)
    {
        if (!isInteracting)
            EnterInteraction();
        // 👉 se già dentro, ignora (non uscire!)
    }

    // ---------- Interazione ----------
    public void EnterInteraction()
    {
        if (Time.time < reopenBlockUntil) return;
        if (isInteracting) return;

        if (!cameraTargetPosition || !bulletinController || !cameraInteractor)
        {
            Debug.LogError("[BulletinInteraction] Riferimenti mancanti.");
            return;
        }

        // 🔒 Blocchiamo subito: la transizione è in corso
        isInteracting = true;

        cameraInteractor.EnterInteraction(
            cameraTargetPosition,
            onComplete: () =>
            {
                bulletinController.EnterInteraction(this);

                // Nascondi HUD
                HUDManager.Instance?.SetInteracting(true);
            }
        );
    }


    public void ExitInteraction()
    {
        if (!isInteracting) return;

        cameraInteractor.ExitInteraction(
            onComplete: () =>
            {
                isInteracting = false;
                reopenBlockUntil = Time.time + reopenCooldown;

                // Ripristina HUD
                HUDManager.Instance?.SetInteracting(false);
            }
        );
    }
}
