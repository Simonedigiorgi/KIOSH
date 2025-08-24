using UnityEngine;

public enum CookingToolType { Pot, Pan }

public class Ingredient : MonoBehaviour
{
    public CookingToolType compatibleTool;
    public GameObject cookedPrefab;
}
