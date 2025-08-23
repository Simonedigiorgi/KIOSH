public interface IPlaceableReceiver
{
    bool CanAccept(PickupObject item);  // Questo oggetto accetta l'oggetto passato?
    void Place(PickupObject item);      // Posiziona l'oggetto nel punto corretto
}
