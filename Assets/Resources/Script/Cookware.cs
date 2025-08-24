using UnityEngine;

public class Cookware : MonoBehaviour
{
    public CookingToolType toolType;              // Indica se è pentola o padella
    public Transform cookTarget;                  // Dove far comparire il prefab cucinato
    public float cookTime = 5f;

    private GameObject currentCookingInstance;
    private bool isCooking = false;
    private float timer = 0f;
    private Ingredient currentIngredient;

    void Update()
    {
        if (isCooking)
        {
            timer += Time.deltaTime;
            if (timer >= cookTime)
            {
                OnCookComplete();
            }
        }
    }

    public bool TryAddIngredient(PickupObject pickup)
    {
        if (isCooking || pickup.type != PickupType.Ingredient) return false;

        Ingredient ingredient = pickup.GetComponent<Ingredient>();
        if (ingredient == null || ingredient.cookedPrefab == null) return false;

        // Verifica compatibilità utensile
        if (ingredient.compatibleTool != toolType) return false;

        Destroy(pickup.gameObject); // Distrugge l'ingrediente in mano

        currentIngredient = ingredient;

        currentCookingInstance = Instantiate(ingredient.cookedPrefab, cookTarget.position, cookTarget.rotation, cookTarget);
        isCooking = true;
        timer = 0f;

        // Suono di sfrigolio qui (opzionale)
        return true;
    }

    void OnCookComplete()
    {
        isCooking = false;

        // Cambia colore (o effetto visivo)
        if (currentCookingInstance != null)
        {
            Renderer rend = currentCookingInstance.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = Color.red; // oppure altro effetto
        }

        // Puoi aggiungere effetto audio/particelle qui
    }

    public void ClearCookedIngredient()
    {
        if (currentCookingInstance)
            Destroy(currentCookingInstance);

        currentCookingInstance = null;
        currentIngredient = null;
        isCooking = false;
        timer = 0f;
    }

    public bool HasCookedIngredient()
    {
        return !isCooking && currentCookingInstance != null;
    }

    public void OnPlacedInReceiver()
    {
        // Puoi usare questo metodo se vuoi, ma per ora può rimanere vuoto.
        // Ad esempio: potresti aggiornare uno stato, giocare un suono, ecc.
    }


}
