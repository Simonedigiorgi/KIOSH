using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTargetPosition; // pivot davanti al monitor
    public Camera playerCamera;            // Main Camera
    public CrosshairManager crosshairManager;
    public BulletinController bulletinController;
    public PlayerController playerController;

    // Backup camera (parent + posa locale)
    private Transform originalCamParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private bool isInteracting = false;

    public void EnterInteraction()
    {
        if (isInteracting) return;
        if (!playerCamera || !cameraTargetPosition || !playerController || !bulletinController)
        {
            Debug.LogError("[BulletinInteraction] Riferimenti mancanti.");
            return;
        }

        // disattiva controlli prima di muovere la camera
        playerController.SetControlsEnabled(false);
        if (crosshairManager) crosshairManager.SetInteracting(true);

        // backup
        originalCamParent = playerCamera.transform.parent;
        originalLocalPos = playerCamera.transform.localPosition;
        originalLocalRot = playerCamera.transform.localRotation;

        // reparent e azzera pose
        playerCamera.transform.SetParent(cameraTargetPosition, false);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;

        // apri UI
        bulletinController.EnterInteraction(this);

        // popola/refresh menu (delivery adapter, se presente)
        var adapter = GetComponentInChildren<DeliveryBulletinAdapter>(true);
        if (adapter) adapter.RefreshMenu();

        isInteracting = true;
    }

    public void ExitInteraction()
    {
        if (!isInteracting) return;

        // ripristina camera
        playerCamera.transform.SetParent(originalCamParent, false);
        playerCamera.transform.localPosition = originalLocalPos;
        playerCamera.transform.localRotation = originalLocalRot;

        // riattiva controlli
        playerController.SetControlsEnabled(true);
        if (crosshairManager) crosshairManager.SetInteracting(false);

        isInteracting = false;
    }
}
