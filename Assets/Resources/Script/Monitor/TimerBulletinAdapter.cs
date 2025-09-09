using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimerBulletinAdapter : BulletinAdapterBase
{
    [Header("UI")]
    public string menuTitle = "Giornata di lavoro";

    private BulletinController controller;

    void Awake() => controller = GetComponentInParent<BulletinController>();

    void OnEnable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += RefreshPanel;
        TimerManager.OnTimerStartedGlobal += RefreshPanel;
        TimerManager.OnTimerCompletedGlobal += RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= RefreshPanel;
        TimerManager.OnTimerStartedGlobal -= RefreshPanel;
        TimerManager.OnTimerCompletedGlobal -= RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(int day, DayPhase phase) => RefreshPanel();
    private void RefreshPanel() => controller?.RefreshNow();

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions) : new List<BulletinController.MenuOption>();

        var tm = TimerManager.Instance;
        var gs = GameStateManager.Instance;

        // mostra SOLO al mattino
        if (gs == null || gs.CurrentPhase != DayPhase.Morning) return list;
        if (tm != null && tm.IsRunning) return list;

        // no doppioni
        if (list.Exists(o => o != null && o.title == menuTitle)) return list;

        var submenu = new BulletinController.MenuOption
        {
            title = menuTitle,
            action = BulletinController.MenuOption.MenuAction.OpenSubmenu,
            subOptions = new List<BulletinController.MenuOption>()
        };

        submenu.subOptions.Add(new BulletinController.MenuOption
        {
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = () => "Timer inattivo"
        });

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
