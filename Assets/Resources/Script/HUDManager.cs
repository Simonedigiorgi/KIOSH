using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI References")]
    public Image crosshairImage;
    public TMP_Text targetNameText;

    [Header("Dialog Box (bloccante)")]
    public TMP_Text dialogText;
    public GameObject dialogPanel;

    [Header("Dialog Input")]
    public KeyCode advanceKey = KeyCode.E;
    [Tooltip("Piccolo ritardo per evitare che il primo E salti subito la prima riga")]
    public float advanceDebounce = 0.15f;

    // Stato
    private readonly Queue<string> dialogQueue = new Queue<string>();
    private bool isShowingDialog = false;
    private float allowAdvanceAt = 0f;     // gate anti-skip

    // Per nascondere crosshair/nome durante interazioni (es. Bulletin)
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
        if (dialogPanel) dialogPanel.SetActive(false);
    }

    void Update()
    {
        // Dialog aperto: nascondi HUD base e ascolta Advance
        if (isShowingDialog)
        {
            HideUI();

            if (Time.time >= allowAdvanceAt && Input.GetKeyDown(advanceKey))
                AdvanceDialog();

            return;
        }

        // Interazione speciale (es. Bulletin)
        if (isInteracting)
        {
            HideUI();
            return;
        }

        // HUD normale
        ShowUI();
        UpdateTargetNameFromInteractor();
    }

    // ---------- Nome target ----------
    void UpdateTargetNameFromInteractor()
    {
        if (interactor != null && interactor.currentTargetName != null)
            targetNameText.text = interactor.currentTargetName.displayName;
        else
            ClearTargetText();
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

    // ---------- Per Bulletin / interazioni che non sono dialoghi ----------
    public void SetInteracting(bool value)
    {
        isInteracting = value;
    }

    // ---------- DIALOG (bloccante) ----------
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

        // Mostra subito la PRIMA riga SENZA consumare E nello stesso frame
        if (dialogText && dialogQueue.Count > 0)
            dialogText.text = dialogQueue.Peek();

        // gate anti-skip
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
