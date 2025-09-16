using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class BulletinController : MonoBehaviour
{
    // Throttle per i rebuild di layout
    private float _nextLayoutRebuildTime = 0f;
    private const float LAYOUT_REBUILD_INTERVAL = 0.10f; // 10 fps max per i rebuild

    [Header("Root Options (statiche da Inspector)")]
    public List<MenuOption> mainOptions;

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

    // Live labels
    private struct LiveLine
    {
        public TMP_Text text;
        public Func<string> getter;
        public string last;
    }
    private readonly List<LiveLine> liveLines = new List<LiveLine>();

    // Bridge
    private TMP_Text readingBodyLine;
    private BulletinInteraction activeInteraction;

    [BoxGroup("Components")] public BulletinConfig bulletinConfig;

    [BoxGroup("UI")] public GameObject listPanel;
    [BoxGroup("UI")] public TMP_Text lineTemplate;

    [BoxGroup("UI Behavior")] public bool showBackWhenIdle = true;
    [BoxGroup("UI Behavior")] public string backLabel = "Back";

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
        public Color? customColor = null;

        public enum MenuAction { OpenSubmenu, ShowReading, Invoke, Label, LiveLabel }
        public MenuAction action;

        [TextArea(3, 10)] public List<string> readingPages;
        public List<MenuOption> subOptions;
        public UnityEvent onInvoke;

        [System.NonSerialized] public Func<string> dynamicTextProvider;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        if (!listPanel) Debug.LogError("[BulletinController] Assegna ListPanel nel Inspector.");

        baseStaticOptions = (mainOptions != null) ? new List<MenuOption>(mainOptions) : new List<MenuOption>();
        lastBuiltOptions = new List<MenuOption>(baseStaticOptions);
    }

    void OnEnable()
    {
        StartCoroutine(DeferredRefresh());

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += OnPhaseChanged_Global;
    }

    void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= OnPhaseChanged_Global;
    }

    private IEnumerator DeferredRefresh()
    {
        yield return null;
        RefreshNow();
    }

    public void RefreshNow()
    {
        var options = new List<MenuOption>(baseStaticOptions);

        var adapters = GetComponentsInChildren<BulletinAdapterBase>(true);
        foreach (var adapter in adapters)
        {
            options = adapter.BuildOptions(options) ?? options;
        }

        lastBuiltOptions = options;
        ShowMenu(lastBuiltOptions);
    }

    void Update()
    {
        // Aggiornamento etichette live
        if (liveLines.Count > 0)
        {
            bool anyChanged = false;
            for (int i = 0; i < liveLines.Count; i++)
            {
                var ll = liveLines[i];
                if (!ll.text) continue;
                string newText = ll.getter != null ? ll.getter() : string.Empty;
                if (newText != ll.last)
                {
                    ll.text.text = newText;
                    ll.last = newText;
                    anyChanged = true;
                }
                liveLines[i] = ll;
            }

            if (anyChanged && listPanel && Time.unscaledTime >= _nextLayoutRebuildTime)
            {
                var rt = listPanel.GetComponent<RectTransform>();
                if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                _nextLayoutRebuildTime = Time.unscaledTime + LAYOUT_REBUILD_INTERVAL;
            }
        }

        // Interazione utente
        if (!isInteracting) return;
        if (Time.frameCount == openedAtFrame) return;

        if (Input.GetKeyDown(bulletinConfig.downKey)) { MoveSelection(1); PlayMove(); }
        if (Input.GetKeyDown(bulletinConfig.upKey)) { MoveSelection(-1); PlayMove(); }

        if (Input.GetKeyDown(bulletinConfig.confirmKey))
        {
            PlayConfirm();
            if (state == MenuState.General) ConfirmGeneral();
            else ConfirmReading();
        }

        if (state == MenuState.Reading)
        {
            if (Input.GetKeyDown(bulletinConfig.prevPageKey)) { PreviousPage(); PlayMove(); }
            if (Input.GetKeyDown(bulletinConfig.nextPageKey)) { NextPage(); PlayMove(); }
        }
    }

    // ===== API =====
    public void EnterInteraction(BulletinInteraction interaction)
    {
        activeInteraction = interaction;
        isInteracting = true;
        openedAtFrame = Time.frameCount;

        menuHistory.Clear();
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

        if (currentMenuIndex == activeLines.Count - 1) { GoBack(); return; }

        int optIndex = currentMenuIndex;
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
                RefreshNow();
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

                switch (opt.action)
                {
                    case MenuOption.MenuAction.Label:
                        CreateLabelLine(parent, opt.title, opt);
                        break;
                    case MenuOption.MenuAction.LiveLabel:
                        CreateLiveLabelLine(parent, opt.dynamicTextProvider);
                        break;
                    default:
                        CreateSelectableLine(parent, opt.title, opt);
                        break;
                }
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
        t.color = opt.customColor ?? bulletinConfig.selectableNormalColor;

        activeLines.Add(t);
        selectableOptions.Add(opt);
    }

    private void CreateLabelLine(Transform parent, string text, MenuOption opt = null)
    {
        var t = SpawnLine(parent, text);
        t.color = (opt != null && opt.customColor.HasValue)
            ? opt.customColor.Value
            : bulletinConfig.labelColor;
    }

    private void CreateLiveLabelLine(Transform parent, Func<string> getter)
    {
        string initial = getter != null ? getter() : string.Empty;
        var t = SpawnLine(parent, initial);
        t.color = bulletinConfig.labelColor;
        liveLines.Add(new LiveLine { text = t, getter = getter, last = initial });
    }

    private void CreateBackLine(Transform parent)
    {
        var t = SpawnLine(parent, backLabel);
        t.color = bulletinConfig.selectableNormalColor;
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
        liveLines.Clear();
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
            t.color = (i == currentMenuIndex)
                ? bulletinConfig.selectableHighlightColor
                : bulletinConfig.selectableNormalColor;
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
        if (audioSource && bulletinConfig.sfxMove) audioSource.PlayOneShot(bulletinConfig.sfxMove);
    }

    private void PlayConfirm()
    {
        if (audioSource && bulletinConfig.sfxConfirm) audioSource.PlayOneShot(bulletinConfig.sfxConfirm);
    }

    private void OnPhaseChanged_Global(int day, DayPhase phase)
    {
        RefreshNow();
    }
}
