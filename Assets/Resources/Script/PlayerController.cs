using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    private bool controlsEnabled = true;   // 👈 nuovo

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Blocca il cursore al centro dello schermo
        Cursor.lockState = CursorLockMode.Locked;

        // Ignora collisioni tra il Player (es. Layer "Player") e Interactable
        int playerLayer = LayerMask.NameToLayer("Player");
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        Physics.IgnoreLayerCollision(playerLayer, interactableLayer, true);
    }


    void Update()
    {
        if (!controlsEnabled) return;   // 👈 blocca del tutto movimento+look quando sei in board

        HandleMovement();
        HandleCameraRotation();
        ApplyGravity();
    }

    // Gestisce il movimento del personaggio con WASD
    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);
    }

    // Gestisce la rotazione della camera con il mouse
    void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotazione verticale (guardare su/giù)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotazione orizzontale (gira il corpo)
        transform.Rotate(Vector3.up * mouseX);
    }

    // Applica la gravità al personaggio
    void ApplyGravity()
    {
        // Controlla se il personaggio è a terra
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            // Reimposta la velocità verticale se a terra
            velocity.y = -2f;
        }

        // Applica gravità
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }


    public void SetControlsEnabled(bool enabled)  // 👈 nuovo
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            // opzionale: reset velocità per evitare “strascico”
            velocity = Vector3.zero;
        }
    }
}
