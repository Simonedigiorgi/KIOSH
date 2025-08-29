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
            HandleInteraction(IsHoldingObject());

        if (Input.GetKeyDown(KeyCode.Q))
            DropHeld();
    }

    void UpdateRaycastTarget()
    {
        currentTarget = null;
        currentTargetName = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactableLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // ignora l’oggetto che ho in mano
            if (heldObject != null &&
                (hitObject == heldObject || hitObject.transform.IsChildOf(heldObject.transform)))
                continue;

            // 1) PackageBox
            var package = hitObject.GetComponentInParent<PackageBox>();
            if (package != null)
            {
                currentTarget = package.gameObject;
                currentTargetName = package.GetComponent<InteractableName>();
                return;
            }

            // 2) PickupObject
            var pickup = hitObject.GetComponentInParent<PickupObject>();
            if (pickup != null)
            {
                currentTarget = pickup.gameObject;
                currentTargetName = pickup.GetComponent<InteractableName>();
                return;
            }

            // 3) BulletinInteraction (BOARD)
            var board = hitObject.GetComponentInParent<BulletinInteraction>();
            if (board != null)
            {
                currentTarget = board.gameObject;
                currentTargetName = board.GetComponent<InteractableName>();
                return;
            }

            // 4) ObjectReceiver (plate position o altri receiver)
            var receiver = hitObject.GetComponentInParent<ObjectReceiver>();
            if (receiver != null)
            {
                currentTarget = receiver.gameObject;
                currentTargetName = receiver.GetComponent<InteractableName>();
                return;
            }

            // 5) DeliveryBox (per lo sportello)
            var box = hitObject.GetComponentInParent<DeliveryBox>();
            if (box != null)
            {
                currentTarget = hitObject; // qui teniamo proprio lo sportello
                currentTargetName = hitObject.GetComponent<InteractableName>();
                return;
            }

            // fallback
            currentTarget = hitObject;
            currentTargetName = hitObject.GetComponent<InteractableName>();
            return;
        }
    }

    // ---------- CORE ----------
    void HandleInteraction(bool isHolding)
    {
        if (currentTarget == null)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        // 0) BOARD
        var bulletin = currentTarget.GetComponentInParent<BulletinInteraction>();
        if (bulletin != null)
        {
            bulletin.EnterInteraction();
            return;
        }

        // 1) Sportello DeliveryBox
        if (TryToggleDeliveryDoor()) return;

        // 2) Dispenser piatti
        if (!isHolding && TryUseDishDispenser()) return;

        // 3) PackageBox
        if (!isHolding && TryUsePackageBox()) return;

        // 4) Pickup specializzato
        if (isHolding && heldPickup != null && heldPickup.InteractWith(currentTarget)) return;

        // 5) piatto + cookware
        if (isHolding && TryUseDishWithCookware()) return;

        // 6) ingrediente + cookware
        if (isHolding && TryCookWithHeldIngredient()) return;

        // 7) ObjectReceiver (es. plate position)
        if (isHolding && TryPlaceInObjectReceiver()) return;

        // 8) fallback pickup
        if (!isHolding && TryPickUpTarget()) return;

        Debug.Log("⚠️ Nessuna azione disponibile per questo target.");
    }

    // ---------- HELPER ----------
    bool TryToggleDeliveryDoor()
    {
        var box = currentTarget.GetComponentInParent<DeliveryBox>();
        if (box != null)
        {
            if (box.HandleDoorClick(currentTarget.transform))
                return true;
        }
        return false;
    }

    bool TryUseDishDispenser()
    {
        var dispenser = currentTarget.GetComponent<DishDispenser>();
        if (dispenser != null)
        {
            dispenser.TryGiveDishToPlayer(this);
            return true;
        }
        return false;
    }

    bool TryUsePackageBox()
    {
        var box = currentTarget.GetComponent<PackageBox>();
        if (box != null && box.isPlaced)
        {
            box.TryDeliver(this);
            return true;
        }
        return false;
    }

    bool TryPlaceInObjectReceiver()
    {
        if (heldPickup == null) return false;
        var receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            ClearHeld();
            return true;
        }
        return false;
    }

    bool TryPickUpTarget()
    {
        var pickup = currentTarget.GetComponent<PickupObject>()
                   ?? currentTarget.GetComponentInParent<PickupObject>();

        if (pickup != null && pickup.canBePickedUp)
        {
            PickUp(pickup);
            return true;
        }
        return false;
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
        if (heldPickup != null) heldPickup.Drop();
        ClearHeld();
    }

    public void ClearHeld()
    {
        if (heldPickup != null) heldPickup.isHeld = false;
        heldObject = null;
        heldPickup = null;
    }

    public bool IsHoldingObject() => heldObject != null;

    public void ReceiveExternalPickup(PickupObject pickup)
    {
        heldObject = pickup.gameObject;
        heldPickup = pickup;
    }

    bool TryCookWithHeldIngredient()
    {
        if (heldPickup == null || heldPickup.type != PickupType.Ingredient) return false;
        var cookware = currentTarget.GetComponentInParent<Cookware>();
        return cookware != null && cookware.TryAddIngredient(heldPickup);
    }

    bool TryUseDishWithCookware()
    {
        if (heldPickup == null || heldPickup.type != PickupType.Dish) return false;
        var dish = heldPickup.GetComponent<Dish>();
        var cookware = currentTarget.GetComponentInParent<Cookware>();

        if (dish != null && cookware != null && cookware.HasCookedIngredient())
        {
            var ingredient = cookware.GetCurrentIngredient();
            if (ingredient != null && dish.TryAddCookedIngredient(ingredient))
            {
                cookware.ConsumeServing();
                return true;
            }
        }
        return false;
    }
}
