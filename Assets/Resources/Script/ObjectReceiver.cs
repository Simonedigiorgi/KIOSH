using System.Collections.Generic;
using UnityEngine;

public class ObjectReceiver : MonoBehaviour
{
    [Header("Settings")]
    public List<PickupType> acceptedTypes;
    public Transform placePivot;

    public bool CanAccept(PickupObject item)
    {
        return item != null && acceptedTypes.Contains(item.type);
    }

    public void Place(PickupObject item)
    {
        if (item == null) return;

        // piazza l’oggetto
        item.transform.SetParent(null);
        item.transform.SetPositionAndRotation(placePivot.position, placePivot.rotation);

        item.isHeld = false;
        item.canBePickedUp = true;

        // Cookware
        var cookware = item.GetComponent<Cookware>();
        if (cookware != null) cookware.OnPlacedInReceiver();

        // Package
        var package = item.GetComponent<PackageBox>();
        if (package != null) package.Place();

        // Dish in DeliveryBox
        var dish = item.GetComponent<Dish>();
        if (dish != null)
        {
            var box = GetComponentInParent<DeliveryBox>();
            if (box != null) box.RegisterDish(dish);
        }
    }

    public void Unplace(PickupObject item)
    {
        if (item == null) return;

        // basta renderlo raccoglibile di nuovo
        item.canBePickedUp = true;
        item.isHeld = false;

        // se è un piatto dentro a una DeliveryBox → deregistra
        var dish = item.GetComponent<Dish>();
        if (dish != null)
        {
            var box = GetComponentInParent<DeliveryBox>();
            if (box != null) box.OnDishRemoved(dish);
        }
    }
}
