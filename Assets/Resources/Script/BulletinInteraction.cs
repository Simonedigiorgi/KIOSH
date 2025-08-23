using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    public Transform cameraTargetPosition;     // Posizione davanti al monitor
    public Transform playerCamera;             // Camera del player
    public float cameraMoveSpeed = 5f;
    public CrosshairManager crosshairManager;
    public BulletinController bulletinController;

    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    private bool isInteracting = false;
    private bool hasEntered = false;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Premi E per entrare
        if (!isInteracting && Input.GetKeyDown(KeyCode.E) && IsLookingAtScreen())
        {
            EnterInteraction();
        }

        // Premi ESC per uscire
        if (isInteracting && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitInteraction();
        }
    }

    void EnterInteraction()
    {
        if (hasEntered) return;

        isInteracting = true;
        hasEntered = true;

        // Salva posizione camera
        originalCamPosition = playerCamera.position;
        originalCamRotation = playerCamera.rotation;

        // Blocca movimento player
        FindObjectOfType<PlayerController>().enabled = false;
        crosshairManager.SetInteracting(true);

        // Sposta camera davanti al pannello
        playerCamera.position = cameraTargetPosition.position;
        playerCamera.rotation = cameraTargetPosition.rotation;

        // Blocca il cursore (non visibile, ma disattiva movimenti inutili)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ⬇️ Questa riga era mancante!
        bulletinController.EnterInteraction(); // Attiva la logica interna
    }


    public void ExitInteraction()
    {
        isInteracting = false;
        hasEntered = false;

        // Ripristina camera e movimento
        playerCamera.position = originalCamPosition;
        playerCamera.rotation = originalCamRotation;
        FindObjectOfType<PlayerController>().enabled = true;
        crosshairManager.SetInteracting(false);

        // Blocca il cursore di nuovo
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reimposta il pannello alla sua schermata iniziale
        bulletinController.ForceBackToIntro();
    }

    // Controlla se il player guarda verso il pannello (puoi sostituire con raycast o trigger)
    bool IsLookingAtScreen()
    {
        // In alternativa, puoi aggiungere un trigger collider e usare un flag esterno
        return true; // fallback semplificato
    }

}
