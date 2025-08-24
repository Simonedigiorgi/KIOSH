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

        item.transform.position = placePivot.position;
        item.transform.rotation = placePivot.rotation;
        item.transform.SetParent(null);

        item.isHeld = false;
        item.canBePickedUp = true;

        var rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null)
            interactor.ClearHeld();

        var cookware = item.GetComponent<Cookware>();
        if (cookware != null)
        {
            cookware.OnPlacedInReceiver();
        }

        var package = item.GetComponent<PackageBox>();
        if (package != null)
        {
            package.Place(); // Bloccato dopo essere stato posizionato
        }
    }

    public void Unplace(PickupObject item)
    {
        if (item == null) return;

        var rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        item.canBePickedUp = true;
    }
}
