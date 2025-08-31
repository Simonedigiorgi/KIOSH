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

    [HideInInspector] public Transform currentPlacePoint;

    public void PickUp(Transform hand)
    {
        if (!canBePickedUp) return;

        isHeld = true;

        // Parenting mantenendo world transform → NON tocca lo scale
        transform.SetParent(hand, true);
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
            currentPlacePoint = null;
        }
    }

    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null, true); // mantiene scala e rotazione
    }

    public virtual bool InteractWith(GameObject target) => false;
}
