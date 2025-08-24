using UnityEngine;

public class PackageBox : MonoBehaviour
{
    public GameObject ingredientPrefab;
    public int ingredientCount = 3;
    public bool isPlaced { get; private set; }

    private bool isDepleted => ingredientCount <= 0;

    public void Place()
    {
        isPlaced = true;
    }

    public void TryDeliver(PlayerInteractor player)
    {
        if (!isPlaced || isDepleted || player.IsHoldingObject()) return;

        // Instanzia l'ingrediente davanti al giocatore (non ancora nella mano)
        GameObject instance = Instantiate(
            ingredientPrefab,
            player.handPivot.position,
            player.handPivot.rotation
        );

        PickupObject pickup = instance.GetComponent<PickupObject>();
        if (pickup == null)
        {
            Debug.LogError("❌ Il prefab non ha PickupObject!");
            Destroy(instance);
            return;
        }

        pickup.canBePickedUp = true;
        pickup.isHeld = false;

        var rb = instance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // 👉 Passiamo il controllo al sistema di pickup del player
        player.PickUp(pickup);

        ingredientCount--;
        Debug.Log($"📦 Ingredienti rimanenti: {ingredientCount}");
    }
}
