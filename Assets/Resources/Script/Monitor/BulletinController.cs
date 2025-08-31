using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BulletinController : MonoBehaviour
{
    [Header("Root Options (statiche da Inspector)")]
    public List<MenuOption> mainOptions; // <-- SOLO base statiche

    // Copia immutabile delle opzioni di base (snapshot preso in Awake)
    private List<MenuOption> baseStaticOptions;
    // Ultime opzioni “costruite” dagli adapter (solo per mostrare a schermo)
    private List<MenuOption> lastBuiltOptions;

    // Stato / stack
    private List<MenuOption> currentOptions;
    private readonly Stack<List<MenuOption>> menuHistory = new Stack<List<MenuOption>>();

    // Righe generate
    private readonly List<TMP_Text> spawnedLines = new List<TMP_Text>();
    private readonly List<TMP_Text> activeLines = new List<TMP_Text>();
    private readonly List<MenuOption> selectableOptions = new List<MenuOption>();
    private int currentMenuIndex = 0;

    // Bridge
    private TMP_Text readingBodyLine;
    private BulletinInteraction activeInteraction;

    [Header("UI")]
    public GameObject listPanel;
    public TMP_Text lineTemplate;

    [Header("UI Behavior")]
    public bool showBackWhenIdle = true;

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

    [Header("Audio")]
    public AudioClip sfxMove;
    public AudioClip sfxConfirm;
    private AudioSource audioSource;

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
        [TextArea(3, 10)] public List<string> readingPages;
        public List<MenuOption> subOptions;
        public UnityEvent onInvoke;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        if (!listPanel) Debug.LogError("[BulletinController] Assegna ListPanel nel Inspector.");

        // Prendiamo una snapshot delle opzioni statiche impostate da Inspector
        baseStaticOptions = (mainOptions != null) ? new List<MenuOption>(mainOptions)
                                                  : new List<MenuOption>();
        // all’avvio mostriamo almeno le statiche
        lastBuiltOptions = new List<MenuOption>(baseStaticOptions);
    }

    void OnEnable()
    {
        StartCoroutine(DeferredRefresh());
    }

    private IEnumerator DeferredRefresh()
    {
        yield return null;
        RefreshNow();
    }

    /// <summary>
    /// Ricostruisce le opzioni da mostrare partendo SEMPRE dalle statiche
    /// e applicando tutti gli Adapter trovati nel pannello.
    /// </summary>
    public void RefreshNow()
    {
        // riparti sempre dalle opzioni statiche salvate in Awake
        var options = new List<MenuOption>(baseStaticOptions);

        // applica tutti gli adapter presenti nel pannello
        var adapters = GetComponentsInChildren<BulletinAdapterBase>(true);
        foreach (var adapter in adapters)
        {
            options = adapter.BuildOptions(options) ?? options;
        }

        // memorizza l’ultima build (solo per UI)
        lastBuiltOptions = options;

        // mostra a schermo (senza toccare le opzioni statiche)
        ShowMenu(lastBuiltOptions);
    }

    void Update()
    {
        if (!isInteracting) return;
        if (Time.frameCount == openedAtFrame) return;

        if (Input.GetKeyDown(downKey)) { MoveSelection(1); PlayMove(); }
        if (Input.GetKeyDown(upKey)) { MoveSelection(-1); PlayMove(); }

        if (Input.GetKeyDown(confirmKey))
        {
            PlayConfirm();
            if (state == MenuState.General) ConfirmGeneral();
            else ConfirmReading();
        }

        if (state == MenuState.Reading)
        {
            if (Input.GetKeyDown(prevPageKey)) { PreviousPage(); PlayMove(); }
            if (Input.GetKeyDown(nextPageKey)) { NextPage(); PlayMove(); }
        }
    }

    // ===== API =====
    public void EnterInteraction(BulletinInteraction interaction)
    {
        activeInteraction = interaction;
        isInteracting = true;
        openedAtFrame = Time.frameCount;

        menuHistory.Clear();
        // mostra l’ultima build se esiste, altrimenti le statiche
        ShowMenu(lastBuiltOptions ?? baseStaticOptions);
    }

    public void ExitInteraction()
    {
        isInteracting = false;
        state = MenuState.General;

        ShowMenu(lastBuiltOptions ?? baseStaticOptions);
        openedAtFrame = Time.frameCount;

        activeInteraction?.ExitInteraction();
        activeInteraction = null;
    }

    // ===== MENU =====
    private void ShowMenu(List<MenuOption> options)
    {
        state = MenuState.General;
        if (listPanel) listPanel.SetActive(true);

        currentOptions = options ?? new List<MenuOption>();
        currentMenuIndex = 0;

        bool includeBack = isInteracting || showBackWhenIdle;
        BuildList(currentOptions, includeBack: includeBack, onlyBack: false);
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
                menuHistory.Clear();
                // dopo un’azione, ricalcoliamo la build corrente
                RefreshNow();
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

        ClearList();
        Transform parent = listPanel ? listPanel.transform : null;
        if (!parent || !lineTemplate) return;

        readingBodyLine = Instantiate(lineTemplate, parent);
        readingBodyLine.gameObject.SetActive(true);
        readingBodyLine.alignment = TextAlignmentOptions.TopLeft;
        readingBodyLine.enableWordWrapping = true;
        readingBodyLine.overflowMode = TextOverflowModes.Overflow;

        var rt = readingBodyLine.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);

        var le = readingBodyLine.GetComponent<LayoutElement>();
        if (le) Destroy(le);

        UpdateReadingBodyTextAndHeight();

        CreateBackLine(parent);

        currentMenuIndex = activeLines.Count - 1;
        UpdateHighlight();

        StartCoroutine(RebuildEndOfFrame());
    }

    private IEnumerator RebuildEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (listPanel)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listPanel.GetComponent<RectTransform>());
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

    private void BuildList(List<MenuOption> options, bool includeBack, bool onlyBack)
    {
        ClearList();

        var parent = listPanel ? listPanel.transform : null;
        if (!parent || !lineTemplate) return;

        if (!onlyBack && options != null)
        {
            foreach (var opt in options)
            {
                if (opt == null) continue;

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

        t.enableAutoSizing = false;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.text = text;

        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);

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
        t.color = labelColor;
    }

    private void CreateBackLine(Transform parent)
    {
        var t = SpawnLine(parent, backLabel);
        t.color = selectableNormalColor;
        activeLines.Add(t);
    }

    private void ClearList()
    {
        if (listPanel)
        {
            var parent = listPanel.transform;
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
            ShowMenu(menuHistory.Count > 0 ? menuHistory.Peek() : (lastBuiltOptions ?? baseStaticOptions));
            return;
        }

        if (menuHistory.Count == 0) ExitInteraction();
        else ShowMenu(menuHistory.Pop());
    }

    private void UpdatePage() => UpdateReadingBodyTextAndHeight();

    private void UpdateReadingBodyTextAndHeight()
    {
        if (!readingBodyLine) return;

        string body = GetCurrentPageText();
        string prefix = (currentPages != null && currentPages.Length > 1)
            ? $"Pag. {currentPage + 1}/{currentPages.Length} - "
            : "";

        readingBodyLine.text = prefix + body;

        readingBodyLine.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        if (listPanel)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listPanel.GetComponent<RectTransform>());
    }

    private void PlayMove()
    {
        if (audioSource && sfxMove) audioSource.PlayOneShot(sfxMove);
    }

    private void PlayConfirm()
    {
        if (audioSource && sfxConfirm) audioSource.PlayOneShot(sfxConfirm);
    }
}
