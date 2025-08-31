using UnityEngine;

[CreateAssetMenu(menuName = "UI/HUD Message Set")]
public class HUDMessageSet : ScriptableObject
{
    [TextArea(2, 5)]
    public string[] lines;
}
