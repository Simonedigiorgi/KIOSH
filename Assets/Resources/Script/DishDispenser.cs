using System.Collections.Generic;
using UnityEngine;

public class DishDispenser : MonoBehaviour
{
    public GameObject dishPrefab;
    public int maxDishesInScene = 5;

    private List<GameObject> activeDishes = new List<GameObject>();

    public void TryGiveDishToPlayer(PlayerInteractor player)
    {
        if (player == null || player.IsHoldingObject())
        {
            Debug.Log("🚫 Il giocatore non esiste o ha già qualcosa in mano.");
            return;
        }

        if (activeDishes.Count >= maxDishesInScene)
        {
            Debug.Log("📛 Troppi piatti in scena.");
            return;
        }

        GameObject dish = Instantiate(
            dishPrefab,
            player.handPivot.position,
            player.handPivot.rotation,
            player.handPivot
        );

        var pickup = dish.GetComponent<PickupObject>();
        if (pickup == null)
        {
            Debug.LogError("❌ Il prefab del piatto non ha PickupObject.");
            Destroy(dish);
            return;
        }

        pickup.canBePickedUp = true;
        pickup.isHeld = true;

        var rb = dish.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        player.ReceiveExternalPickup(pickup);

        // ✅ Tracciamo il piatto
        activeDishes.Add(dish);
    }

    public void RemoveDish(GameObject dish)
    {
        if (activeDishes.Contains(dish))
        {
            activeDishes.Remove(dish);
        }
    }
}
