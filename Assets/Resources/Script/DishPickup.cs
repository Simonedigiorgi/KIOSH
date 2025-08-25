using UnityEngine;

public class DishPickup : PickupObject
{
    public Dish dish;

    public override bool InteractWith(GameObject target)
    {
        Cookware cookware = target.GetComponent<Cookware>();
        if (cookware != null && cookware.HasCookedIngredient())
        {
            Ingredient ingredient = cookware.GetCurrentIngredient();
            if (ingredient != null && dish.TryAddCookedIngredient(ingredient))
            {
                cookware.ClearCookedIngredient();
                return true;
            }
        }

        return false;
    }

}
