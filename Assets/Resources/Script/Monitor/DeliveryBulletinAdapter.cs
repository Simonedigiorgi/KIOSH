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
        // se non è sullo stesso GO, prova parent/children
        if (!bulletin) bulletin = GetComponent<BulletinController>();
        if (!bulletin) bulletin = GetComponentInParent<BulletinController>();
        if (!bulletin) bulletin = GetComponentInChildren<BulletinController>(true);
    }

    /// <summary>
    /// Costruisce e RITORNA le opzioni per il controller.
    /// Il controller chiamerà questo metodo quando deve refreshare.
    /// </summary>
    public List<BulletinController.MenuOption> BuildOptions()
    {
        var list = new List<BulletinController.MenuOption>();

        if (!deliveryBox)
        {
            // placeholder utile per capire che la UI funziona
            list.Add(new BulletinController.MenuOption { title = "Piatti spediti: 0/?", action = BulletinController.MenuOption.MenuAction.Label });
            list.Add(new BulletinController.MenuOption
            {
                title = "Spedisci",
                action = BulletinController.MenuOption.MenuAction.ShowReading,
                readingPages = new List<string> { "Nessuna DeliveryBox collegata." }
            });
            return list;
        }

        string progress = string.Format(
            string.IsNullOrEmpty(progressFormat) ? "Piatti spediti: {0}/{1}" : progressFormat,
            DeliveryBox.TotalDelivered,
            deliveryBox.deliveryGoal
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
                var opt = new BulletinController.MenuOption
                {
                    title = "Spedizione cibo / Automatica",
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
                    title = "Spedizione cibo / Automatica",
                    action = BulletinController.MenuOption.MenuAction.ShowReading,
                    readingPages = new List<string> { msgInsertDish }
                });
            }
        }

        return list;
    }

    // Chiamalo quando cambiano dati/stato (porta aperta, piatto inserito, consegna, ecc.)
    public void NotifyChanged()
    {
        if (bulletin) bulletin.RefreshNow();
    }
}
