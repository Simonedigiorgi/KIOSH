using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    public Image crosshairImage;
    public TMP_Text targetNameText;

    [Header("Interaction Toggle")]
    public bool isInteracting = false;

    private PlayerInteractor interactor;

    void Start()
    {
        interactor = FindObjectOfType<PlayerInteractor>();
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
        UpdateTargetNameFromInteractor();
    }

    void UpdateTargetNameFromInteractor()
    {
        if (interactor.currentTargetName != null)
        {
            targetNameText.text = interactor.currentTargetName.displayName;
        }
        else
        {
            ClearTargetText();
        }
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

    public void SetInteracting(bool value)
    {
        isInteracting = value;
    }
}
