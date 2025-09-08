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

    // 👇 accessor pubblici sicuri
    public PickupObject HeldPickup => heldPickup;
    public GameObject HeldObject => heldObject;

    private void Awake()
    {
        playerCamera = Camera.main;
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

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // ignora muri / blocchi
            if (hitObject.layer == LayerMask.NameToLayer("Default"))
                continue;   // <<--- PRIMA era return

            // ignora l’oggetto che stiamo già tenendo in mano
            if (heldObject != null &&
                (hitObject == heldObject || hitObject.transform.IsChildOf(heldObject.transform)))
                continue;

            // controlla se appartiene ai layer interagibili
            if ((interactableLayer.value & (1 << hitObject.layer)) == 0)
                continue;

            // se ha un IInteractable valido → è il target
            var interactable = hitObject.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = hitObject;
                currentInteractable = interactable;
                return;
            }
        }
    }


    // ---------- CORE ----------
    void HandleInteraction()
    {
        // Caso speciale: sei già nello spioncino
        var door = FindObjectOfType<RoomDoor>();
        if (door != null && door.IsLookingThroughPeephole)
        {
            door.InteractWithPeephole();
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
