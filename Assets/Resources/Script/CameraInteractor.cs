using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CameraInteractor : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float defaultTransitionTime = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera playerCamera;
    private PlayerController playerController;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private float originalFOV;
    private Transform originalParent;

    private Coroutine transitionCoroutine;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCamera = Camera.main;
    }

    public void EnterInteraction(Transform target, float transitionTime = -1f, Action onComplete = null, float? fov = null)
    {
        if (!playerCamera || !playerController) return;

        playerController.SetControlsEnabled(false);
        SaveOriginal();

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionTo(
            target.position,
            target.rotation,
            transitionTime < 0 ? defaultTransitionTime : transitionTime,
            onComplete,
            fov
        ));
    }

    public void ExitInteraction(float transitionTime = -1f, Action onComplete = null)
    {
        if (!playerCamera || !playerController) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionTo(
            originalPos,
            originalRot,
            transitionTime < 0 ? defaultTransitionTime : transitionTime,
            () =>
            {
                playerCamera.transform.SetParent(originalParent, false);
                playerController.SetControlsEnabled(true);
                onComplete?.Invoke();
            },
            originalFOV
        ));
    }

    private void SaveOriginal()
    {
        originalParent = playerCamera.transform.parent;
        originalPos = playerCamera.transform.position;
        originalRot = playerCamera.transform.rotation;
        originalFOV = playerCamera.fieldOfView;
    }

    private IEnumerator TransitionTo(Vector3 targetPos, Quaternion targetRot, float duration, Action onComplete, float? targetFOV = null)
    {
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;
        float startFOV = playerCamera.fieldOfView;
        float endFOV = targetFOV ?? startFOV;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float easedT = transitionCurve.Evaluate(t);

            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, easedT);

            yield return null;
        }

        onComplete?.Invoke();
    }
}
