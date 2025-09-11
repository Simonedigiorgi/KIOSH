using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI References")]
    public Image crosshairImage;
    public TMP_Text targetNameText;

    [Header("Dialog Box (bloccante)")]
    public TMP_Text dialogText;
    public GameObject dialogPanel;

    [Header("Blackout")]
    public GameObject blackoutPanel; // Pannello nero a schermo intero (Canvas UI)

    [Header("Dialog Input")]
    public KeyCode advanceKey = KeyCode.E;
    [Tooltip("Piccolo ritardo per evitare che il primo E salti subito la prima riga")]
    public float advanceDebounce = 0.15f;

    // Stato dialoghi
    private readonly Queue<string> dialogQueue = new Queue<string>();
    private bool isShowingDialog = false;
    private float allowAdvanceAt = 0f;

    // Stato interazioni (es. Board)
    private bool isInteracting = false;

    private PlayerInteractor interactor;
    private PlayerController playerController;

    public bool IsDialogOpen => isShowingDialog;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        ClearTargetText();

        if (dialogPanel) dialogPanel.SetActive(false);
        if (blackoutPanel) blackoutPanel.SetActive(false);
    }

    void Update()
    {
        // --- Dialoghi bloccanti ---
        if (isShowingDialog)
        {
            HideUI();

            if (Time.time >= allowAdvanceAt && Input.GetKeyDown(advanceKey))
                AdvanceDialog();

            return;
        }

        // --- Interazioni speciali (es. Bulletin) ---
        if (isInteracting)
        {
            HideUI();
            return;
        }

        // --- HUD normale ---
        ShowUI();
        UpdateTargetNameFromInteractor();
    }

    // ---------- Target ----------
    void UpdateTargetNameFromInteractor()
    {
        if (interactor != null && interactor.currentTarget != null)
        {
            var nameComp = interactor.currentTarget.GetComponentInParent<InteractableName>();
            if (nameComp != null) targetNameText.text = nameComp.displayName;
            else ClearTargetText();
        }
        else
        {
            ClearTargetText();
        }
    }

    void ClearTargetText() => targetNameText.text = "";

    void HideUI()
    {
        if (crosshairImage) crosshairImage.enabled = false;
        if (targetNameText) targetNameText.enabled = false;
    }

    void ShowUI()
    {
        if (crosshairImage) crosshairImage.enabled = true;
        if (targetNameText) targetNameText.enabled = true;
    }

    // ---------- Interazioni ----------
    public void SetInteracting(bool value) => isInteracting = value;

    // ---------- Blackout (helper) ----------
    CanvasGroup GetOrAddBlackoutCanvasGroup()
    {
        if (!blackoutPanel) return null;
        var cg = blackoutPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = blackoutPanel.AddComponent<CanvasGroup>();
        return cg;
    }

    public void ShowBlackout()
    {
        if (blackoutPanel) blackoutPanel.SetActive(true);
    }

    public void HideBlackout()
    {
        if (blackoutPanel) blackoutPanel.SetActive(false);
    }

    public void SetBlackoutAlpha(float a)
    {
        if (!blackoutPanel) return;
        blackoutPanel.SetActive(true);
        var cg = GetOrAddBlackoutCanvasGroup();
        if (cg) cg.alpha = Mathf.Clamp01(a);
    }

    public void ShowBlackoutImmediateFull() => SetBlackoutAlpha(1f);

    public IEnumerator FadeBlackout(float duration)
    {
        if (!blackoutPanel) yield break;

        blackoutPanel.SetActive(true);
        var canvasGroup = GetOrAddBlackoutCanvasGroup();

        float from = 0f, to = 1f;
        canvasGroup.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    public IEnumerator FadeBlackoutOut(float duration)
    {
        if (!blackoutPanel) yield break;

        blackoutPanel.SetActive(true);
        var canvasGroup = GetOrAddBlackoutCanvasGroup();

        float from = 1f, to = 0f;
        canvasGroup.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
        // Se vuoi spegnerlo del tutto:
        // blackoutPanel.SetActive(false);
    }

    // ---------- Dialoghi (NUOVE API, senza HUDMessageSet) ----------
    /// <summary>Mostra un singolo messaggio.</summary>
    public void ShowDialog(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        StartDialog(new string[] { message });
    }

    /// <summary>Mostra una sequenza di righe (params).</summary>
    public void ShowDialogLines(params string[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        StartDialog(lines);
    }

    /// <summary>Mostra una sequenza di righe (List).</summary>
    public void ShowDialogList(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return;
        StartDialog(lines);
    }

    /// <summary>Mostra righe caricate da un TextAsset (una riga per newline).</summary>
    public void ShowDialogFromTextAsset(TextAsset textAsset)
    {
        if (!textAsset) return;
        var lines = textAsset.text.Replace("\r\n", "\n").Split('\n');
        StartDialog(lines);
    }

    // ---------- Interni dialog ----------
    private void StartDialog(IEnumerable<string> lines)
    {
        dialogQueue.Clear();
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line))
                dialogQueue.Enqueue(line);
        }

        if (dialogQueue.Count == 0) return;

        isShowingDialog = true;
        if (dialogPanel) dialogPanel.SetActive(true);

        // blocca movimento/rotazione player
        playerController?.SetControlsEnabled(false);

        if (dialogText) dialogText.text = dialogQueue.Peek();

        // evita skip immediato
        allowAdvanceAt = Time.time + advanceDebounce;
    }

    private void AdvanceDialog()
    {
        if (dialogQueue.Count > 0) dialogQueue.Dequeue();

        if (dialogQueue.Count == 0)
        {
            EndDialog();
            return;
        }

        if (dialogText) dialogText.text = dialogQueue.Peek();
        allowAdvanceAt = Time.time + advanceDebounce;
    }

    private void EndDialog()
    {
        isShowingDialog = false;
        if (dialogPanel) dialogPanel.SetActive(false);
        playerController?.SetControlsEnabled(true);
    }
}
