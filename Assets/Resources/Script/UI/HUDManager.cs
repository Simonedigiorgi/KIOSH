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
        interactor = FindObjectOfType<PlayerInteractor>();
        playerController = FindObjectOfType<PlayerController>();

        ClearTargetText();

        if (dialogPanel)
            dialogPanel.SetActive(false);

        if (blackoutPanel)
            blackoutPanel.SetActive(false); // spento all’avvio
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
            if (nameComp != null)
                targetNameText.text = nameComp.displayName;
            else
                ClearTargetText();
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
    public void SetInteracting(bool value)
    {
        isInteracting = value;
    }

    // ---------- Blackout (helper) ----------
    CanvasGroup GetOrAddBlackoutCanvasGroup()
    {
        if (!blackoutPanel) return null;
        var cg = blackoutPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = blackoutPanel.AddComponent<CanvasGroup>();
        return cg;
    }

    /// <summary>Attiva il pannello blackout (senza toccare l'alpha).</summary>
    public void ShowBlackout()
    {
        if (blackoutPanel)
            blackoutPanel.SetActive(true);
    }

    /// <summary>Disattiva il pannello blackout.</summary>
    public void HideBlackout()
    {
        if (blackoutPanel)
            blackoutPanel.SetActive(false);
    }

    /// <summary>Setta immediatamente alpha del blackout (0..1). Attiva il pannello se serve.</summary>
    public void SetBlackoutAlpha(float a)
    {
        if (!blackoutPanel) return;
        blackoutPanel.SetActive(true);
        var cg = GetOrAddBlackoutCanvasGroup();
        if (cg) cg.alpha = Mathf.Clamp01(a);
    }

    /// <summary>Imposta subito nero pieno (alpha=1) e attiva il pannello.</summary>
    public void ShowBlackoutImmediateFull()
    {
        SetBlackoutAlpha(1f);
    }

    /// <summary>Fade IN: da 0 a 1 in duration secondi (lascia attivo e nero).</summary>
    public IEnumerator FadeBlackout(float duration)
    {
        if (!blackoutPanel) yield break;

        blackoutPanel.SetActive(true);
        var canvasGroup = GetOrAddBlackoutCanvasGroup();

        float from = 0f;
        float to = 1f;

        canvasGroup.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    /// <summary>Fade OUT: da 1 a 0 in duration secondi (alla fine lascia attivo ma trasparente).</summary>
    public IEnumerator FadeBlackoutOut(float duration)
    {
        if (!blackoutPanel) yield break;

        blackoutPanel.SetActive(true);
        var canvasGroup = GetOrAddBlackoutCanvasGroup();

        float from = 1f;
        float to = 0f;

        canvasGroup.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        canvasGroup.alpha = to;
        // Se preferisci spegnerlo del tutto alla fine, decommenta:
        // blackoutPanel.SetActive(false);
    }

    // ---------- Dialoghi ----------
    public void ShowDialog(HUDMessageSet messageSet)
    {
        if (messageSet == null || messageSet.lines == null || messageSet.lines.Length == 0)
            return;

        StartDialog(messageSet.lines);
    }

    public void ShowDialog(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        StartDialog(new string[] { message });
    }

    private void StartDialog(IEnumerable<string> lines)
    {
        dialogQueue.Clear();
        foreach (var line in lines)
            dialogQueue.Enqueue(line);

        isShowingDialog = true;
        if (dialogPanel) dialogPanel.SetActive(true);

        // blocca movimento/rotazione player
        playerController?.SetControlsEnabled(false);

        if (dialogText && dialogQueue.Count > 0)
            dialogText.text = dialogQueue.Peek();

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
