using UnityEngine;

public enum PickupType { Package, Pot, Pan, Ingredient, Dish }

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour
{
    public bool canBePickedUp = true;
    public bool isHeld = false;
    public PickupType type = PickupType.Package; // Default ora è Package

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PickUp(Transform hand)
    {
        isHeld = true;
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }

    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null);

        if (rb)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }
    }

    public virtual bool InteractWith(GameObject target)
    {
        return false;
    }

}
