using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTargetPosition;
    public Camera playerCamera;
    public CrosshairManager crosshairManager;
    public BulletinController bulletinController;
    public PlayerController playerController;

    // Backup camera
    private Transform originalCamParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private bool isInteracting = false;

    // 🔒 Anti-rientro immediato
    [Header("Input Gate")]
    [Tooltip("Tempo minimo dopo l’uscita prima di poter rientrare")]
    public float reopenCooldown = 0.20f;   // 200 ms
    private float reopenBlockUntil = 0f;

    public void EnterInteraction()
    {
        // ⛔ blocca se siamo nel cooldown post-uscita
        if (Time.time < reopenBlockUntil) return;
        if (isInteracting) return;

        if (playerCamera == null || cameraTargetPosition == null ||
            playerController == null || bulletinController == null)
        {
            Debug.LogError("[BulletinInteraction] Riferimenti mancanti.");
            return;
        }

        // Disabilita controlli prima di toccare la camera
        playerController.SetControlsEnabled(false);
        if (crosshairManager != null) crosshairManager.SetInteracting(true);

        // Salva parent+posa
        originalCamParent = playerCamera.transform.parent;
        originalLocalPos = playerCamera.transform.localPosition;
        originalLocalRot = playerCamera.transform.localRotation;

        // Reparent & snap
        playerCamera.transform.SetParent(cameraTargetPosition, false);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;

        // Apri UI
        bulletinController.EnterInteraction(this);

        // Se vuoi, qui puoi fare refresh dell’adapter (non necessario per l’uscita)
        var adapter = GetComponentInChildren<DeliveryBulletinAdapter>(true);
        if (adapter != null) adapter.RefreshMenu();

        isInteracting = true;
    }

    public void ExitInteraction()
    {
        if (!isInteracting) return;

        // Ripristina camera
        playerCamera.transform.SetParent(originalCamParent, false);
        playerCamera.transform.localPosition = originalLocalPos;
        playerCamera.transform.localRotation = originalLocalRot;

        // Riabilita controlli
        playerController.SetControlsEnabled(true);
        if (crosshairManager != null) crosshairManager.SetInteracting(false);

        isInteracting = false;

        // ⏱️ imposta il blocco per evitare la riapertura immediata
        reopenBlockUntil = Time.time + reopenCooldown;
    }
}
