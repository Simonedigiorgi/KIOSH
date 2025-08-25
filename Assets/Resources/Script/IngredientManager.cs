using System.Collections.Generic;
using UnityEngine;

public class IngredientManager : MonoBehaviour
{
    public static IngredientManager Instance;

    private readonly HashSet<string> activeIngredients = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public bool IsIngredientActive(string id)
    {
        return activeIngredients.Contains(id);
    }

    public void RegisterIngredient(string id)
    {
        activeIngredients.Add(id);
        Debug.Log($"🧠 Registrato ingrediente attivo: {id}");
    }

    public void UnregisterIngredient(string id)
    {
        activeIngredients.Remove(id);
        Debug.Log($"🧹 Rimosso ingrediente attivo: {id}");
    }
}
