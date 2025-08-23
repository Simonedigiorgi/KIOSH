using System.Collections.Generic;

[System.Serializable]
public class MenuOption
{
    public string title;
    public enum MenuAction { OpenSubmenu, ShowReading }
    public MenuAction action;

    public List<string> readingPages;   // Usato se ShowReading
    public List<MenuOption> subOptions; // Usato se OpenSubmenu
}
