using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class BulletinController : MonoBehaviour
{
    [Header("Root Options (fornite da Adapter)")]
    public List<MenuOption> mainOptions;

    // stato menu
    private List<MenuOption> currentOptions;
    private readonly Stack<List<MenuOption>> menuHistory = new Stack<List<MenuOption>>();

    // righe UI generate (selezionabili + label + back)
    private readonly List<TMP_Text> spawnedLines = new List<TMP_Text>();
    private readonly List<TMP_Text> activeLines = new List<TMP_Text>();   // solo selezionabili + back
    private readonly List<MenuOption> selectableOptions = new List<MenuOption>(); // 1:1 con voci selezionabili
    private int currentMenuIndex = 0; // indice nell'elenco activeLines

    // bridge verso l’interazione camera/player
    private BulletinInteraction activeInteraction;

    [Header("UI")]
    public GameObject listPanel;        // pannello con VerticalLayoutGroup
    public Transform listContainer;     // se null, usa listPanel.transform
    public TMP_Text lineTemplate;       // prefab di una riga (TMP_Text) disattivato nella scena
    public TMP_Text readingText;        // testo del reading (deve avere LayoutElement con FlexibleHeight=1)

    [Header("Colors")]
    public Color labelColor = new(0.75f, 0.9f, 1f);
    public Color selectableNormalColor = Color.white;
    public Color selectableHighlightColor = Color.green;

    [Header("Testi")]
    public string backLabel = "Back";

    [Header("Input")]
    public KeyCode confirmKey = KeyCode.E;
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode prevPageKey = KeyCode.A;
    public KeyCode nextPageKey = KeyCode.D;

    // reading
    private string[] currentPages;
    private int currentPage = 0;

    // anti-doppio input (E del frame di apertura)
    private int openedAtFrame = -1;

    private enum MenuState { General, Reading }
    private MenuState state = MenuState.General;

    private bool isInteracting = false;
    public bool IsOpen => isInteracting;

    // cache parent originale del readingText (se lo spostiamo nel listContainer)
    private Transform readingOrigParent;
    private int readingOrigSibling = -1;

    [System.Serializable]
    public class MenuOption
    {
        public string title;
        public enum MenuAction { OpenSubmenu, ShowReading, Invoke, Label }
        public MenuAction action;
        [TextArea(3, 10)] public List<string> readingPages; // se ShowReading
        public List<MenuOption> subOptions;                 // se OpenSubmenu
        public UnityEvent onInvoke;                         // se Invoke
    }

    void Update()
    {
        if (!isInteracting) return;

        // evita che l’E di apertura confermi subito qualcosa
        if (Time.frameCount == openedAtFrame) return;

        if (Input.GetKeyDown(downKey)) MoveSelection(1);
        if (Input.GetKeyDown(upKey)) MoveSelection(-1);

        if (Input.GetKeyDown(confirmKey))
        {
            if (state == MenuState.General) ConfirmGeneral();
            else ConfirmReading();
        }

        if (state == MenuState.Reading)
        {
            if (Input.GetKeyDown(prevPageKey)) PreviousPage();
            if (Input.GetKeyDown(nextPageKey)) NextPage();
        }
    }

    // ===== API =====
    public void EnterInteraction(BulletinInteraction interaction)
    {
        activeInteraction = interaction;
        isInteracting = true;
        openedAtFrame = Time.frameCount;

        menuHistory.Clear();
        ShowMenu(mainOptions);
    }

    public void ExitInteraction()
    {
        isInteracting = false;

        ClearList();
        // rimettiamo il readingText al suo posto e nascondiamo
        RestoreReadingParent();
        if (readingText) readingText.gameObject.SetActive(false);

        if (activeInteraction != null)
        {
            activeInteraction.ExitInteraction(); // restituisce camera/controlli
            activeInteraction = null;
        }
    }

    public void SetRootOptions(List<MenuOption> options, bool refreshIfOpen = true)
    {
        mainOptions = options ?? new List<MenuOption>();
        if (isInteracting && refreshIfOpen)
        {
            menuHistory.Clear();
            ShowMenu(mainOptions);
        }
    }

    // ===== MENU =====
    private void ShowMenu(List<MenuOption> options)
    {
        state = MenuState.General;

        if (listPanel) listPanel.SetActive(true);

        // assicuriamoci che il readingText non resti nel container in modalità menu
        RestoreReadingParent();
        if (readingText) readingText.gameObject.SetActive(false);

        currentOptions = options ?? new List<MenuOption>();
        currentMenuIndex = 0; // sempre dalla prima voce
        BuildList(currentOptions, includeBack: true, onlyBack: false);
        UpdateHighlight();
    }

    private void ConfirmGeneral()
    {
        if (activeLines.Count == 0) return;

        // l'ultima riga è Back
        if (currentMenuIndex == activeLines.Count - 1)
        {
            GoBack();
            return;
        }

        int optIndex = currentMenuIndex; // mappa 1:1 con selectableOptions
        if (optIndex < 0 || optIndex >= selectableOptions.Count) return;

        var selected = selectableOptions[optIndex];
        switch (selected.action)
        {
            case MenuOption.MenuAction.OpenSubmenu:
                menuHistory.Push(currentOptions);
                ShowMenu(selected.subOptions);
                break;

            case MenuOption.MenuAction.ShowReading:
                ShowReading(selected.readingPages);
                break;

            case MenuOption.MenuAction.Invoke:
                selected.onInvoke?.Invoke();
                // dopo l’azione torno alla root (aggiorno eventuali contatori)
                menuHistory.Clear();
                ShowMenu(mainOptions);
                break;

            case MenuOption.MenuAction.Label:
                // non selezionabile
                break;
        }
    }

    // ===== READING =====
    private void ShowReading(List<string> pages)
    {
        state = MenuState.Reading;

        currentPages = (pages != null) ? pages.ToArray() : new string[0];
        currentPage = 0;

        // porta il readingText dentro il listContainer come PRIMO elemento
        Transform parent = listContainer ? listContainer : (listPanel ? listPanel.transform : null);
        if (readingText && parent)
        {
            if (!readingOrigParent)    // cache una sola volta
            {
                readingOrigParent = readingText.transform.parent;
                readingOrigSibling = readingText.transform.GetSiblingIndex();
            }
            readingText.transform.SetParent(parent, false);
            readingText.transform.SetSiblingIndex(0); // in alto
            readingText.gameObject.SetActive(true);
        }

        UpdatePage();

        // in reading mostriamo solo Back (ed è in fondo perché aggiunto dopo il testo)
        BuildList(null, includeBack: true, onlyBack: true);
        currentMenuIndex = activeLines.Count - 1;
        UpdateHighlight();
    }

    private void ConfirmReading() => GoBack();

    private void UpdatePage()
    {
        if (!readingText) return;
        readingText.text = (currentPages != null && currentPages.Length > 0)
            ? currentPages[currentPage]
            : string.Empty;
    }

    private void NextPage()
    {
        if (currentPages == null) return;
        if (currentPage < currentPages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    private void PreviousPage()
    {
        if (currentPages == null) return;
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    // ===== COSTRUZIONE LISTA =====
    private void BuildList(List<MenuOption> options, bool includeBack, bool onlyBack)
    {
        ClearList();

        Transform parent = listContainer ? listContainer : (listPanel ? listPanel.transform : null);
        if (!parent || !lineTemplate) return;

        // voci dall'alto verso il basso
        if (!onlyBack && options != null)
        {
            foreach (var opt in options)
            {
                if (opt.action == MenuOption.MenuAction.Label)
                {
                    CreateLine(parent, opt.title, selectable: false, isLabel: true);
                }
                else
                {
                    int idx = selectableOptions.Count;
                    selectableOptions.Add(opt);
                    CreateLine(parent, opt.title, selectable: true, isLabel: false);
                }
            }
        }

        // Back sempre in fondo
        if (includeBack)
            CreateLine(parent, backLabel, selectable: true, isLabel: false);
    }

    private void CreateLine(Transform parent, string text, bool selectable, bool isLabel)
    {
        var t = Instantiate(lineTemplate, parent);
        t.gameObject.SetActive(true);
        t.enableVertexGradient = false;            // evita gradient/material grigi
        t.color = isLabel ? labelColor : selectableNormalColor;
        t.text = text;

        spawnedLines.Add(t);

        if (selectable)
            activeLines.Add(t);
    }

    private void ClearList()
    {
        // distruggi SOLO le righe generate (non il readingText)
        for (int i = 0; i < spawnedLines.Count; i++)
            if (spawnedLines[i]) Destroy(spawnedLines[i].gameObject);

        spawnedLines.Clear();
        activeLines.Clear();
        selectableOptions.Clear();
        currentMenuIndex = 0;
    }

    private void MoveSelection(int dir)
    {
        if (activeLines.Count == 0) return;
        currentMenuIndex = (currentMenuIndex + dir + activeLines.Count) % activeLines.Count;
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            var t = activeLines[i];
            if (!t) continue;
            t.enableVertexGradient = false;
            t.color = (i == currentMenuIndex) ? selectableHighlightColor : selectableNormalColor;
        }
    }

    private void GoBack()
    {
        if (state == MenuState.Reading)
        {
            // torna al menu precedente o alla root, e rimetti a posto il reading
            RestoreReadingParent();
            ShowMenu(menuHistory.Count > 0 ? menuHistory.Peek() : mainOptions);
            return;
        }

        // state == General
        if (menuHistory.Count == 0)
        {
            ExitInteraction(); // chiude e torna al player
        }
        else
        {
            var prev = menuHistory.Pop();
            ShowMenu(prev);
        }
    }

    private void RestoreReadingParent()
    {
        if (!readingText || !readingOrigParent) return;
        readingText.transform.SetParent(readingOrigParent, false);
        if (readingOrigSibling >= 0)
            readingText.transform.SetSiblingIndex(readingOrigSibling);
        readingText.gameObject.SetActive(false);
    }
}
