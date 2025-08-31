using System.Collections;
using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Header("Debug / Stato")]
    public bool canBeOpened = true; // 👈 controlla davvero se la porta può muoversi

    [Header("Refs")]
    public Transform handle;
    public Transform peephole;
    public Transform peepholeCameraTarget;
    public Camera playerCamera;
    public PlayerController playerController;

    [Header("Movement")]
    public float slideDistance = 1.2f;
    public float moveSpeed = 2f;
    public float transitionTime = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera FX")]
    public float peepholeFOV = 35f;
    public float fovTransitionTime = 0.5f;

    private float originalFOV;
    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;
    private Vector3 peepholeClosedPos;
    private Vector3 peepholeOpenPos;

    private bool isDoorOpen = false;
    private bool isLookingThroughPeephole = false;

    // backup camera
    private Transform originalCamParent;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private Coroutine transitionCoroutine;

    public bool IsLookingThroughPeephole => isLookingThroughPeephole;

    void Start()
    {
        doorClosedPos = transform.localPosition;
        doorOpenPos = doorClosedPos + Vector3.right * slideDistance;

        peepholeClosedPos = peephole.localPosition;
        peepholeOpenPos = peepholeClosedPos + Vector3.right * 0.25f;
    }

    // ---------- HANDLE ----------
    public void InteractWithHandle()
    {
        // 👇 Rispetta la condizione
        if (!canBeOpened)
        {
            Debug.Log("🚪 Porta bloccata, non può essere aperta.");
            return;
        }

        isDoorOpen = !isDoorOpen;
        StopAllCoroutines();
        StartCoroutine(SlideDoor(isDoorOpen));
    }

    private IEnumerator SlideDoor(bool open)
    {
        Vector3 start = transform.localPosition;
        Vector3 target = open ? doorOpenPos : doorClosedPos;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    // ---------- PEEPHOLE ----------
    public void InteractWithPeephole()
    {
        if (isLookingThroughPeephole) ExitPeephole();
        else EnterPeephole();
    }

    private void EnterPeephole()
    {
        if (!playerCamera || !peepholeCameraTarget || !playerController) return;

        playerController.SetControlsEnabled(false);

        StopAllCoroutines();
        StartCoroutine(SlidePeephole(true));

        originalCamParent = playerCamera.transform.parent;
        originalCamPos = playerCamera.transform.position;
        originalCamRot = playerCamera.transform.rotation;
        originalFOV = playerCamera.fieldOfView;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(
            peepholeCameraTarget.position,
            peepholeCameraTarget.rotation,
            () => isLookingThroughPeephole = true
        ));

        StartCoroutine(TransitionFOV(originalFOV, peepholeFOV));
    }

    private void ExitPeephole()
    {
        if (!playerCamera || !playerController) return;

        StopAllCoroutines();
        StartCoroutine(SlidePeephole(false));

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(
            originalCamPos,
            originalCamRot,
            () =>
            {
                playerCamera.transform.SetParent(originalCamParent, false);
                playerController.SetControlsEnabled(true);
                isLookingThroughPeephole = false;
            }
        ));

        StartCoroutine(TransitionFOV(playerCamera.fieldOfView, originalFOV));
    }

    private IEnumerator SlidePeephole(bool open)
    {
        Vector3 start = peephole.localPosition;
        Vector3 target = open ? peepholeOpenPos : peepholeClosedPos;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            peephole.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
    {
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            float easedT = transitionCurve.Evaluate(t);

            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);

            yield return null;
        }

        onComplete?.Invoke();
    }

    private IEnumerator TransitionFOV(float start, float target)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fovTransitionTime;
            float easedT = transitionCurve.Evaluate(t);
            playerCamera.fieldOfView = Mathf.Lerp(start, target, easedT);
            yield return null;
        }
        playerCamera.fieldOfView = target;
    }
}
