using System.Collections;
using UnityEngine;

public class BulletinInteraction : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTargetPosition;
    public Camera playerCamera;
    public CrosshairManager crosshairManager;
    public BulletinController bulletinController;
    public PlayerController playerController;

    [Header("Camera Transition")]
    public float transitionTime = 0.5f;

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

        // disabilita controlli
        playerController.SetControlsEnabled(false);
        if (crosshairManager) crosshairManager.SetInteracting(true);

        // salva stato world-space
        originalCamParent = playerCamera.transform.parent;
        originalWorldPos = playerCamera.transform.position;
        originalWorldRot = playerCamera.transform.rotation;

        // interrompi coroutine vecchia
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(
            targetPos: cameraTargetPosition.position,
            targetRot: cameraTargetPosition.rotation,
            onComplete: () =>
            {
                // snap finale & reparent
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

        // interrompi coroutine vecchia
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        // reparent provvisorio al parent originale
        playerCamera.transform.SetParent(originalCamParent, true);

        transitionCoroutine = StartCoroutine(TransitionCamera(
            targetPos: originalWorldPos,
            targetRot: originalWorldRot,
            onComplete: () =>
            {
                // snap finale
                playerCamera.transform.SetParent(originalCamParent, false);
                playerCamera.transform.position = originalWorldPos;
                playerCamera.transform.rotation = originalWorldRot;

                playerController.SetControlsEnabled(true);
                if (crosshairManager) crosshairManager.SetInteracting(false);

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
            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        onComplete?.Invoke();
    }
}
