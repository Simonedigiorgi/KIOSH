using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class BulletinController : MonoBehaviour
{
    [Header("General Menu Config")]
    public List<MenuOption> mainOptions;

    private List<MenuOption> currentOptions;
    private List<MenuOption> rootOptions; // riferimento al main menu originale
    private Stack<List<MenuOption>> menuHistory = new Stack<List<MenuOption>>();
    private List<Button> activeButtons = new List<Button>();


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
        public enum MenuAction { OpenSubmenu, ShowReading }
        public MenuAction action;

        [TextArea(3, 10)] public List<string> readingPages;
        public List<MenuOption> subOptions;
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
                // ⬇️ Aggiungi questa riga per gestire Enter
                if (Input.GetKeyDown(KeyCode.Return)) HandleReadingMenuSelect();
                break;
        }
    }

    public void EnterInteraction()
    {
        if (hasEntered) return;
        isInteracting = true;
        ShowIntro();
    }

    public void ExitInteraction()
    {
        isInteracting = false;
        hasEntered = false;
        HideAllPanels();
        ShowIntro();

        // 🔁 Ritorna il controllo al giocatore
        FindObjectOfType<BulletinInteraction>()?.ExitInteraction();
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

        currentOptions = options;
        currentMenuIndex = 0;

        // Pulisce vecchi bottoni
        foreach (Transform child in generalMenuContainer)
            Destroy(child.gameObject);

        activeButtons.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            MenuOption opt = options[i];
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
                menuHistory.Push(currentOptions); // Salva lo stato attuale prima di entrare nel sottomenu
                ShowDynamicMenu(selected.subOptions);
                break;

            case MenuOption.MenuAction.ShowReading:
                ShowCustomReading(selected.readingPages);
                break;
        }
    }
    void ShowCustomReading(List<string> pages)
    {
        // ✅ Salva il menu attuale nello stack per tornare indietro
        menuHistory.Push(currentOptions);

        currentPages = pages.ToArray();
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
    }

    void NextPage()
    {
        if (currentPage < currentPages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    void PreviousPage()
    {
        if (currentPage > 0)
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
                    ExitInteraction(); // Sei nel menu principale → esci
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
                    ShowGeneralMenu(); // Fallback di sicurezza
                }
                break;

            default:
                ExitInteraction(); // fallback
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
