using UnityEngine;

public enum PickupType { Generic, Pot, Pan, Ingredient, Dish }

public enum IngredientTarget { None, Pot, Pan } // ➕ Aggiunto

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour
{
    public ObjectReceiver currentReceiver = null; // 🔄 chi lo ha ricevuto
    public bool canBePickedUp = true;
    public bool isHeld = false;
    public PickupType type = PickupType.Generic;

    [Header("Ingredient Settings")]
    public IngredientTarget cookTarget = IngredientTarget.None; // Solo per gli ingredienti
    public GameObject visualPrefab; // Prefab visuale da instanziare sulla pentola/padella

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
}
