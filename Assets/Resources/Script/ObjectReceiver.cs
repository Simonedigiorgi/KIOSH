using System.Collections.Generic;
using UnityEngine;

public class ObjectReceiver : MonoBehaviour
{
    [Header("Settings")]
    public List<PickupType> acceptedTypes;

    [Tooltip("Lista di posizioni possibili (Place_1, Place_2, ...)")]
    public List<Transform> placePoints = new List<Transform>();

    private Dictionary<Transform, PickupObject> occupied = new Dictionary<Transform, PickupObject>();

    void Awake()
    {
        foreach (var p in placePoints)
        {
            if (p != null && !occupied.ContainsKey(p))
                occupied[p] = null;
        }
    }

    public bool CanAccept(PickupObject item)
    {
        return item != null && acceptedTypes.Contains(item.type);
    }

    public void Place(PickupObject item, Vector3 hitPoint)
    {
        if (item == null || placePoints.Count == 0) return;

        // trova slot libero più vicino
        Transform bestPoint = null;
        float bestDist = Mathf.Infinity;

        foreach (var point in placePoints)
        {
            if (point == null) continue;
            if (occupied.ContainsKey(point) && occupied[point] != null) continue;

            float d = Vector3.Distance(hitPoint, point.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestPoint = point;
            }
        }

        if (bestPoint == null)
        {
            Debug.Log("⚠️ Nessun posto libero disponibile.");
            return;
        }

        // piazza l’oggetto in world space SENZA alterarne la scala
        item.transform.SetParent(null);
        item.transform.position = bestPoint.position;
        item.transform.rotation = bestPoint.rotation;
        // 🔑 la scala rimane quella del prefab originale

        item.isHeld = false;
        item.canBePickedUp = true;
        item.currentPlacePoint = bestPoint;

        occupied[bestPoint] = item;

        // Cookware → logica aggiuntiva
        var cookware = item.GetComponent<Cookware>();
        if (cookware != null)
        {
            cookware.OnPlacedInReceiver();

            // se ha un cookTarget → aggancia lì
            if (cookware.cookTarget != null)
            {
                item.transform.SetParent(cookware.cookTarget, true);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }
        }

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

        item.canBePickedUp = true;
        item.isHeld = false;

        if (item.currentPlacePoint != null)
        {
            occupied[item.currentPlacePoint] = null;
            item.currentPlacePoint = null;
        }

        var dish = item.GetComponent<Dish>();
        if (dish != null)
        {
            var box = GetComponentInParent<DeliveryBox>();
            if (box != null) box.OnDishRemoved(dish);
        }
    }
}
