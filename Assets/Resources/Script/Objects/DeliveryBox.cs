using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System; // <— serve per Action<int>

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class DeliveryBox : MonoBehaviour, IInteractable
{
    // ---------- Stato piatto ----------
    private Dish currentDish;
    private PickupObject currentPickup; // cache per evitare GetComponent ripetuti
    public Dish CurrentDish => currentDish;
    public bool IsOccupied => currentDish != null;

    // ---------- Sportello ----------
    [Header("Sportello")]
    [Required, SerializeField] private Transform door;
    [SerializeField] private float doorOpenZ = -80f;
    [SerializeField] private float doorClosedZ = 0f;
    [Min(0.01f), SerializeField] private float doorAnimTime = 0.5f;

    private bool isDoorOpen = false;
    private bool isDoorAnimating = false;
    public bool IsDoorOpen => isDoorOpen;

    // ---------- Progresso ----------
    [Header("Progress")]
    public static int TotalDelivered = 0;
    [Min(1)] public int deliveryGoal = 10;

    // ---------- UI ----------
    [Header("UI (opzionale)")]
    [Required, SerializeField] private BulletinController bulletinController;

    // ---------- Audio ----------
    [Header("Audio")]
    [SerializeField] private AudioClip doorToggleClip; // ⏯ unica clip per apri/chiudi sportello
    [SerializeField] private AudioClip deliveryClip;   // 🔊 suono alla consegna
    private AudioSource audioSource;                   // preso via GetComponent

    // ---------- Events ----------
    [Header("Events")]
    public UnityEvent onAllDeliveriesCompleted; // editor-friendly (RoomDoor.OpenDoor, ecc.)

    // 🔔 Nuovo: notifica ogni volta che cambia il totale spedito
    public static event Action<int> OnDeliveredCountChanged;

    // ---------- Runtime ----------
    private Coroutine doorRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetStaticsOnSceneLoad()
    {
        TotalDelivered = 0;
        OnDeliveredCountChanged?.Invoke(TotalDelivered); // reset listener al cambio scena
    }

    void Awake()
    {
        // Niente auto-find per la UI: ci fidiamo delle ref serializzate
        // AudioSource sempre sullo stesso GO
        audioSource = GetComponent<AudioSource>();
    }

    void OnValidate()
    {
        if (doorAnimTime < 0.01f) doorAnimTime = 0.01f;
        if (deliveryGoal < 1) deliveryGoal = 1;
    }

    // ---------- IInteractable ----------
    public void Interact(PlayerInteractor interactor) => ToggleDoor();

    // ========== SPORTELLO ==========
    public void ToggleDoor()
    {
        if (!door || isDoorAnimating) return;
        StartDoorAnimation(!isDoorOpen);
    }

    private void StartDoorAnimation(bool open)
    {
        if (doorRoutine != null) StopCoroutine(doorRoutine);

        // SFX unico per apri/chiudi
        if (doorToggleClip && audioSource) audioSource.PlayOneShot(doorToggleClip);

        doorRoutine = StartCoroutine(AnimateDoor(open));
    }

    private IEnumerator AnimateDoor(bool open)
    {
        isDoorAnimating = true;

        float startZ = NormalizeAngle(door.localEulerAngles.z);
        float targetZ = open ? doorOpenZ : doorClosedZ;

        float t = 0f;
        float invDur = 1f / doorAnimTime;

        while (t < doorAnimTime)
        {
            t += Time.deltaTime;
            float k = t * invDur; if (k > 1f) k = 1f;
            float a = Mathf.Lerp(startZ, targetZ, k);

            var e = door.localEulerAngles;
            door.localEulerAngles = new Vector3(e.x, e.y, a);

            yield return null;
        }

        // snap finale
        var end = door.localEulerAngles;
        door.localEulerAngles = new Vector3(end.x, end.y, targetZ);

        isDoorOpen = open;
        isDoorAnimating = false;
        doorRoutine = null;

        if (currentPickup) currentPickup.canBePickedUp = isDoorOpen;

        RefreshUI();
    }

    private static float NormalizeAngle(float z) => (z > 180f) ? z - 360f : z;

    // ========== GESTIONE PIATTO ==========
    public void RegisterDish(Dish dish)
    {
        currentDish = dish;
        currentPickup = dish ? dish.GetComponent<PickupObject>() : null;

        if (currentPickup) currentPickup.canBePickedUp = isDoorOpen;

        Debug.Log("[DeliveryBox] Piatto inserito nel delivery box.");
        RefreshUI();
    }

    public void OnDishRemoved(Dish dish)
    {
        if (currentDish != dish) return;

        currentDish = null;
        currentPickup = null;

        Debug.Log("[DeliveryBox] Piatto rimosso dalla DeliveryBox.");
        RefreshUI();
    }

    // ========== SPEDIZIONE ==========
    public void OnDeliveryButtonClick()
    {
        if (isDoorOpen)
        {
            Debug.Log("[DeliveryBox] Sportello aperto: chiudere per spedire.");
            return;
        }
        if (!currentDish) return;

        if (!currentDish.IsComplete)
        {
            Debug.Log("[DeliveryBox] Piatto incompleto: non può essere spedito.");
            return;
        }

        Debug.Log("[DeliveryBox] Piatto spedito!");

        if (deliveryClip && audioSource) audioSource.PlayOneShot(deliveryClip);

        Destroy(currentDish.gameObject);
        currentDish = null;
        currentPickup = null;

        TotalDelivered++;

        // 🔔 Notifica globale del nuovo totale
        OnDeliveredCountChanged?.Invoke(TotalDelivered);

        RefreshUI();

        if (TotalDelivered >= deliveryGoal)
        {
            Debug.Log("[DeliveryBox] Tutte le consegne completate!");
            DeliveryBulletinAdapter.RaiseAllDeliveriesCompleted();
            onAllDeliveriesCompleted?.Invoke();
        }
    }

    // ========== UI helper ==========
    private void RefreshUI()
    {
        if (bulletinController) bulletinController.RefreshNow();
    }
}
