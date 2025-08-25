using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    public Transform cameraTargetPosition;
    public Transform playerCamera;
    public float cameraMoveSpeed = 5f;
    public CrosshairManager crosshairManager;
    public BulletinController bulletinController;

    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    private bool isInteracting = false;
    private bool hasEntered = false;

    private PlayerInteractor playerInteractor;

    void Start()
    {
        playerInteractor = FindObjectOfType<PlayerInteractor>();
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!isInteracting && Input.GetKeyDown(KeyCode.E) && IsLookingAtPanel())
        {
            if (!playerInteractor.IsHoldingObject())
            {
                EnterInteraction();
            }
        }

        if (isInteracting && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitInteraction();
        }
    }

    bool IsLookingAtPanel()
    {
        return playerInteractor.currentTarget == this.gameObject;
    }

    void EnterInteraction()
    {
        if (hasEntered) return;

        isInteracting = true;
        hasEntered = true;

        originalCamPosition = playerCamera.position;
        originalCamRotation = playerCamera.rotation;

        FindObjectOfType<PlayerController>().enabled = false;
        crosshairManager.SetInteracting(true);

        playerCamera.position = cameraTargetPosition.position;
        playerCamera.rotation = cameraTargetPosition.rotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bulletinController.EnterInteraction();
    }

    public void ExitInteraction()
    {
        isInteracting = false;
        hasEntered = false;

        playerCamera.position = originalCamPosition;
        playerCamera.rotation = originalCamRotation;

        FindObjectOfType<PlayerController>().enabled = true;
        crosshairManager.SetInteracting(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bulletinController.ForceBackToIntro();
    }
}
