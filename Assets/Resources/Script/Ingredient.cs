using UnityEngine;

public enum CookingToolType { Pot, Pan }

public class Ingredient : MonoBehaviour
{
    public CookingToolType compatibleTool;
    public GameObject cookedPrefab;
    public float cookTime = 5f;

    [Header("Identificativo univoco")]
    public string ingredientID;  // ← usato per controllare duplicati nei box

    [Header("Piatto finale")]
    public GameObject dishPrefab; // 👉 prefab da istanziare nel piatto
}
