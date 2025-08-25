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
            // Un solo punto d’ingresso: passa se stai tenendo qualcosa oppure no
            HandleInteraction(IsHoldingObject());
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

        if (Physics.Raycast(playerCamera.transform.position,
                            playerCamera.transform.forward,
                            out RaycastHit hit, interactDistance, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            // controlla se ha un PickupObject
            PickupObject pickup = hitObject.GetComponent<PickupObject>();
            if (pickup == null)
            {
                pickup = hitObject.GetComponentInParent<PickupObject>();
            }

            if (pickup != null)
            {
                // ✅ se ho colpito un oggetto raccoglibile
                currentTarget = pickup.gameObject;
                currentTargetName = pickup.GetComponent<InteractableName>();
            }
            else
            {
                // ✅ fallback: uso direttamente l’oggetto colpito (es. Cookware, DeliveryBox, ecc.)
                currentTarget = hitObject;
                currentTargetName = hitObject.GetComponent<InteractableName>();
            }
        }
    }


    // ---------- CORE UNIFICATO ----------
    void HandleInteraction(bool isHolding)
    {
        if (currentTarget == null)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        // 🎛️ Apri la board SEMPRE (mani vuote o piene)
        var bulletin = currentTarget.GetComponentInParent<BulletinInteraction>();
        if (bulletin != null)
        {
            bulletin.EnterInteraction();
            return;
        }

        // 0) Sportello delivery (cliccabile sempre: mani vuote o piene)
        if (TryToggleDeliveryDoor()) return;

        // 1) Dispenser piatti (solo se non sto già tenendo qualcosa)
        if (!isHolding && TryUseDishDispenser()) return;

        // 2) PackageBox (solo se non sto tenendo qualcosa)
        if (!isHolding && TryUsePackageBox()) return;

        // 3) Se sto tenendo qualcosa, prima lascia gestire al Pickup specializzato (es. DishPickup)
        if (isHolding && heldPickup != null && heldPickup.InteractWith(currentTarget))
            return;

        // 💡 ingrediente in mano → prova a cucinare sulla cookware
        if (isHolding && TryCookWithHeldIngredient()) return;

        // 4) Mettere oggetto tenuto in mano dentro un ObjectReceiver
        if (isHolding && TryPlaceInObjectReceiver()) return;

        // 📦 inserimento piatto nel delivery box
        if (isHolding && TryInsertDishInDeliveryBox()) return;

        // 6) Premere il bottone del DeliveryBox (mani vuote o piene indifferente)
        if (TryPressDeliveryButton()) return;

        // 7) Fallback: provare a prendere un PickupObject libero
        if (!isHolding && TryPickUpTarget()) return;

        // Altrimenti niente da fare
        Debug.Log("⚠️ Nessuna azione disponibile per questo target.");
    }

    // ---------- HELPER AZIONI ----------

    bool TryToggleDeliveryDoor()
    {
        if (currentTarget == null) return false;

        DeliveryBox box = currentTarget.GetComponentInParent<DeliveryBox>();
        if (box != null)
        {
            if (box.HandleDoorClick(currentTarget.transform))
                return true;
        }
        return false;
    }

    bool TryUseDishDispenser()
    {
        DishDispenser dispenser = currentTarget.GetComponent<DishDispenser>();
        if (dispenser != null)
        {
            dispenser.TryGiveDishToPlayer(this);
            return true;
        }
        return false;
    }

    bool TryUsePackageBox()
    {
        PackageBox box = currentTarget.GetComponent<PackageBox>();
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

        ObjectReceiver receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            ClearHeld();
            return true;
        }
        return false;
    }

    bool TryInsertDishInDeliveryBox()
    {
        if (heldPickup == null || heldPickup.type != PickupType.Dish) return false;

        DeliveryBox deliveryBox = currentTarget.GetComponent<DeliveryBox>();
        if (deliveryBox == null)
            deliveryBox = currentTarget.GetComponentInParent<DeliveryBox>();

        if (deliveryBox != null)
        {
            deliveryBox.TryInsertDish(heldPickup);
            ClearHeld();
            return true;
        }

        return false;
    }


    bool TryPressDeliveryButton()
    {
        if (currentTarget.name == "DeliveryButton") // oppure usa un tag
        {
            DeliveryBox parentBox = currentTarget.GetComponentInParent<DeliveryBox>();
            if (parentBox != null)
            {
                parentBox.OnDeliveryButtonClick();
                return true;
            }
        }
        return false;
    }

    bool TryPickUpTarget()
    {
        // prova prima sul target diretto
        PickupObject pickup = currentTarget.GetComponent<PickupObject>();
        if (pickup == null)
        {
            // poi risali ai parent
            pickup = currentTarget.GetComponentInParent<PickupObject>();
        }

        if (pickup != null)
        {
            if (pickup.canBePickedUp)
            {
                PickUp(pickup);
                return true;
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

        return false;
    }


    // ---------- UTILITIES HELD ----------
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

        // prendi la cookware dal target o dai suoi parent (collider figli ecc.)
        Cookware cookware = currentTarget.GetComponent<Cookware>();
        if (cookware == null) cookware = currentTarget.GetComponentInParent<Cookware>();

        if (cookware != null && cookware.TryAddIngredient(heldPickup))
        {
            ClearHeld();
            return true;
        }
        return false;
    }

}
