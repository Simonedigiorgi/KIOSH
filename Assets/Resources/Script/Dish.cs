using System.Collections.Generic;
using UnityEngine;

public class Dish : MonoBehaviour
{
    [Header("Dish Settings")]
    public Transform ingredientPivot;
    public int maxIngredients = 2;

    private int currentCount = 0;
    private HashSet<string> addedIngredients = new HashSet<string>();

    public bool IsComplete => currentCount >= maxIngredients;

    public bool TryAddCookedIngredient(Ingredient ingredient)
    {
        if (IsComplete || ingredient == null || ingredient.dishPrefab == null)
        {
            Debug.LogWarning("❌ Piatto pieno o ingrediente non valido");
            return false;
        }

        if (addedIngredients.Contains(ingredient.ingredientID))
        {
            Debug.LogWarning($"⚠️ L'ingrediente {ingredient.ingredientID} è già nel piatto!");
            return false;
        }

        Instantiate(
            ingredient.dishPrefab,
            ingredientPivot.position,
            ingredientPivot.rotation,
            ingredientPivot
        );

        addedIngredients.Add(ingredient.ingredientID);
        currentCount++;
        return true;
    }

    void OnDestroy()
    {
        FindObjectOfType<DishDispenser>()?.RemoveDish(gameObject);
    }
}
