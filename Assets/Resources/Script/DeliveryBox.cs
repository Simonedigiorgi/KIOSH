using System.Collections;
using UnityEngine;

public class DeliveryBox : MonoBehaviour
{
    private Dish currentDish;
    public Dish CurrentDish => currentDish;

    [Header("Sportello")]
    public Transform door;
    public float doorOpenZ = -80f;
    public float doorClosedZ = 0f;
    public float doorAnimTime = 0.5f;
    private bool isDoorOpen = false;
    private bool isDoorAnimating = false;
    public bool IsDoorOpen => isDoorOpen;

    [Header("Progress")]
    public static int TotalDelivered = 0;
    public int deliveryGoal = 10;

    [Header("UI (opzionale)")]
    [SerializeField] private BulletinController bulletinController;
    [SerializeField] private DeliveryBulletinAdapter bulletinAdapter;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deliveryClip; // 🔊 suono alla consegna

    // Reset automatico dei contatori statici ad ogni load di scena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetStaticsOnSceneLoad()
    {
        TotalDelivered = 0;
    }

    void Awake()
    {
        if (!bulletinController)
            bulletinController = GetComponent<BulletinController>()
                               ?? GetComponentInParent<BulletinController>()
                               ?? GetComponentInChildren<BulletinController>(true);

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    // ========== SPORTELLO ==========
    public void ToggleDoor()
    {
        if (door == null || isDoorAnimating) return;
        StartCoroutine(AnimateDoor(!isDoorOpen));
    }

    public bool HandleDoorClick(Transform clicked)
    {
        if (door == null || clicked == null) return false;
        if (clicked == door || clicked.IsChildOf(door))
        {
            ToggleDoor();
            return true;
        }
        return false;
    }

    private IEnumerator AnimateDoor(bool open)
    {
        isDoorAnimating = true;

        float startZ = NormalizeAngle(door.localEulerAngles.z);
        float targetZ = open ? doorOpenZ : doorClosedZ;

        float t = 0f;
        while (t < doorAnimTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startZ, targetZ, t / doorAnimTime);
            Vector3 e = door.localEulerAngles;
            door.localEulerAngles = new Vector3(e.x, e.y, a);
            yield return null;
        }

        Vector3 end = door.localEulerAngles;
        door.localEulerAngles = new Vector3(end.x, end.y, targetZ);

        isDoorOpen = open;
        isDoorAnimating = false;

        if (currentDish != null)
        {
            var pickup = currentDish.GetComponent<PickupObject>();
            if (pickup != null) pickup.canBePickedUp = isDoorOpen;
        }

        NotifyUI();
    }

    private float NormalizeAngle(float z) => (z > 180f) ? z - 360f : z;

    // ========== GESTIONE PIATTO ==========
    public void RegisterDish(Dish dish)
    {
        currentDish = dish;
        if (dish != null)
        {
            var pickup = dish.GetComponent<PickupObject>();
            if (pickup != null) pickup.canBePickedUp = isDoorOpen;
        }
        Debug.Log("[DeliveryBox] Piatto inserito nel delivery box.");
        NotifyUI();
    }

    public void OnDishRemoved(Dish dish)
    {
        if (currentDish == dish)
        {
            currentDish = null;
            Debug.Log("[DeliveryBox] Piatto rimosso dalla DeliveryBox.");
            NotifyUI();
        }
    }

    // ========== SPEDIZIONE ==========
    public void OnDeliveryButtonClick()
    {
        if (isDoorOpen)
        {
            Debug.Log("[DeliveryBox] Sportello aperto: chiudere per spedire.");
            return;
        }
        if (currentDish == null) return;

        if (!currentDish.IsComplete)
        {
            Debug.Log("[DeliveryBox] Piatto incompleto: non puo essere spedito.");
            return;
        }

        Debug.Log("[DeliveryBox] Piatto spedito!");

        // 🔊 suono consegna
        if (audioSource && deliveryClip)
            audioSource.PlayOneShot(deliveryClip);

        Destroy(currentDish.gameObject);
        currentDish = null;

        TotalDelivered++;
        NotifyUI();

        // Check completamento goal qui, non nella board
        if (TotalDelivered >= deliveryGoal)
        {
            Debug.Log("[DeliveryBox] Tutte le consegne completate!");
            DeliveryBulletinAdapter.RaiseAllDeliveriesCompleted();
        }
    }

    public bool IsOccupied => currentDish != null;

    // ========== UI helper ==========
    private void NotifyUI()
    {
        if (!bulletinController)
            bulletinController = GetComponent<BulletinController>()
                               ?? GetComponentInParent<BulletinController>()
                               ?? GetComponentInChildren<BulletinController>(true);

        if (bulletinController)
            bulletinController.RefreshNow();
    }
}
