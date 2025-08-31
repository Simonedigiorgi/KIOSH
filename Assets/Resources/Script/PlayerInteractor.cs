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
    public InteractableName currentTargetName { get; private set; }

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
            HandleInteraction(IsHoldingObject());

        if (Input.GetKeyDown(KeyCode.Q))
            DropHeld();
    }

    // ---------- TARGET SELECTION ----------
    void UpdateRaycastTarget()
    {
        currentTarget = null;
        currentTargetName = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.layer == LayerMask.NameToLayer("Default"))
                return; // muro o blocco → interrompi

            if (heldObject != null &&
                (hitObject == heldObject || hitObject.transform.IsChildOf(heldObject.transform)))
                continue;

            if ((interactableLayer.value & (1 << hitObject.layer)) == 0)
                continue;

            currentTarget = hitObject;
            currentTargetName = hitObject.GetComponent<InteractableName>();
            return;
        }
    }

    // ---------- CORE ----------
    void HandleInteraction(bool isHolding)
    {
        var door = FindObjectOfType<RoomDoor>();
        if (door != null && door.IsLookingThroughPeephole)
        {
            door.InteractWithPeephole();
            return;
        }

        if (!currentTarget)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        if (TryBoard()) return;
        if (TryDeliveryDoor()) return;
        if (TryRoomDoor()) return;
        if (!isHolding && TryDishDispenser()) return;
        if (!isHolding && TryPackageBox()) return;
        if (isHolding && TryPickupSpecialized()) return;
        if (isHolding && TryDishWithCookware()) return;
        if (isHolding && TryCookIngredient()) return;
        if (isHolding && TryObjectReceiver()) return;
        if (!isHolding && TryPickup()) return;

        Debug.Log("⚠️ Nessuna azione disponibile per questo target.");
    }

    // ---------- DOOR ----------
    bool TryRoomDoor()
    {
        var door = currentTarget.GetComponentInParent<RoomDoor>();
        if (door == null) return false;

        if (door.peephole != null && currentTarget.transform == door.peephole)
        {
            door.InteractWithPeephole();
            return true;
        }

        if (door.handle != null && currentTarget.transform == door.handle)
        {
            door.InteractWithHandle();
            return true;
        }

        return false;
    }

    // ---------- ACTIONS ----------
    bool TryBoard()
    {
        var bulletin = currentTarget.GetComponentInParent<BulletinInteraction>();
        if (bulletin)
        {
            bulletin.EnterInteraction();
            return true;
        }
        return false;
    }

    bool TryDeliveryDoor()
    {
        var box = currentTarget.GetComponentInParent<DeliveryBox>();
        return box != null && box.HandleDoorClick(currentTarget.transform);
    }

    bool TryDishDispenser()
    {
        var dispenser = currentTarget.GetComponentInParent<DishDispenser>();
        if (dispenser)
        {
            dispenser.TryGiveDishToPlayer(this);
            return true;
        }
        return false;
    }

    bool TryPackageBox()
    {
        var box = currentTarget.GetComponentInParent<PackageBox>();
        if (box && box.isPlaced)
        {
            box.TryDeliver(this);
            return true;
        }
        return false;
    }

    bool TryPickupSpecialized()
    {
        return heldPickup != null && heldPickup.InteractWith(currentTarget);
    }

    bool TryDishWithCookware()
    {
        if (heldPickup?.type != PickupType.Dish) return false;
        var dish = heldPickup.GetComponent<Dish>();
        var cookware = currentTarget.GetComponentInParent<Cookware>();

        if (dish && cookware && cookware.HasCookedIngredient())
        {
            var ingredient = cookware.GetCurrentIngredient();
            if (ingredient && dish.TryAddCookedIngredient(ingredient))
            {
                cookware.ConsumeServing();
                return true;
            }
        }
        return false;
    }

    bool TryCookIngredient()
    {
        if (heldPickup?.type != PickupType.Ingredient) return false;
        var cookware = currentTarget.GetComponentInParent<Cookware>();

        if (cookware && cookware.TryAddIngredient(heldPickup))
        {
            return true;
        }
        else
        {
            Debug.Log("⚠️ Ingrediente incompatibile con questo strumento.");
            HUDManager.Instance?.ShowDialog("Non puoi usare questo ingrediente");
            return true;
        }
    }

    bool TryObjectReceiver()
    {
        if (heldPickup == null) return false;

        var receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
                receiver.Place(heldPickup, hit.point);
            else
                receiver.Place(heldPickup, receiver.transform.position);

            ClearHeld();
            return true;
        }
        return false;
    }

    bool TryPickup()
    {
        var pickup = currentTarget.GetComponentInParent<PickupObject>();
        if (pickup && pickup.canBePickedUp)
        {
            var receiver = pickup.GetComponentInParent<ObjectReceiver>();
            if (receiver != null)
                receiver.Unplace(pickup);

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
