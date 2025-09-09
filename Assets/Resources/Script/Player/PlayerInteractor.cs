using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    public Transform handPivot;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Perf")]
    [Tooltip("How often to update aim raycast. Set to 0 to only raycast on E.")]
    [Range(0f, 30f)] public float raycastHz = 10f; // 0 = only on demand

    private float _nextRaycastTime = 0f;

    private Camera playerCamera;
    private GameObject heldObject;
    private PickupObject heldPickup;

    public GameObject currentTarget { get; private set; }
    public IInteractable currentInteractable { get; private set; }

    public PickupObject HeldPickup => heldPickup;
    public GameObject HeldObject => heldObject;

    // Reusable buffer
    private static readonly RaycastHit[] hitsBuffer = new RaycastHit[12];

    // Cached door
    [SerializeField] private string doorTag = "RoomDoor";
    private RoomDoor cachedDoor;

    private void Awake()
    {
        playerCamera = Camera.main;
        CacheDoor();
    }

    private void CacheDoor()
    {
        if (cachedDoor) return;
        if (!string.IsNullOrEmpty(doorTag))
        {
            var go = GameObject.FindWithTag(doorTag);
            if (go) cachedDoor = go.GetComponent<RoomDoor>();
        }
        if (!cachedDoor) cachedDoor = FindObjectOfType<RoomDoor>(); // one-shot fallback
    }

    void Update()
    {
        if (HUDManager.Instance != null && HUDManager.Instance.IsDialogOpen)
            return;

        // Throttled aim raycast (optional)
        if (raycastHz > 0f && Time.time >= _nextRaycastTime)
        {
            UpdateRaycastTargetInternal();
            _nextRaycastTime = Time.time + 1f / raycastHz;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Force a fresh raycast just before interacting
            if (raycastHz == 0f) UpdateRaycastTargetInternal();

            HandleInteraction();
        }

        if (Input.GetKeyDown(KeyCode.Q))
            DropHeld();
    }

    // ---------- TARGET SELECTION ----------
    private void UpdateRaycastTargetInternal()
    {
        currentTarget = null;
        currentInteractable = null;

        var cam = playerCamera;
        if (!cam) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        int count = Physics.RaycastNonAlloc(ray, hitsBuffer, interactDistance, interactableLayer, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        IInteractable bestInteractable = null;
        GameObject bestTarget = null;

        for (int i = 0; i < count; i++)
        {
            var h = hitsBuffer[i];
            var go = h.collider ? h.collider.gameObject : null;
            if (!go) continue;

            // ignore held object
            if (heldObject != null && (go == heldObject || go.transform.IsChildOf(heldObject.transform)))
                continue;

            IInteractable it = go.GetComponent<IInteractable>();
            if (it == null) it = go.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestInteractable = it;
                bestTarget = go;
            }
        }

        if (bestInteractable != null)
        {
            currentInteractable = bestInteractable;
            currentTarget = bestTarget;
        }
    }

    // ---------- CORE ----------
    private void HandleInteraction()
    {
        // Peephole special case
        if (!cachedDoor) CacheDoor();
        if (cachedDoor != null && cachedDoor.IsLookingThroughPeephole)
        {
            cachedDoor.InteractWithPeephole();
            return;
        }

        if (!currentTarget || currentInteractable == null)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        currentInteractable.Interact(this);
    }

    // ---------- HELD ----------
    public void PickUp(PickupObject pickup)
    {
        heldObject = pickup.gameObject;
        heldPickup = pickup;
        pickup.PickUp(handPivot);
    }

    private void DropHeld()
    {
        if (heldPickup) heldPickup.Drop();
        ClearHeld();
    }

    public void ClearHeld()
    {
        if (heldPickup) heldPickup.isHeld = false;
        heldObject = null;
        heldPickup = null;
    }

    public bool IsHoldingObject() => heldObject != null;

    public void ReceiveExternalPickup(PickupObject pickup)
    {
        heldObject = pickup.gameObject;
        heldPickup = pickup;
    }
}
