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

    // ---------- Blackout ----------
    public void ShowBlackout()
    {
        if (blackoutPanel)
            blackoutPanel.SetActive(true);
    }

    public IEnumerator FadeBlackout(float duration)
    {
        if (!blackoutPanel) yield break;

        blackoutPanel.SetActive(true);

        var canvasGroup = blackoutPanel.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = blackoutPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
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
