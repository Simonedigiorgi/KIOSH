using System.Collections.Generic;
using UnityEngine;

public class IngredientManager : MonoBehaviour
{
    public static IngredientManager Instance { get; private set; }

    private readonly HashSet<string> activeIngredients = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this; // NON persistente: nessun DontDestroyOnLoad
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null; // evita riferimenti a oggetti distrutti dopo il load
    }

    public bool IsIngredientActive(string id) => activeIngredients.Contains(id);

    public void RegisterIngredient(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        activeIngredients.Add(id);
        Debug.Log($"🧠 Registrato ingrediente attivo: {id}");
    }

    public void UnregisterIngredient(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        activeIngredients.Remove(id);
        Debug.Log($"🧹 Rimosso ingrediente attivo: {id}");
    }

    // Utility opzionale, se vuoi pulire manualmente lo stato
    public void ClearAll() => activeIngredients.Clear();
}
