using UnityEngine;

public class CookerReceiver : MonoBehaviour, IPlaceableReceiver
{
    public IngredientTarget acceptedTarget; // Pan o Pot
    public Transform cookPivot;
    private GameObject currentVisual;

    public bool CanAccept(PickupObject item)
    {
        return currentVisual == null &&
               item.type == PickupType.Ingredient &&
               item.cookTarget == acceptedTarget &&
               item.visualPrefab != null;
    }

    public void Place(PickupObject item)
    {
        // Instanzia il visual sulla padella/pentola
        currentVisual = Instantiate(item.visualPrefab, cookPivot.position, cookPivot.rotation, cookPivot.parent);

        // Distruggi l'ingrediente originale
        Destroy(item.gameObject);

        // Libera la mano
        var interactor = FindObjectOfType<PlayerInteractor>();
        if (interactor != null)
        {
            interactor.ClearHeld();
        }
    }

    public void Unplace()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }
    }
}
