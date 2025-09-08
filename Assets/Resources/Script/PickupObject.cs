using UnityEngine;

public enum PickupType
{
    Dish // 👈 per ora gestiamo solo i piatti
}

public class PickupObject : MonoBehaviour, IInteractable
{
    public bool canBePickedUp = true;
    public bool isHeld = false;
    public PickupType type = PickupType.Dish;

    [HideInInspector] public Transform currentPlacePoint;

    public void PickUp(Transform hand)
    {
        if (!canBePickedUp) return;

        isHeld = true;

        transform.SetParent(hand, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Se il piatto era dentro una DeliveryBox → liberala
        var box = GetComponentInParent<DeliveryBox>();
        var dish = GetComponent<Dish>();
        if (box != null && dish != null && box.CurrentDish == dish)
        {
            box.OnDishRemoved(dish);
        }

        // Se era in un ObjectReceiver → liberiamo lo slot
        if (currentPlacePoint != null)
        {
            var receiver = currentPlacePoint.GetComponentInParent<ObjectReceiver>();
            if (receiver != null) receiver.Unplace(this);
            currentPlacePoint = null;
        }
    }

    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null, true); // mantiene scala e rotazione
    }

    public virtual bool InteractWith(GameObject target) => false;

    // ---------- IInteractable ----------
    public void Interact(PlayerInteractor interactor)
    {
        if (!isHeld && canBePickedUp)
        {
            interactor.PickUp(this);
        }
    }
}
