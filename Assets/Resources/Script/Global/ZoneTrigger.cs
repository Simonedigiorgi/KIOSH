using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Bedroom, Kitchen }
    public ZoneType zoneType;

    [Header("Trigger Behavior")]
    public bool fireOnce = true;

    // privato ma visibile in Inspector
    [SerializeField] private bool resetOnWorkdayStart = true;

    // la porta viene risolta automaticamente via tag "RoomDoor"
    private RoomDoor linkedDoor;
    private bool hasFired = false;

    private void Awake()
    {
        // Serve alla Kitchen; lo lasciamo risolto qui.
        TryFindDoorByTag();
    }

    private void OnEnable()
    {
        if (resetOnWorkdayStart)
            TimerManager.OnTimerStartedGlobal += ResetOneShot;
    }

    private void OnDisable()
    {
        if (resetOnWorkdayStart)
            TimerManager.OnTimerStartedGlobal -= ResetOneShot;
    }

    private void ResetOneShot()
    {
        hasFired = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var tm = TimerManager.Instance;

        if (zoneType == ZoneType.Bedroom)
        {
            // ✅ Delega a TimerManager: chiusura porta + chiusura reentry (se attivo)
            tm?.SetPlayerInsideRoom(true);
            return;
        }

        if (zoneType == ZoneType.Kitchen)
        {
            if (fireOnce && hasFired) return;

            if (linkedDoor == null) TryFindDoorByTag();

            if (linkedDoor != null)
            {
                linkedDoor.CloseDoor();
                hasFired = true;
            }
            else
            {
                Debug.LogWarning("[ZoneTrigger] Nessuna RoomDoor trovata con tag 'RoomDoor'.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (zoneType == ZoneType.Bedroom)
        {
            TimerManager.Instance?.SetPlayerInsideRoom(false);
        }
    }

    private void TryFindDoorByTag()
    {
        var go = GameObject.FindWithTag("RoomDoor");
        linkedDoor = go ? go.GetComponent<RoomDoor>() : null;
    }
}
