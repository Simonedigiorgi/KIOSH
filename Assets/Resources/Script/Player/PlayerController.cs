using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Mouse Look")]
    [SerializeField, Tooltip("1..10 consigliato")] private float mouseSensitivity = 3f;
    [SerializeField] private float mouseSmoothing = 0.05f;  // 0 = immediato, 0.03..0.08 morbido
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private Transform cam;
    private CharacterController controller;

    private float xRotation;           // pitch
    private Vector3 velocity;          // solo Y per gravità
    private bool controlsEnabled = true;

    // smoothing del mouse
    private Vector2 mouseDeltaCurrent;
    private Vector2 mouseDeltaVel;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private const float MOUSE_BASE = 0.02f; // fattore che rende “normale” la scala della sens

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main ? Camera.main.transform : null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!controlsEnabled) return;

        HandleLook();
        HandleMoveAndGravity();
    }

    private void HandleLook()
    {
        if (!cam) return;

        // Raw (niente smoothing Unity) → poi ammorbidiamo noi
        float rawX = Input.GetAxisRaw("Mouse X");
        float rawY = Input.GetAxisRaw("Mouse Y");

        // Scala “umana”: mouseSensitivity 1..10
        Vector2 target = new Vector2(rawX, rawY) * mouseSensitivity * MOUSE_BASE;

        // Ammorbidisci picchi
        mouseDeltaCurrent = Vector2.SmoothDamp(mouseDeltaCurrent, target, ref mouseDeltaVel, mouseSmoothing);

        // Pitch camera (inverti se vuoi)
        xRotation = Mathf.Clamp(xRotation - mouseDeltaCurrent.y, minLookAngle, maxLookAngle);
        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw player
        transform.Rotate(Vector3.up * mouseDeltaCurrent.x);
    }

    private void HandleMoveAndGravity()
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * ix + transform.forward * iz);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        Vector3 horizontal = inputDir * speed;

        // Gravità con “stick” al terreno
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += Physics.gravity.y * Time.deltaTime;

        // Una sola Move
        Vector3 motion = (horizontal + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime;
        controller.Move(motion);

        // Animator: velocità planare desiderata
        if (animator)
        {
            float planarSpeed = horizontal.magnitude; // m/s
            animator.SetFloat(SpeedHash, planarSpeed, 0.1f, Time.deltaTime);
        }
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            velocity = Vector3.zero;
            mouseDeltaCurrent = Vector2.zero;
            mouseDeltaVel = Vector2.zero;
        }
    }

    public void SyncCameraRotation()
    {
        if (!cam) return;
        float nx = cam.localEulerAngles.x;
        if (nx > 180f) nx -= 360f;
        xRotation = Mathf.Clamp(nx, minLookAngle, maxLookAngle);
    }

    public void ResetCameraRotation()
    {
        xRotation = 0f;
        if (cam) cam.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }
}
