using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("UI References")]
    public Image crosshairImage;
    public TMP_Text targetNameText;

    [Header("Raycast Settings")]
    public float rayDistance = 3f;
    public LayerMask interactableLayer;

    [Header("Interaction Toggle")]
    public bool isInteracting = false;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        ClearTargetText();
    }

    void Update()
    {
        if (isInteracting)
        {
            HideUI();
            return;
        }

        ShowUI();
        UpdateTargetName();
    }

    // Esegue il raycast e aggiorna il nome del target
    void UpdateTargetName()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            // Se ha un componente con nome
            InteractableName target = hit.collider.GetComponent<InteractableName>();
            if (target != null)
            {
                targetNameText.text = target.displayName;
                return;
            }
        }

        ClearTargetText();
    }

    void ClearTargetText()
    {
        targetNameText.text = "";
    }

    void HideUI()
    {
        crosshairImage.enabled = false;
        targetNameText.enabled = false;
    }

    void ShowUI()
    {
        crosshairImage.enabled = true;
        targetNameText.enabled = true;
    }

    // Chiamati da altri script (es. BulletinInteraction)
    public void SetInteracting(bool value)
    {
        isInteracting = value;
    }
}
