using System.Collections.Generic;
using UnityEngine;

public class ObjectReceiver : MonoBehaviour, IPlaceableReceiver
{
    public bool markAsHeld = false;
    private bool isOccupied = false;
    public Transform placePivot;
    public List<PickupType> acceptedTypes;

    public bool CanAccept(PickupObject item)
    {
        return !isOccupied && acceptedTypes.Contains(item.type);
    }

    public void Place(PickupObject item)
    {
        isOccupied = true;

        // Sgancia dalla mano
        item.Drop(); // 👈 Aggiungi questa riga per rimuoverlo dalla mano correttamente

        // Posiziona nel punto designato
        item.transform.position = placePivot.position;
        item.transform.rotation = placePivot.rotation;

        item.isHeld = false;
        item.currentReceiver = this;

        item.canBePickedUp = !markAsHeld;

        var rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = true;
        }

        // Ignora collisione con il player (opzionale se hai ancora questo passaggio)
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            var playerCollider = playerController.GetComponent<Collider>();
            var objectCollider = item.GetComponent<Collider>();

            if (playerCollider != null && objectCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, objectCollider, true);
            }
        }

        // Rimuovi le reference dal PlayerInteractor
        var interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null)
            interactor.ClearHeld();
    }



    public void Unplace()
    {
        isOccupied = false;
    }
}
