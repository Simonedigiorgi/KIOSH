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

            // ✅ Blocca oggetti dietro ai muri
            if (((1 << hitObject.layer) & interactableLayer) == 0)
            {
                Debug.Log($"⛔ Raycast ha colpito qualcosa fuori da interactableLayer: {hitObject.name}");
                return;
            }

            // 🎯 Verifica se è un PickupObject
            var pickup = hitObject.GetComponentInParent<PickupObject>();
            if (pickup != null)
            {
                currentTarget = pickup.gameObject;
                currentTargetName = pickup.GetComponent<InteractableName>();
            }
            else
            {
                // 🎯 Altri oggetti con solo InteractableName
                currentTarget = hitObject;
                currentTargetName = hitObject.GetComponent<InteractableName>();
            }

            Debug.Log($"🎯 Raycast colpisce: {hit.collider.name} | Target: {currentTarget.name}");
        }
        else
        {
            Debug.Log("❌ Nessun target nel mirino");
        }
    }



    void TryInteractOrPickUp()
    {
        if (currentTarget == null)
        {
            Debug.Log("❌ Nessun target valido nel mirino.");
            return;
        }

        PickupObject pickup = currentTarget.GetComponent<PickupObject>();
        if (pickup == null)
            pickup = currentTarget.GetComponentInParent<PickupObject>();

        if (pickup != null)
        {
            Debug.Log($"✅ Trovato: {pickup.name}, canBePickedUp: {pickup.canBePickedUp}, isHeld: {pickup.isHeld}");

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

        // 🍳 Tenta di cucinare un ingrediente
        Cookware cookware = currentTarget.GetComponent<Cookware>();
        if (cookware != null)
        {
            if (cookware.TryAddIngredient(heldPickup))
            {
                ClearHeld();
                return;
            }
        }

        // 🔲 Tenta di piazzare oggetti come padella o pentola
        ObjectReceiver receiver = currentTarget.GetComponent<ObjectReceiver>();
        if (receiver != null && receiver.CanAccept(heldPickup))
        {
            receiver.Place(heldPickup);
            ClearHeld();
            return;
        }

        // Altri usi futuri
    }


    void PickUp(PickupObject pickup)
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
