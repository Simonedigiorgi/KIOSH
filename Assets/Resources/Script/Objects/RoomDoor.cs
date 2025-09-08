using System.Collections;
using UnityEngine;

public class RoomDoor : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public Transform peephole;
    public Transform peepholeCameraTarget;
    public CameraInteractor cameraInteractor;

    [Header("Movement")]
    public float slideDistance = 1.2f;
    public float moveSpeed = 2f;

    [Header("Camera FX")]
    public float peepholeFOV = 35f;

    [Header("Audio")]
    public AudioClip sfxOpen;
    public AudioClip sfxClose;
    private AudioSource audioSource;

    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;
    private Vector3 peepholeClosedPos;
    private Vector3 peepholeOpenPos;

    private bool isDoorOpen = false;
    private bool isLookingThroughPeephole = false;

    public bool IsLookingThroughPeephole => isLookingThroughPeephole;

    void Start()
    {
        doorClosedPos = transform.localPosition;
        doorOpenPos = doorClosedPos + Vector3.right * slideDistance;

        if (peephole != null)
        {
            peepholeClosedPos = peephole.localPosition;
            peepholeOpenPos = peepholeClosedPos + Vector3.right * 0.25f;
        }

        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // ---------- IInteractable ----------
    public void Interact(PlayerInteractor interactor)
    {
        // Se sto già guardando nello spioncino → E serve a uscire
        if (isLookingThroughPeephole)
        {
            ExitPeephole();
            return;
        }

        // Se colpisci lo spioncino (prima di entrarci)
        if (peephole != null && interactor.currentTarget == peephole.gameObject)
        {
            InteractWithPeephole();
        }
        else
        {
            // Interazione con la porta
            if (isDoorOpen) CloseDoor();
            else OpenDoor();
        }
    }

    // ---------- Porta ----------
    public void OpenDoor()
    {
        if (isDoorOpen) return;

        isDoorOpen = true;
        StopAllCoroutines();
        StartCoroutine(SlideDoor(true));
        if (sfxOpen) audioSource.PlayOneShot(sfxOpen);
    }

    public void CloseDoor()
    {
        if (!isDoorOpen) return;

        isDoorOpen = false;
        StopAllCoroutines();
        StartCoroutine(SlideDoor(false));
        if (sfxClose) audioSource.PlayOneShot(sfxClose);
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

    // ---------- Spioncino ----------
    public void InteractWithPeephole()
    {
        if (isLookingThroughPeephole) ExitPeephole();
        else EnterPeephole();
    }

    private void EnterPeephole()
    {
        if (!peepholeCameraTarget || !cameraInteractor) return;

        StopAllCoroutines();
        StartCoroutine(SlidePeephole(true));

        cameraInteractor.EnterInteraction(
            peepholeCameraTarget,
            fov: peepholeFOV,
            onComplete: () => isLookingThroughPeephole = true
        );
    }

    private void ExitPeephole()
    {
        if (!cameraInteractor) return;

        StopAllCoroutines();
        StartCoroutine(SlidePeephole(false));

        cameraInteractor.ExitInteraction(
            onComplete: () => isLookingThroughPeephole = false
        );
    }

    private IEnumerator SlidePeephole(bool open)
    {
        if (peephole == null) yield break;

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
}
