using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimerBulletinAdapter : BulletinAdapterBase
{
    [Header("UI")]
    public string menuTitle = "Giornata di lavoro";

    [Header("Eventi")]
    public UnityEvent onTimerStarted;
    public UnityEvent onTimerCompleted;

    private BulletinController controller;

    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();

        TimerManager.OnTimerStartedGlobal += HandleTimerStarted;
        TimerManager.OnTimerCompletedGlobal += HandleTimerCompleted;
        TimerManager.OnReentryStartedGlobal += HandleTimerChanged;
        TimerManager.OnReentryCompletedGlobal += HandleTimerChanged;
    }

    void OnDestroy()
    {
        TimerManager.OnTimerStartedGlobal -= HandleTimerStarted;
        TimerManager.OnTimerCompletedGlobal -= HandleTimerCompleted;
        TimerManager.OnReentryStartedGlobal -= HandleTimerChanged;
        TimerManager.OnReentryCompletedGlobal -= HandleTimerChanged;
    }

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = baseOptions != null ? new List<BulletinController.MenuOption>(baseOptions)
                                       : new List<BulletinController.MenuOption>();

        var tm = TimerManager.Instance;

        // Se timer o reentry attivi → NON mostrare il submenu (voce nascosta)
        if (tm != null && (tm.IsRunning || tm.IsReentryActive))
            return list;

        // Altrimenti mostra il submenu "Giornata di lavoro" con il bottone Avvia (evita duplicati)
        if (!list.Exists(o => o != null && o.title == menuTitle))
        {
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
            start.onInvoke.AddListener(StartTimerFromThisPanel);
            submenu.subOptions.Add(start);

            list.Add(submenu);
        }

        return list;
    }

    private void StartTimerFromThisPanel()
    {
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StartTimer();
            controller?.RefreshNow();
        }
    }

    private void HandleTimerStarted()
    {
        onTimerStarted?.Invoke();
        RefreshAllBoards();
    }

    private void HandleTimerCompleted()
    {
        onTimerCompleted?.Invoke();
        RefreshAllBoards();
    }

    private void HandleTimerChanged() => RefreshAllBoards();

    private void RefreshAllBoards()
    {
        var controllers = Object.FindObjectsOfType<BulletinController>();
        foreach (var c in controllers) c.RefreshNow();
    }
}
