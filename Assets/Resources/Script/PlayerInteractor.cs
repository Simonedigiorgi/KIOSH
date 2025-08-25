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
            GameObject hitObject = hit.collider.gameObject;

            var pickup = hitObject.GetComponentInParent<PickupObject>();
            if (pickup != null)
            {
                currentTarget = pickup.gameObject;
                currentTargetName = pickup.GetComponent<InteractableName>();
            }
            else
            {
                currentTarget = hitObject;
                currentTargetName = hitObject.GetComponent<InteractableName>();
            }
        }
    }

    void TryInteractOrPickUp()
    {
        if (currentTarget == null)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        // Se è un pacco
        PackageBox box = currentTarget.GetComponent<PackageBox>();
        if (box != null)
        {
            if (box.isPlaced)
            {
                box.TryDeliver(this);
                return;
            }
        }

        // Se è un oggetto prendibile
        PickupObject pickup = currentTarget.GetComponent<PickupObject>();
        if (pickup == null)
            pickup = currentTarget.GetComponentInParent<PickupObject>();

        if (pickup != null)
        {
            if (pickup.canBePickedUp)
            {
                PickUp(pickup);
                return;
            }
            else
            {
                Debug.Log("⚠️ Oggetto non raccoglibile al momento.");
            }
        }
        else
        {
            Debug.Log("❌ Oggetto non ha PickupObject.");
        }
    }

    void TryUseHeldObject()
    {
        if (currentTarget == null || heldPickup == null) return;

        // 🔄 Chiama InteractWith() se il pickup lo supporta
        if (heldPickup.InteractWith(currentTarget))
        {
            return;
        }

        // 🥩 Caso classico: ingrediente da mettere in cookware
        if (heldPickup.type == PickupType.Ingredient)
        {
            Cookware cookware = currentTarget.GetComponent<Cookware>();
            if (cookware != null && cookware.TryAddIngredient(heldPickup))
            {
                ClearHeld();
                return;
            }
        }

        // 🔄 Altro receiver (es. tavolo, contenitori)
        ObjectReceiver receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            ClearHeld();
            return;
        }
    }


    public void PickUp(PickupObject pickup)
    {
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

        ClearHeld();
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

    public bool IsHoldingObject()
    {
        return heldObject != null;
    }

    public void ReceiveExternalPickup(PickupObject pickup)
    {
        heldObject = pickup.gameObject;
        heldPickup = pickup;
    }
}
