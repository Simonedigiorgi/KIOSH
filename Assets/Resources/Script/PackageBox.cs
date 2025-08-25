using UnityEngine;

public class PackageBox : MonoBehaviour
{
    public GameObject ingredientPrefab;
    public bool isPlaced = false;

    public void Place()
    {
        isPlaced = true;
        GetComponent<PickupObject>().canBePickedUp = false;
    }

    public void TryDeliver(PlayerInteractor player)
    {
        if (!isPlaced || player.IsHoldingObject()) return;

        Ingredient ing = ingredientPrefab.GetComponent<Ingredient>();
        if (ing == null)
        {
            Debug.LogError("❌ Il prefab non ha un componente Ingredient!");
            return;
        }

        if (IngredientManager.Instance.IsIngredientActive(ing.ingredientID))
        {
            Debug.Log($"⛔ Ingrediente '{ing.ingredientID}' già attivo.");
            return;
        }

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
    }



}
