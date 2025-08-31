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

        // 🔑 salva scala globale prima del parenting
        Vector3 originalScale = transform.lossyScale;

        transform.SetParent(hand, true); // "true" mantiene worldPosition/Rotation/Scale
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 🔑 ripristina scala globale originale
        transform.localScale = originalScale;

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
