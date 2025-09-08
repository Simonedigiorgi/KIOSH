using UnityEngine;

public class Dish : MonoBehaviour
{
    [Header("Dish Settings")]
    public Transform ingredientPivot;
    public GameObject filledPrefab;

    private bool isFilled = false;
    public bool IsComplete => isFilled;

    public bool TryAddFromStation(CookingStation station)
    {
        if (isFilled || station == null) return false;
        if (!station.CanServeDish()) return false;

        if (filledPrefab && ingredientPivot)
            Instantiate(filledPrefab, ingredientPivot.position, ingredientPivot.rotation, ingredientPivot);

        isFilled = true;
        return true;
    }

    void OnDestroy()
    {
        FindObjectOfType<DishDispenser>()?.RemoveDish(gameObject);
    }
}
