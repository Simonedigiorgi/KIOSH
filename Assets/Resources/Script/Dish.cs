using UnityEngine;

public class Dish : MonoBehaviour
{
    public Transform[] ingredientPivots; // max 2 pivot
    private int currentCount = 0;

    public bool IsComplete => currentCount >= 2;

    public bool TryAddCookedIngredient(Ingredient ingredient)
    {
        if (IsComplete || ingredient == null || ingredient.dishPrefab == null)
        {
            Debug.LogWarning("❌ Ingrediente non valido o piatto pieno");
            return false;
        }

        Debug.Log("✅ Aggiungo " + ingredient.dishPrefab.name + " nel piatto");

        Instantiate(
            ingredient.dishPrefab,
            ingredientPivots[0].position,
            ingredientPivots[0].rotation,
            ingredientPivots[0]
        );

        currentCount++;
        return true;
    }


}
