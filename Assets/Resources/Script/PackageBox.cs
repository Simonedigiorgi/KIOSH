using UnityEngine;

public class PackageBox : MonoBehaviour
{
    public GameObject ingredientPrefab;
    public int ingredientCount = 3;
    public bool isPlaced = false;

    private bool isDepleted => ingredientCount <= 0;

    public void Place()
    {
        isPlaced = true;
        GetComponent<PickupObject>().canBePickedUp = false;
    }

    public void TryDeliver(PlayerInteractor player)
    {
        if (!isPlaced || isDepleted || player.IsHoldingObject()) return;

        GameObject instance = Instantiate(
            ingredientPrefab,
            player.handPivot.position,
            player.handPivot.rotation,
            player.handPivot
        );

        PickupObject pickup = instance.GetComponent<PickupObject>();
        if (pickup == null)
        {
            Debug.LogError("❌ Il prefab non ha PickupObject!");
            Destroy(instance);
            return;
        }

        pickup.canBePickedUp = true;
        pickup.isHeld = true;

        var rb = pickup.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        player.ReceiveExternalPickup(pickup);

        ingredientCount--;
        Debug.Log($"📦 Ingredienti rimanenti: {ingredientCount}");
    }
}
