using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public DeliveryBox deliveryBox;

    [Header("Testi")]
    [TextArea] public string msgDoorOpen = "Sportello aperto — chiudere per spedire.";
    [TextArea] public string msgInsertDish = "Inserire un piatto completo.";
    [TextArea] public string msgIncompleteDish = "Il piatto inserito non è completo: aggiungi gli ingredienti mancanti.";

    [Header("Progress")]
    public string progressFormat = "Piatti spediti: {0}/{1}";

    // Evento statico globale (resta qui, niente GameEvents)
    public static event Action OnAllDeliveriesCompleted;

    // Raiser pubblico (l’unico punto che invoca l’evento)
    public static void RaiseAllDeliveriesCompleted()
    {
        OnAllDeliveriesCompleted?.Invoke();
    }

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions)
                                         : new List<BulletinController.MenuOption>();

        if (!deliveryBox)
        {
            list.Add(new BulletinController.MenuOption { title = "Piatti spediti: 0/?", action = BulletinController.MenuOption.MenuAction.Label });
            list.Add(new BulletinController.MenuOption
            {
                title = "Spedisci",
                action = BulletinController.MenuOption.MenuAction.ShowReading,
                readingPages = new List<string> { "Nessuna DeliveryBox collegata." }
            });
            return list;
        }

        // Progresso
        string progress = string.Format(
            string.IsNullOrEmpty(progressFormat) ? "Piatti spediti: {0}/{1}" : progressFormat,
            DeliveryBox.TotalDelivered, deliveryBox.deliveryGoal
        );

        list.Add(new BulletinController.MenuOption { title = progress, action = BulletinController.MenuOption.MenuAction.Label });

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
                if (deliveryBox.CurrentDish != null && !deliveryBox.CurrentDish.IsComplete)
                {
                    list.Add(new BulletinController.MenuOption
                    {
                        title = "Spedizione cibo / Automatica",
                        action = BulletinController.MenuOption.MenuAction.ShowReading,
                        readingPages = new List<string> { msgIncompleteDish }
                    });
                }
                else
                {
                    var opt = new BulletinController.MenuOption
                    {
                        title = "Spedizione cibo / Automatica",
                        action = BulletinController.MenuOption.MenuAction.Invoke,
                        onInvoke = new UnityEngine.Events.UnityEvent()
                    };
                    opt.onInvoke.AddListener(deliveryBox.OnDeliveryButtonClick);
                    list.Add(opt);
                }
            }
            else
            {
                list.Add(new BulletinController.MenuOption
                {
                    title = "Spedizione cibo / Automatica",
                    action = BulletinController.MenuOption.MenuAction.ShowReading,
                    readingPages = new List<string> { msgInsertDish }
                });
            }
        }

        return list;
    }
}
