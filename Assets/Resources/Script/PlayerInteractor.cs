using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;
    public Transform handPivot;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask interactableLayer;

    private GameObject heldObject;
    private PickupObject heldPickup;

    // 👇 Oggetto attualmente nel mirino
    public GameObject currentTarget { get; private set; }
    public InteractableName currentTargetName { get; private set; }

    void Update()
    {
        UpdateRaycastTarget();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
                TryInteractOrPickUp();
            else
                TryUseHeldObject();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropHeld();
        }
    }

    void UpdateRaycastTarget()
    {
        currentTarget = null;
        currentTargetName = null;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance, interactableLayer))
        {
            currentTarget = hit.collider.gameObject;
            currentTargetName = currentTarget.GetComponent<InteractableName>();
        }
    }

    void TryInteractOrPickUp()
    {
        if (currentTarget == null)
            return;

        PickupObject pickup = currentTarget.GetComponent<PickupObject>();
        if (pickup != null && pickup.canBePickedUp)
        {
            PickUp(pickup);
            return;
        }

        // Altri tipi di interazione futura
    }

    void TryUseHeldObject()
    {
        if (currentTarget == null || heldPickup == null) return;

        IPlaceableReceiver receiver = currentTarget.GetComponent<IPlaceableReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            heldObject = null;
            heldPickup = null;
            return;
        }

        // Potresti gestire anche altri casi futuri (es: interazione tra oggetti)
    }


void PickUp(PickupObject pickup)
{
    // 🔄 Se era piazzato in un receiver, liberalo
    if (pickup.currentReceiver != null)
    {
        pickup.currentReceiver.Unplace();
        pickup.currentReceiver = null;
    }

    heldObject = pickup.gameObject;
    heldPickup = pickup;
    pickup.PickUp(handPivot);
}


    void DropHeld()
    {
        if (heldPickup != null)
        {
            heldPickup.Drop();
        }

        heldObject = null;
        heldPickup = null;
    }

    public bool IsHoldingObject()
    {
        return heldObject != null;
    }

    public void ClearHeld()
    {
        if (heldPickup != null)
        {
            heldPickup.isHeld = false;
        }

        heldObject = null;
        heldPickup = null;
    }

}
