using UnityEngine;

public enum PickupType { Package, Pot, Pan, Ingredient, Dish }

public class PickupObject : MonoBehaviour
{
    public bool canBePickedUp = true;
    public bool isHeld = false;
    public PickupType type = PickupType.Package; // default

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
    }

    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null);
    }

    // Hook per logica specializzata (Ingredient, Dish, ecc.)
    public virtual bool InteractWith(GameObject target)
    {
        return false;
    }
}
