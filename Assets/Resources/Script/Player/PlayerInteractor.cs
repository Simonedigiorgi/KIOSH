using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    public Transform handPivot;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private Camera playerCamera;
    private GameObject heldObject;
    private PickupObject heldPickup;

    public GameObject currentTarget { get; private set; }
    public IInteractable currentInteractable { get; private set; }

    // accessor pubblici sicuri
    public PickupObject HeldPickup => heldPickup;
    public GameObject HeldObject => heldObject;

    // Buffer riusabile per raycast
    private static readonly RaycastHit[] hitsBuffer = new RaycastHit[12];

    // Cache porta per evitare Find ad ogni pressione
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

        if (!cachedDoor)
            cachedDoor = FindObjectOfType<RoomDoor>(); // fallback one-shot
    }

    void Update()
    {
        if (HUDManager.Instance != null && HUDManager.Instance.IsDialogOpen)
            return;

        UpdateRaycastTarget();

        if (Input.GetKeyDown(KeyCode.E))
            HandleInteraction();

        if (Input.GetKeyDown(KeyCode.Q))
            DropHeld();
    }

    // ---------- TARGET SELECTION ----------
    void UpdateRaycastTarget()
    {
        currentTarget = null;
        currentInteractable = null;

        var cam = playerCamera;
        if (!cam) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // Raycast filtrato per layer (niente allocazioni)
        int count = Physics.RaycastNonAlloc(ray, hitsBuffer, interactDistance, interactableLayer, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        IInteractable bestInteractable = null;
        GameObject bestTarget = null;

        for (int i = 0; i < count; i++)
        {
            var h = hitsBuffer[i];
            var go = h.collider ? h.collider.gameObject : null;
            if (!go) continue;

            // ignora oggetto in mano
            if (heldObject != null && (go == heldObject || go.transform.IsChildOf(heldObject.transform)))
                continue;

            // prova a prendere IInteractable rapidamente
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
    void HandleInteraction()
    {
        // Caso speciale: spioncino
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

    void DropHeld()
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
