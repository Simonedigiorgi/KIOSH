using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Gravity")]
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private Animator animator; // 👈 Aggancia qui l'Animator del Model

    private Transform cameraTransform;
    private CharacterController controller;

    private float xRotation = 0f;
    private Vector3 velocity;
    private bool controlsEnabled = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!controlsEnabled) return;

        HandleMovement();
        ApplyGravity();
    }

    void LateUpdate()
    {
        if (!controlsEnabled) return;

        HandleCameraRotation();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;
        controller.Move(move * speed * Time.deltaTime);

        // ---- Drive Animator con velocità planare ----
        if (animator != null)
        {
            Vector3 planar = controller.velocity;
            planar.y = 0f;
            float planarSpeed = planar.magnitude;

            animator.SetFloat("Speed", planarSpeed, 0.1f, Time.deltaTime); // damping per fluidità
        }
    }

    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // piccolo offset per restare grounded

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        Debug.Log("Controls enabled = " + controlsEnabled);
        if (!enabled)
            velocity = Vector3.zero;
    }

    /// <summary>
    /// Allinea lo stato interno (xRotation) con la rotazione attuale della camera.
    /// Da chiamare dopo una cutscene per evitare salti.
    /// </summary>
    public void SyncCameraRotation()
    {
        if (cameraTransform == null) return;

        Vector3 camAngles = cameraTransform.localEulerAngles;
        xRotation = camAngles.x;

        // Debug per conferma
        Debug.Log("[PlayerController] SyncCameraRotation → xRotation = " + xRotation);
    }

    public void ResetCameraRotation()
    {
        xRotation = 0f; // resetta il pitch interno
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

}
