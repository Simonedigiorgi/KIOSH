using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimerBulletinAdapter : BulletinAdapterBase
{
    [Header("UI")]
    public string menuTitle = "Giornata di lavoro";

    private BulletinController controller;

    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();
    }

    void OnEnable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += RefreshPanel;
        TimerManager.OnReentryStartedGlobal += RefreshPanel;
        TimerManager.OnReentryCompletedGlobal += RefreshPanel;
        TimerManager.OnTimerStartedGlobal += RefreshPanel;
        TimerManager.OnTimerCompletedGlobal += RefreshPanel;
    }

    void OnDisable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= RefreshPanel;
        TimerManager.OnReentryStartedGlobal -= RefreshPanel;
        TimerManager.OnReentryCompletedGlobal -= RefreshPanel;
        TimerManager.OnTimerStartedGlobal -= RefreshPanel;
        TimerManager.OnTimerCompletedGlobal -= RefreshPanel;
    }

    private void RefreshPanel() => controller?.RefreshNow();

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions)
                                         : new List<BulletinController.MenuOption>();

        var tm = TimerManager.Instance;

        // Se timer principale o reentry sono attivi, NON aggiungiamo nulla a questo pannello.
        if (tm != null && (tm.IsRunning || tm.IsReentryActive || tm.IsFrozenAwaitingReentry))
            return list;

        // Evita doppione
        if (list.Exists(o => o != null && o.title == menuTitle))
            return list;

        var submenu = new BulletinController.MenuOption
        {
            title = menuTitle,
            action = BulletinController.MenuOption.MenuAction.OpenSubmenu,
            subOptions = new List<BulletinController.MenuOption>()
        };

        // Stato
        submenu.subOptions.Add(new BulletinController.MenuOption
        {
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = () => "Timer inattivo"
        });

        // Pulsante avvio
        var start = new BulletinController.MenuOption
        {
            title = "Avvia la giornata di lavoro",
            action = BulletinController.MenuOption.MenuAction.Invoke,
            onInvoke = new UnityEvent()
        };
        start.onInvoke.AddListener(() =>
        {
            TimerManager.Instance?.StartTimer();
            controller?.RefreshNow();
        });
        submenu.subOptions.Add(start);

        list.Add(submenu);
        return list;
    }
}
