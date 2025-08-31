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

    public HUDMessageSet wrongIngredientMessage;

    public GameObject currentTarget { get; private set; }
    public InteractableName currentTargetName { get; private set; }

    void Update()
    {
        // Blocca input gameplay mentre il dialogo è aperto
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
        // 👆 ~0 = tutti i layer, così possiamo intercettare anche Default
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Se colpiamo il Default → blocca il raycast qui
            if (hitObject.layer == LayerMask.NameToLayer("Default"))
            {
                return; // muro o altro → interrompi
            }

            // ignora oggetto tenuto in mano
            if (heldObject != null &&
                (hitObject == heldObject || hitObject.transform.IsChildOf(heldObject.transform)))
                continue;

            // considera solo layer dentro interactableLayer
            if ((interactableLayer.value & (1 << hitObject.layer)) == 0)
                continue;

            // salva target valido
            currentTarget = hitObject;
            currentTargetName = hitObject.GetComponent<InteractableName>();
            return;
        }
    }


    // ---------- CORE ----------
    void HandleInteraction(bool isHolding)
    {
        // 👇 Se sei nello spioncino → ESCI direttamente
        var door = FindObjectOfType<RoomDoor>();
        if (door != null && door.IsLookingThroughPeephole)
        {
            door.InteractWithPeephole(); // richiama Exit
            return;
        }

        if (!currentTarget)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        // ordine di priorità
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

        // ✅ confronto diretto con i riferimenti della porta
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
            HUDManager.Instance?.ShowDialog(wrongIngredientMessage);
            return true;
        }
    }


    bool TryObjectReceiver()
    {
        if (heldPickup == null) return false;

        var receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            // usa l’ultimo punto del raycast per decidere il Place più vicino
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
            {
                receiver.Place(heldPickup, hit.point);
            }
            else
            {
                receiver.Place(heldPickup, receiver.transform.position); // fallback
            }

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
            // 🔑 se l’oggetto era in un ObjectReceiver → liberiamo lo slot
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
