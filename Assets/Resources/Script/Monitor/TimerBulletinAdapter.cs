using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimerBulletinAdapter : BulletinAdapterBase
{
    private string menuTitle = "Avvia la giornata di lavoro";

    [Header("Events")]
    [Tooltip("Invocato quando l’utente seleziona 'Avvia la giornata di lavoro'. Aggancia qui TimerManager.StartTimer, RoomDoor.OpenDoor, FX, ecc.")]
    public UnityEvent onStartRequested;

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
        var list = (baseOptions != null)
            ? new List<BulletinController.MenuOption>(baseOptions)
            : new List<BulletinController.MenuOption>();

        var tm = TimerManager.Instance;
        var gs = GameStateManager.Instance;

        // mostra SOLO al mattino
        if (gs == null || gs.CurrentPhase != DayPhase.Morning) return list;
        if (tm != null && tm.IsRunning) return list;

        // niente duplicati
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

        // Solo evento editor-friendly: niente avvio automatico qui
        start.onInvoke.AddListener(HandleStartPressed);

        submenu.subOptions.Add(start);
        list.Add(submenu);
        return list;
    }

    private void HandleStartPressed()
    {
        // Lancia SOLO l'UnityEvent configurato dall'Inspector
        onStartRequested?.Invoke();

        // refresh UI
        controller?.RefreshNow();
    }
}
