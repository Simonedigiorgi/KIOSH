using System.Collections;
using UnityEngine;

public class DeliveryBox : MonoBehaviour
{
    [Header("Piatto")]
    public Transform platePosition;
    private Dish currentDish;

    [Header("Sportello")]
    public Transform door;             // assegna il Transform dello sportello
    public float doorOpenZ = -80f;     // rotazione locale Z quando aperto
    public float doorClosedZ = 0f;     // rotazione locale Z quando chiuso
    public float doorAnimTime = 0.5f;  // durata apertura/chiusura
    private bool isDoorOpen = false;
    private bool isDoorAnimating = false;
    public bool IsDoorOpen => isDoorOpen;

    public static int TotalDelivered = 0;   // contatore globale
    public int deliveryGoal = 10;           // obiettivo

    [Header("UI (opzionale)")]
    [SerializeField] private BulletinController bulletinController;
    [SerializeField] private DeliveryBulletinAdapter bulletinAdapter;       // se ancora usato

    void Awake()
    {
        if (!bulletinController && bulletinAdapter)
            bulletinController = bulletinAdapter.bulletin;

        if (!bulletinController)
            bulletinController = GetComponent<BulletinController>()
                               ?? GetComponentInParent<BulletinController>()
                               ?? GetComponentInChildren<BulletinController>(true);
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
            float a = Mathf.Lerp(startZ, targetZ, Mathf.Clamp01(t / doorAnimTime));
            Vector3 e = door.localEulerAngles;
            door.localEulerAngles = new Vector3(e.x, e.y, a);
            yield return null;
        }

        Vector3 end = door.localEulerAngles;
        door.localEulerAngles = new Vector3(end.x, end.y, targetZ);

        isDoorOpen = open;
        isDoorAnimating = false;

        NotifyUI();
    }

    private float NormalizeAngle(float z)
    {
        if (z > 180f) z -= 360f;
        return z;
    }

    // ========== INSERIMENTO PIATTO ==========
    public void TryInsertDish(PickupObject pickup)
    {
        if (currentDish != null || pickup == null) return;

        Dish dish = pickup.GetComponent<Dish>();
        if (dish == null || !dish.IsComplete) return;

        pickup.transform.SetPositionAndRotation(platePosition.position, platePosition.rotation);
        pickup.transform.SetParent(platePosition);

        var rb = pickup.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;
#if UNITY_6000_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.linearVelocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        pickup.canBePickedUp = false;
        pickup.isHeld = false;

        currentDish = dish;
        Debug.Log("📦 Piatto inserito nel delivery box.");

        NotifyUI();
    }

    // ========== SPEDIZIONE ==========
    public void OnDeliveryButtonClick()
    {
        if (isDoorOpen) { Debug.Log("⛔ Sportello aperto: chiudere per spedire."); return; }
        if (currentDish == null || !currentDish.IsComplete) return;

        Debug.Log("🚀 Piatto spedito!");
        Destroy(currentDish.gameObject);
        currentDish = null;

        TotalDelivered++;

        NotifyUI();
    }

    public bool IsOccupied => currentDish != null;

    // ========== UI helper ==========
    private void NotifyUI()
    {
        var controller = bulletinController;

        if (!controller && bulletinAdapter)
            controller = bulletinAdapter.bulletin
                      ?? bulletinAdapter.GetComponent<BulletinController>()
                      ?? bulletinAdapter.GetComponentInParent<BulletinController>()
                      ?? bulletinAdapter.GetComponentInChildren<BulletinController>(true);

        if (!controller)
            controller = GetComponent<BulletinController>()
                      ?? GetComponentInParent<BulletinController>()
                      ?? GetComponentInChildren<BulletinController>(true);

        if (controller)
            controller.RefreshNow();
    }
}
