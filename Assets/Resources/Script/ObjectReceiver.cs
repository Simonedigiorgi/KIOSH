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

        // Posiziona l’oggetto nel pivot
        item.transform.position = placePivot.position;
        item.transform.rotation = placePivot.rotation;
        item.transform.SetParent(null); // ❗ Non lo parentiamo

        item.isHeld = false;
        item.canBePickedUp = true;

        // 🔄 Riattiva le collisioni!
        var rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = true; // ← 💥 questa è la chiave
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


        var interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null)
            interactor.ClearHeld();

        var cookware = item.GetComponent<Cookware>();
        if (cookware != null)
        {
            cookware.OnPlacedInReceiver(); // Se serve per aggiornamenti
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
