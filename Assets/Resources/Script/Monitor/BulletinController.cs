using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events; // ✅ necessario per UnityEvent

public class BulletinController : MonoBehaviour
{
    [Header("General Menu Config")]
    public List<MenuOption> mainOptions;

    private List<MenuOption> currentOptions;
    private List<MenuOption> rootOptions; // riferimento al main menu originale
    private Stack<List<MenuOption>> menuHistory = new Stack<List<MenuOption>>();
    private List<Button> activeButtons = new List<Button>();
    private BulletinInteraction activeInteraction;

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject commandPanel;
    public GameObject generalMenuPanel;
    public GameObject readingGroup;
    public Button backMenuButton; // Assegna da inspector

    [Header("UI References")]
    public Button menuButtonTemplate; // Prefab nascosto da clonare
    public Transform generalMenuContainer; // Container per istanziare i bottoni
    public TMP_Text bulletinText;
    public Button leftButton;
    public Button rightButton;

    private int currentPage = 0;
    private int currentMenuIndex = 0;
    private string[] currentPages;

    private enum MenuState { None, Intro, General, Reading }
    private MenuState currentState = MenuState.Intro;
    private bool isInteracting = false;
    private bool hasEntered = false;

    [System.Serializable]
    public class MenuOption
    {
        public string title;

        // ✅ aggiunto "Invoke"
        public enum MenuAction { OpenSubmenu, ShowReading, Invoke }
        public MenuAction action;

        [TextArea(3, 10)] public List<string> readingPages;   // usato se ShowReading
        public List<MenuOption> subOptions;                   // usato se OpenSubmenu

        // ✅ aggiunto: azione da eseguire quando la voce è di tipo Invoke
        public UnityEvent onInvoke;
    }

    void Start()
    {
        ShowIntro();

        if (backMenuButton != null)
            backMenuButton.onClick.AddListener(OnBackMenuPressed);
    }

    void Update()
    {
        if (!isInteracting) return;

        switch (currentState)
        {
            case MenuState.Intro:
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    hasEntered = true;
                    ShowGeneralMenu();
                }
                break;

            case MenuState.General:
                HandleMenuNavigation();
                if (Input.GetKeyDown(KeyCode.Return)) HandleGeneralMenuSelect();
                break;

            case MenuState.Reading:
                if (Input.GetKeyDown(KeyCode.A)) PreviousPage();
                if (Input.GetKeyDown(KeyCode.D)) NextPage();
                if (Input.GetKeyDown(KeyCode.Return)) HandleReadingMenuSelect();
                break;
        }
    }

    public void EnterInteraction(BulletinInteraction interaction)
    {
        activeInteraction = interaction;
        isInteracting = true;
        hasEntered = false;
        ShowIntro();
    }

    public void ExitInteraction()
    {
        isInteracting = false;
        hasEntered = false;
        HideAllPanels();
        ShowIntro();
    }

    void ShowIntro()
    {
        currentState = MenuState.Intro;
        HideAllPanels();
        introPanel.SetActive(true);
        commandPanel.SetActive(false);
    }

    void ShowGeneralMenu()
    {
        menuHistory.Clear();
        rootOptions = mainOptions;
        ShowDynamicMenu(mainOptions);
    }

    void ShowDynamicMenu(List<MenuOption> options)
    {
        currentState = MenuState.General;
        HideAllPanels();
        generalMenuPanel.SetActive(true);
        commandPanel.SetActive(true);

        currentOptions = options ?? new List<MenuOption>(); // ✅ safety
        currentMenuIndex = 0;

        // Pulisce vecchi bottoni
        foreach (Transform child in generalMenuContainer)
            Destroy(child.gameObject);

        activeButtons.Clear();

        for (int i = 0; i < currentOptions.Count; i++)
        {
            int index = i;
            MenuOption opt = currentOptions[i];
            Button newBtn = Instantiate(menuButtonTemplate, generalMenuContainer);
            newBtn.gameObject.SetActive(true);
            newBtn.GetComponentInChildren<TMP_Text>().text = opt.title;

            newBtn.onClick.AddListener(() =>
            {
                currentMenuIndex = index;
                HandleGeneralMenuSelect();
            });

            activeButtons.Add(newBtn);
        }

        // Aggiungi il tasto Back alla lista
        if (backMenuButton != null)
        {
            activeButtons.Add(backMenuButton);
        }

        StartCoroutine(DelayedHighlight());
    }

    System.Collections.IEnumerator DelayedHighlight()
    {
        yield return null; // aspetta un frame
        UpdateMenuHighlight();
    }

    void HandleGeneralMenuSelect()
    {
        if (currentMenuIndex >= activeButtons.Count) return;

        Button selectedBtn = activeButtons[currentMenuIndex];

        if (selectedBtn == backMenuButton)
        {
            OnBackMenuPressed();
            return;
        }

        if (currentOptions == null || currentMenuIndex >= currentOptions.Count) return;

        var selected = currentOptions[currentMenuIndex];
        switch (selected.action)
        {
            case MenuOption.MenuAction.OpenSubmenu:
                menuHistory.Push(currentOptions);
                ShowDynamicMenu(selected.subOptions);
                break;

            case MenuOption.MenuAction.ShowReading:
                ShowCustomReading(selected.readingPages);
                break;

            case MenuOption.MenuAction.Invoke: // ✅ nuovo caso supportato
                if (selected.onInvoke != null)
                    selected.onInvoke.Invoke();
                // dopo l’azione, ricarica il menu (aggiorna UI)
                ShowGeneralMenu();
                break;
        }
    }

    void ShowCustomReading(List<string> pages)
    {
        // ✅ Salva il menu attuale nello stack per tornare indietro
        menuHistory.Push(currentOptions);

        currentPages = (pages != null) ? pages.ToArray() : new string[0];
        currentPage = 0;

        leftButton.gameObject.SetActive(currentPages.Length > 1);
        rightButton.gameObject.SetActive(currentPages.Length > 1);

        UpdatePage();
        ShowReading();
    }

    void ShowReading()
    {
        currentState = MenuState.Reading;
        HideAllPanels();
        readingGroup.SetActive(true);
        commandPanel.SetActive(true);

        // ⬇️ Aggiorna bottoni attivi per la lettura (solo il back)
        activeButtons.Clear();
        if (backMenuButton != null)
        {
            activeButtons.Add(backMenuButton);
            currentMenuIndex = 0;
            UpdateMenuHighlight();
        }
    }

    void UpdatePage()
    {
        if (currentPages != null && currentPages.Length > 0)
            bulletinText.text = currentPages[currentPage];
        else
            bulletinText.text = string.Empty;
    }

    void NextPage()
    {
        if (currentPages != null && currentPage < currentPages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    void PreviousPage()
    {
        if (currentPages != null && currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    void HideAllPanels()
    {
        introPanel.SetActive(false);
        generalMenuPanel.SetActive(false);
        readingGroup.SetActive(false);
        commandPanel.SetActive(false);
    }

    void HandleMenuNavigation()
    {
        int count = activeButtons.Count;
        if (count == 0) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            currentMenuIndex = (currentMenuIndex + 1) % count;
            UpdateMenuHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            currentMenuIndex = (currentMenuIndex - 1 + count) % count;
            UpdateMenuHighlight();
        }
    }

    void UpdateMenuHighlight()
    {
        for (int i = 0; i < activeButtons.Count; i++)
        {
            var text = activeButtons[i].GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.color = (i == currentMenuIndex) ? Color.green : Color.white;
        }
    }

    public void ForceBackToIntro()
    {
        currentState = MenuState.Intro;
        HideAllPanels();
        introPanel.SetActive(true);
        commandPanel.SetActive(false); // solo intro visibile
    }

    void OnBackMenuPressed()
    {
        switch (currentState)
        {
            case MenuState.General:
                if (currentOptions == rootOptions)
                {
                    // Chiudi UI
                    ExitInteraction();
                    // Ripristina camera/player
                    activeInteraction?.ExitInteraction();
                    activeInteraction = null;
                }
                else if (menuHistory.Count > 0)
                {
                    var previousMenu = menuHistory.Pop();
                    ShowDynamicMenu(previousMenu);
                }
                break;

            case MenuState.Reading:
                if (menuHistory.Count > 0)
                {
                    var previousMenu = menuHistory.Pop();
                    ShowDynamicMenu(previousMenu);
                }
                else
                {
                    ShowGeneralMenu();
                }
                break;

            default:
                ExitInteraction();
                activeInteraction?.ExitInteraction();
                activeInteraction = null;
                break;
        }
    }


    void HandleReadingMenuSelect()
    {
        if (currentMenuIndex >= activeButtons.Count) return;

        Button selectedBtn = activeButtons[currentMenuIndex];

        if (selectedBtn == backMenuButton)
        {
            OnBackMenuPressed();
        }
    }
}
