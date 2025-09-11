using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Bedroom, Kitchen }
    public ZoneType zoneType;

    [Header("Trigger Behavior")]
    public bool fireOnce = true;
    private int playerInsideCount = 0;

    [SerializeField] private bool resetOnWorkdayStart = true;

    [Header("Bedroom")]
    [Tooltip("Se true, chiude la RoomDoor entrando in Bedroom quando le consegne sono completate.")]
    [SerializeField] private bool closeDoorOnBedroomEnterIfDeliveriesDone = true;

    [Header("Refs (opzionale)")]
    [Tooltip("Se valorizzato, bypassa la ricerca via tag.")]
    [SerializeField] private RoomDoor linkedDoor;

    private bool hasFired = false;

    private void Awake()
    {
        if (!linkedDoor) TryFindDoorByTag();
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

    private void ResetOneShot() => hasFired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Primo collider che entra → evento
        if (playerInsideCount++ == 0)
        {
            var tm = TimerManager.Instance;

            if (zoneType == ZoneType.Bedroom)
            {
                // 1) stato dominio
                tm?.SetPlayerInsideRoom(true);

                // 2) chiusura porta locale (post-disaccoppiamento)
                if (closeDoorOnBedroomEnterIfDeliveriesDone && tm != null && tm.DeliveriesCompleted)
                {
                    if (!linkedDoor) TryFindDoorByTag();
                    if (linkedDoor) linkedDoor.CloseDoor();
                    else Debug.LogWarning("[ZoneTrigger] RoomDoor non trovata (tag mancante o GO inattivo).");
                }
                return;
            }

            if (zoneType == ZoneType.Kitchen)
            {
                if (fireOnce && hasFired) return;
                if (!linkedDoor) TryFindDoorByTag();
                if (linkedDoor) { linkedDoor.CloseDoor(); hasFired = true; }
                else Debug.LogWarning("[ZoneTrigger] Nessuna RoomDoor trovata con tag 'RoomDoor'.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Ultimo collider che esce → evento
        if (--playerInsideCount <= 0)
        {
            playerInsideCount = 0;
            if (zoneType == ZoneType.Bedroom)
                TimerManager.Instance?.SetPlayerInsideRoom(false);
        }
    }

    private void TryFindDoorByTag()
    {
        // 1) via tag (richiesta originale)
        var go = GameObject.FindWithTag("RoomDoor"); // NON trova oggetti disattivati
        if (go) { linkedDoor = go.GetComponent<RoomDoor>(); if (linkedDoor) return; }

        // 2) fallback: cerca anche tra oggetti inattivi (Unity 6 API)
        linkedDoor = FindFirstObjectByType<RoomDoor>(FindObjectsInactive.Include);
    }
}
