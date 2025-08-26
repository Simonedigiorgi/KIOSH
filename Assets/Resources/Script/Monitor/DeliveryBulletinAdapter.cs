using System.Collections.Generic;
using UnityEngine;

public class DeliveryBulletinAdapter : MonoBehaviour
{
    [Header("Refs")]
    public DeliveryBox deliveryBox;
    public BulletinController bulletin;

    [Header("Testi")]
    [TextArea] public string msgDoorOpen = "Sportello aperto — chiudere per spedire.";
    [TextArea] public string msgInsertDish = "Inserire un piatto completo.";

    [Header("Progress")]
    public string progressFormat = "Piatti spediti: {0}/{1}";

    void Awake()
    {
        if (!bulletin) bulletin = GetComponent<BulletinController>();
    }

    public void RefreshMenu()
    {
        if (!bulletin || !deliveryBox) return;

        var list = new List<BulletinController.MenuOption>();

        // Etichetta progresso (NON selezionabile)
        string progress = string.Format(
            string.IsNullOrEmpty(progressFormat) ? "Piatti spediti: {0}/{1}" : progressFormat,
            DeliveryBox.TotalDelivered,
            deliveryBox.deliveryGoal
        );
        list.Add(new BulletinController.MenuOption
        {
            title = progress,
            action = BulletinController.MenuOption.MenuAction.Label
        });

        if (deliveryBox.IsDoorOpen)
        {
            list.Add(new BulletinController.MenuOption
            {
                title = "Sportello aperto",
                action = BulletinController.MenuOption.MenuAction.ShowReading,
                readingPages = new List<string> { msgDoorOpen }
            });
        }
        else
        {
            if (deliveryBox.IsOccupied)
            {
                var opt = new BulletinController.MenuOption
                {
                    title = "Spedisci",
                    action = BulletinController.MenuOption.MenuAction.Invoke,
                    onInvoke = new UnityEngine.Events.UnityEvent()
                };
                opt.onInvoke.AddListener(deliveryBox.OnDeliveryButtonClick);
                list.Add(opt);
            }
            else
            {
                list.Add(new BulletinController.MenuOption
                {
                    title = "Spedisci",
                    action = BulletinController.MenuOption.MenuAction.ShowReading,
                    readingPages = new List<string> { msgInsertDish }
                });
            }
        }

        // Passa il root al controller. Se è già aperto, refresh immediato.
        bulletin.SetRootOptions(list, refreshIfOpen: true);
    }
}
