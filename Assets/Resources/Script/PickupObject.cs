using UnityEngine;

public enum PickupType
{
    Package,
    Ingredient,
    Dish
}

public class PickupObject : MonoBehaviour
{
    public bool canBePickedUp = true;
    public bool isHeld = false;
    public PickupType type = PickupType.Package;

    // 🔑 slot corrente se piazzato
    [HideInInspector] public Transform currentPlacePoint;

    public void PickUp(Transform hand)
    {
        if (!canBePickedUp) return;

        isHeld = true;
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // se questo piatto era dentro una DeliveryBox → liberala
        var box = GetComponentInParent<DeliveryBox>();
        var dish = GetComponent<Dish>();
        if (box != null && dish != null && box.CurrentDish == dish)
        {
            box.OnDishRemoved(dish);
        }

        // se era in un PlacePoint → liberiamo
        if (currentPlacePoint != null)
        {
            var receiver = currentPlacePoint.GetComponentInParent<ObjectReceiver>();
            if (receiver != null) receiver.Unplace(this);
            currentPlacePoint = null; // reset
        }
    }

    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null);
    }

    public virtual bool InteractWith(GameObject target) => false;
}
