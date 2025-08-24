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

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Blocca oggetti non nel layer giusto
            if (((1 << hitObject.layer) & interactableLayer) == 0)
                return;

            // 1. Prova a colpire un Pickup
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

        // 1. Verifica se è un PickupObject (padella, pacco, ingrediente, ecc.)
        PickupObject pickup = currentTarget.GetComponent<PickupObject>();
        if (pickup == null)
            pickup = currentTarget.GetComponentInParent<PickupObject>();

        // 2. Verifica se è un pacco
        var package = pickup != null ? pickup.GetComponent<PackageBox>() : null;

        if (package != null)
        {
            if (package.isPlaced)
            {
                // Se il pacco è stato posizionato → prova a consegnare ingrediente
                package.TryDeliver(this);
                return;
            }
            else
            {
                // Se il pacco NON è stato posizionato → si comporta come un normale pickup
                if (pickup.canBePickedUp)
                {
                    PickUp(pickup);
                    return;
                }
            }
        }

        // 3. Altri oggetti pickup normali (non pacchi)
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


    bool IsPackagePlaced(PackageBox box)
    {
        return box != null && box.isPlaced;
    }


    void TryUseHeldObject()
    {
        if (currentTarget == null || heldPickup == null) return;

        // 1. Cucinare
        Cookware cookware = currentTarget.GetComponent<Cookware>();
        if (cookware != null)
        {
            if (cookware.TryAddIngredient(heldPickup))
            {
                ClearHeld();
                return;
            }
        }

        // 2. Piazzare
        ObjectReceiver receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            ClearHeld();
            return;
        }

        // Aggiungi altri casi d’uso qui
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
}
