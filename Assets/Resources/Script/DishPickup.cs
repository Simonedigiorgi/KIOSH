using UnityEngine;

public class DishPickup : PickupObject
{
    public Dish dish;

    void Awake() { if (dish == null) dish = GetComponent<Dish>(); }

    public override bool InteractWith(GameObject target)
    {
        Cookware cookware = target.GetComponent<Cookware>();
        if (cookware == null || !cookware.HasCookedIngredient()) return false;

        Ingredient ingredient = cookware.GetCurrentIngredient();
        if (ingredient == null) return false;

        // Se il piatto accetta, consumiamo la porzione e ritorniamo comunque true
        if (dish != null && dish.TryAddCookedIngredient(ingredient))
        {
            cookware.ConsumeServing(); // può arrivare a 0 e pulire la cookware
            return true;               // ✅ l'interazione è avvenuta con successo
        }

        return false;
    }


}
