using UnityEngine;

public class DeliveryBox : MonoBehaviour
{
    [Header("Piatto")]
    public Transform platePosition;
    private Dish currentDish;

    [Header("Bottone di spedizione")]
    public GameObject deliveryButtonObject; // il cubo o mesh usato come tasto

    void Update()
    {
        // Optional: evidenzia il tasto se il piatto è completo
        if (deliveryButtonObject != null)
            deliveryButtonObject.SetActive(currentDish != null);
    }

    // Chiamato quando il giocatore inserisce un piatto
    public void TryInsertDish(PickupObject pickup)
    {
        if (currentDish != null) return;

        Dish dish = pickup.GetComponent<Dish>();
        if (dish == null || !dish.IsComplete) return;

        pickup.transform.SetPositionAndRotation(platePosition.position, platePosition.rotation);
        pickup.transform.SetParent(platePosition); // ✅ resta ancorato al piatto box

        var rb = pickup.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // opzionale: blocca il re-pick finché sta nel box
        pickup.canBePickedUp = false;
        pickup.isHeld = false;

        currentDish = dish;
        Debug.Log("📦 Piatto inserito nel delivery box.");
    }


    // Chiamato quando clicchi sul bottone
    public void OnDeliveryButtonClick()
    {
        if (currentDish == null || !currentDish.IsComplete) return;

        Debug.Log("🚀 Piatto spedito!");
        Destroy(currentDish.gameObject);
        currentDish = null;
    }

    public bool IsOccupied => currentDish != null;
}
