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
        if (playerCamera == null || cameraTargetPosition == null || playerController == null || bulletinController == null)
        {
            Debug.LogError("[BulletinInteraction] Riferimenti mancanti: assegna Camera/Target/PlayerController/BulletinController.");
            return;
        }

        // 1) disabilita davvero il controller PRIMA di toccare la camera
        playerController.SetControlsEnabled(false);
        if (crosshairManager != null) crosshairManager.SetInteracting(true);

        // 2) salva parent+posa locali
        originalCamParent = playerCamera.transform.parent;
        originalLocalPos = playerCamera.transform.localPosition;
        originalLocalRot = playerCamera.transform.localRotation;

        // 3) re-parent al target e azzera local pose → combacia al millimetro
        playerCamera.transform.SetParent(cameraTargetPosition, false);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;

        // 4) avvia UI (passiamo chi ha aperto, così la UI può richiudere correttamente)
        bulletinController.EnterInteraction(this);

        // 1) costruisci il menù in base allo stato attuale del box
        var adapter = GetComponentInChildren<DeliveryBulletinAdapter>(true);
        if (adapter != null) adapter.RefreshMenu();

        // 2) poi apri la UI
        bulletinController.EnterInteraction(this);

        isInteracting = true;

        isInteracting = true;
    }

    public void ExitInteraction()
    {
        if (!isInteracting) return;

        // 1) ripristina parent+posa locale della camera
        playerCamera.transform.SetParent(originalCamParent, false);
        playerCamera.transform.localPosition = originalLocalPos;
        playerCamera.transform.localRotation = originalLocalRot;

        // 2) riabilita il controller DOPO aver ripristinato la camera
        playerController.SetControlsEnabled(true);
        if (crosshairManager != null) crosshairManager.SetInteracting(false);

        isInteracting = false;
    }
}
