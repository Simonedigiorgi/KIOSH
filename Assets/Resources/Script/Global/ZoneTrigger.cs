using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Bedroom, Kitchen }
    public ZoneType zoneType;

    [Header("Optional")]
    public RoomDoor linkedDoor; // usato per chiudere/aprire porta automaticamente

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (zoneType == ZoneType.Bedroom)
        {
            TimerManager.Instance?.SetPlayerInsideRoom(true);
        }
        else if (zoneType == ZoneType.Kitchen && linkedDoor != null)
        {
            linkedDoor.CloseDoor();
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
}
