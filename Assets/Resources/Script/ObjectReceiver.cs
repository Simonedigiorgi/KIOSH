using System.Collections.Generic;
using UnityEngine;

public class ObjectReceiver : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public List<PickupType> acceptedTypes;
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

    public void Interact(PlayerInteractor interactor)
    {
        var held = interactor.HeldPickup;
        if (held != null && CanAccept(held))
        {
            Ray ray = new Ray(interactor.transform.position, interactor.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
                Place(held, hit.point);
            else
                Place(held, transform.position);

            interactor.ClearHeld();
        }
        else
        {
            Debug.Log("⚠️ Nessun oggetto valido in mano per posizionare qui.");
        }
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

        // piazza mantenendo scala globale
        item.transform.SetParent(null, true);
        item.transform.position = bestPoint.position;
        item.transform.rotation = bestPoint.rotation;

        item.isHeld = false;
        item.canBePickedUp = true;
        item.currentPlacePoint = bestPoint;

        occupied[bestPoint] = item;

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
