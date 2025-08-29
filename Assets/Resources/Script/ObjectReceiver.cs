using System.Collections.Generic;
using UnityEngine;

public class ObjectReceiver : MonoBehaviour
{
    [Header("Settings")]
    public List<PickupType> acceptedTypes;

    [Tooltip("Lista di posizioni possibili (Place_1, Place_2, ...)")]
    public List<Transform> placePoints = new List<Transform>();

    // Stato dei punti → true se occupato
    private Dictionary<Transform, PickupObject> occupied = new Dictionary<Transform, PickupObject>();

    void Awake()
    {
        // inizializza tutti come liberi
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

        // trova la posizione libera più vicina
        Transform bestPoint = null;
        float bestDist = Mathf.Infinity;

        foreach (var point in placePoints)
        {
            if (point == null) continue;
            if (occupied.ContainsKey(point) && occupied[point] != null) continue; // già occupato

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

        // piazza l’oggetto
        item.transform.SetParent(null);
        item.transform.SetPositionAndRotation(bestPoint.position, bestPoint.rotation);

        item.isHeld = false;
        item.canBePickedUp = true;

        // segna occupato
        occupied[bestPoint] = item;

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

        item.canBePickedUp = true;
        item.isHeld = false;

        // libera lo slot occupato
        foreach (var kv in new Dictionary<Transform, PickupObject>(occupied))
        {
            if (kv.Value == item)
            {
                occupied[kv.Key] = null;
                break;
            }
        }

        // se è un piatto dentro a una DeliveryBox → deregistra
        var dish = item.GetComponent<Dish>();
        if (dish != null)
        {
            var box = GetComponentInParent<DeliveryBox>();
            if (box != null) box.OnDishRemoved(dish);
        }
    }
}
