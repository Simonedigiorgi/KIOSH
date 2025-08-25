using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryBulletinAdapter : MonoBehaviour
{
    [Header("Refs")]
    public DeliveryBox deliveryBox;
    public BulletinController bulletin;

    [Header("Testi")]
    [TextArea] public string msgDoorOpen = "Sportello aperto — chiudere per spedire.";
    [TextArea] public string msgInsertDish = "Inserire un piatto completo.";

    void Awake()
    {
        if (bulletin == null) bulletin = GetComponent<BulletinController>();
    }

    public void RefreshMenu()
    {
        if (bulletin == null || deliveryBox == null) return;

        var list = new List<BulletinController.MenuOption>();

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

        bulletin.mainOptions = list; // ✅ solo questo
    }
}
