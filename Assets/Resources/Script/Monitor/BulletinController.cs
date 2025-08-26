using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BulletinController : MonoBehaviour
{
    [Header("Root Options (fornite da Adapter)")]
    public List<MenuOption> mainOptions;

    // Stato / stack
    private List<MenuOption> currentOptions;
    private readonly Stack<List<MenuOption>> menuHistory = new Stack<List<MenuOption>>();

    // Righe generate
    private readonly List<TMP_Text> spawnedLines = new List<TMP_Text>();  // tutte le istanze
    private readonly List<TMP_Text> activeLines = new List<TMP_Text>();  // solo selezionabili (+ Back)
    private readonly List<MenuOption> selectableOptions = new List<MenuOption>(); // 1:1 con activeLines (tranne Back)
    private int currentMenuIndex = 0;

    // Bridge verso interazione player/camera
    private TMP_Text readingBodyLine;   // riga multilinea del reading
    private BulletinInteraction activeInteraction;

    [Header("UI")]
    public GameObject listPanel;              // ha VerticalLayoutGroup + ContentSizeFitter
    public Transform listContainer;           // se null, usa listPanel.transform
    public TMP_Text lineTemplate;             // prefab TMP_Text (DISATTIVATO in scena)
    public VerticalLayoutGroup listLayout;    // (opzionale) VerticalLayoutGroup del ListPanel da forzare

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

    // Reading
    private string[] currentPages;
    private int currentPage = 0;

    // Anti doppio input
    private int openedAtFrame = -1;

    private enum MenuState { General, Reading }
    private MenuState state = MenuState.General;

    private bool isInteracting = false;
    public bool IsOpen => isInteracting;

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

    void Awake()
    {
        if (listLayout != null)
        {
            listLayout.reverseArrangement = false;
            listLayout.childAlignment = TextAnchor.UpperLeft;

            // LAYOUT: il layout controlla dimensioni dei figli
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;   // ✅ PRIMA era false: va messo a true!

            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
        }
    }


    void Update()
    {
        if (!isInteracting) return;
        if (Time.frameCount == openedAtFrame) return; // evita l’“E” del frame di apertura

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
        if (listPanel) listPanel.SetActive(false);

        activeInteraction?.ExitInteraction();
        activeInteraction = null;
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

        currentOptions = options ?? new List<MenuOption>();
        currentMenuIndex = 0;

        BuildList(currentOptions, includeBack: true, onlyBack: false);
        UpdateHighlight();
    }

    private void ConfirmGeneral()
    {
        if (activeLines.Count == 0) return;

        // ultima riga = Back
        if (currentMenuIndex == activeLines.Count - 1) { GoBack(); return; }

        int optIndex = currentMenuIndex; // 1:1 con selectableOptions
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
                menuHistory.Clear();         // torna alla root (aggiorna contatori ecc.)
                ShowMenu(mainOptions);
                break;

            case MenuOption.MenuAction.Label:
                break;
        }
    }

    // ===== READING =====
    private void ShowReading(List<string> pages)
    {
        state = MenuState.Reading;

        currentPages = (pages != null) ? pages.ToArray() : new string[0];
        currentPage = 0;

        if (listPanel) listPanel.SetActive(true);

        // Costruisci: corpo + Back
        ClearList();
        Transform parent = GetContainer();
        if (!parent || !lineTemplate) return;

        // 1) Corpo (multilinea, auto-height)
        readingBodyLine = Instantiate(lineTemplate, parent);
        readingBodyLine.gameObject.SetActive(true);
        readingBodyLine.alignment = TextAlignmentOptions.TopLeft;
        readingBodyLine.enableWordWrapping = true;
        readingBodyLine.overflowMode = TextOverflowModes.Overflow;

        // stretch orizzontale
        var rt = readingBodyLine.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);

        // 🔧 rimuovi qualunque LayoutElement ereditato dal prefab
        var le = readingBodyLine.GetComponent<LayoutElement>();
        if (le) Destroy(le);

        // testo + calcolo altezza (TMP preferred)
        UpdateReadingBodyTextAndHeight();

        // 2) Back sempre in fondo
        CreateBackLine(parent);

        // selezione di default sul Back (ultima voce)
        currentMenuIndex = activeLines.Count - 1;
        UpdateHighlight();

        // ✅ rebuild DOPO aver creato anche Back
        StartCoroutine(RebuildEndOfFrame());
    }

    private IEnumerator RebuildEndOfFrame()
    {
        // aspetta la fine del frame per avere le mesh TMP aggiornate
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        var root = (listPanel != null) ? listPanel.GetComponent<RectTransform>() : null;
        if (root != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }


    private void ConfirmReading() => GoBack();

    private string GetCurrentPageText()
        => (currentPages != null && currentPages.Length > 0) ? currentPages[currentPage] : string.Empty;

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
    private Transform GetContainer() => listContainer ? listContainer : (listPanel ? listPanel.transform : null);

    private void BuildList(List<MenuOption> options, bool includeBack, bool onlyBack)
    {
        ClearList();

        var parent = GetContainer();
        if (!parent || !lineTemplate) return;

        if (!onlyBack && options != null)
        {
            foreach (var opt in options)
            {
                if (opt.action == MenuOption.MenuAction.Label)
                    CreateLabelLine(parent, opt.title);
                else
                    CreateSelectableLine(parent, opt.title, opt);
            }
        }

        if (includeBack) CreateBackLine(parent);
    }

    private TMP_Text SpawnLine(Transform parent, string text)
    {
        var t = Instantiate(lineTemplate, parent);
        t.gameObject.SetActive(true);

        // Testo
        t.enableAutoSizing = false;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.text = text;

        // Stretched in orizzontale
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);

        // ✅ IMPORTANTE: rimuovi qualsiasi LayoutElement del prefab (bloccherebbe l'altezza)
        var le = t.GetComponent<LayoutElement>();
        if (le) Destroy(le);

        spawnedLines.Add(t);
        return t;
    }


    private void CreateSelectableLine(Transform parent, string text, MenuOption opt)
    {
        var t = SpawnLine(parent, text);
        t.color = selectableNormalColor;

        activeLines.Add(t);
        selectableOptions.Add(opt);
    }

    private void CreateLabelLine(Transform parent, string text)
    {
        var t = SpawnLine(parent, text);
        t.color = labelColor; // non selezionabile
    }

    private void CreateBackLine(Transform parent)
    {
        var t = SpawnLine(parent, backLabel);
        t.color = selectableNormalColor;
        activeLines.Add(t); // selezionabile (ma NON in selectableOptions)
    }

    private void ClearList()
    {
        Transform parent = GetContainer();
        if (parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
        readingBodyLine = null;
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
            t.color = (i == currentMenuIndex) ? selectableHighlightColor : selectableNormalColor;
        }
    }

    private void GoBack()
    {
        if (state == MenuState.Reading)
        {
            ShowMenu(menuHistory.Count > 0 ? menuHistory.Peek() : mainOptions);
            return;
        }

        if (menuHistory.Count == 0) ExitInteraction();
        else ShowMenu(menuHistory.Pop());
    }

    private void UpdatePage() => UpdateReadingBodyTextAndHeight();

    // --- helper ---
    private void UpdateReadingBodyTextAndHeight()
    {
        if (!readingBodyLine) return;

        string txt = (currentPages != null && currentPages.Length > 0)
            ? currentPages[currentPage]
            : string.Empty;

        readingBodyLine.text = txt;

        // Aggiorna subito la mesh di TMP
        readingBodyLine.ForceMeshUpdate();

        // Rebuild immediato del layout
        Canvas.ForceUpdateCanvases();
        var root = (listPanel != null) ? listPanel.GetComponent<RectTransform>() : null;
        if (root != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }
}
