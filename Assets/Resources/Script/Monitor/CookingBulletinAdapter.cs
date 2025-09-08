using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CookingBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public CookingStation station;

    private BulletinController controller;

    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();
    }

    void OnEnable()
    {
        CookingStation.OnStationStateChanged += RefreshPanel;
    }

    void OnDisable()
    {
        CookingStation.OnStationStateChanged -= RefreshPanel;
    }

    private void RefreshPanel() => controller?.RefreshNow();

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions)
                                         : new List<BulletinController.MenuOption>();

        if (!station)
        {
            list.Add(new BulletinController.MenuOption
            {
                title = "⚠️ Nessuna stazione collegata",
                action = BulletinController.MenuOption.MenuAction.Label
            });
            return list;
        }

        // Stato live
        list.Add(new BulletinController.MenuOption
        {
            title = "",
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = station.GetProgressText
        });

        // Inserisci cibo
        if (station.CurrentState == CookingStation.State.Empty)
        {
            var opt = new BulletinController.MenuOption
            {
                title = "Inserisci cibo",
                action = BulletinController.MenuOption.MenuAction.Invoke,
                onInvoke = new UnityEvent()
            };
            opt.onInvoke.AddListener(station.InsertFood);
            list.Add(opt);
        }

        // Cucina cibo
        if (station.CurrentState == CookingStation.State.Filled)
        {
            var opt = new BulletinController.MenuOption
            {
                title = "Cucina cibo",
                action = BulletinController.MenuOption.MenuAction.Invoke,
                onInvoke = new UnityEvent()
            };
            opt.onInvoke.AddListener(station.StartCooking);
            list.Add(opt);
        }

        return list;
    }
}
