using System.Collections;
using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTargetPosition;
    public Camera playerCamera;
    public HUDManager HudManager;
    public BulletinController bulletinController;
    public PlayerController playerController;

    [Header("Camera Transition")]
    public float transitionTime = 0.5f;
    [Tooltip("Curva di interpolazione (0→1) per la transizione camera")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Backup camera
    private Transform originalCamParent;
    private Vector3 originalWorldPos;
    private Quaternion originalWorldRot;

    private bool isInteracting = false;
    private Coroutine transitionCoroutine;

    [Header("Input Gate")]
    public float reopenCooldown = 0.20f;
    private float reopenBlockUntil = 0f;

    public void EnterInteraction()
    {
        if (Time.time < reopenBlockUntil) return;
        if (isInteracting) return;

        if (!playerCamera || !cameraTargetPosition || !playerController || !bulletinController)
        {
            Debug.LogError("[BulletinInteraction] Riferimenti mancanti.");
            return;
        }

        playerController.SetControlsEnabled(false);
        if (HudManager) HudManager.SetInteracting(true);

        originalCamParent = playerCamera.transform.parent;
        originalWorldPos = playerCamera.transform.position;
        originalWorldRot = playerCamera.transform.rotation;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(
            targetPos: cameraTargetPosition.position,
            targetRot: cameraTargetPosition.rotation,
            onComplete: () =>
            {
                playerCamera.transform.SetParent(cameraTargetPosition, false);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;

                bulletinController.EnterInteraction(this);
                bulletinController.RefreshNow();

                isInteracting = true;
            }));
    }

    public void ExitInteraction()
    {
        if (!isInteracting) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        playerCamera.transform.SetParent(originalCamParent, true);

        transitionCoroutine = StartCoroutine(TransitionCamera(
            targetPos: originalWorldPos,
            targetRot: originalWorldRot,
            onComplete: () =>
            {
                playerCamera.transform.SetParent(originalCamParent, false);
                playerCamera.transform.position = originalWorldPos;
                playerCamera.transform.rotation = originalWorldRot;

                playerController.SetControlsEnabled(true);
                if (HudManager) HudManager.SetInteracting(false);

                isInteracting = false;
                reopenBlockUntil = Time.time + reopenCooldown;
            }));
    }

    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
    {
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            float easedT = transitionCurve.Evaluate(t); // 👈 EaseInOut invece di lineare

            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);
            yield return null;
        }

        onComplete?.Invoke();
    }
}
