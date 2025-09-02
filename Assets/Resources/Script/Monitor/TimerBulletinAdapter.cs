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

        // Se esiste il manager e la giornata e' stata avviata almeno una volta → UNA SOLA RIGA LIVE
        if (tm != null && tm.HasDayStarted)
        {
            list.Insert(0, new BulletinController.MenuOption
            {
                action = BulletinController.MenuOption.MenuAction.LiveLabel,
                dynamicTextProvider = () =>
                {
                    if (tm.IsReentryActive)
                        return "Rientro: " + TimerManager.FormatTime(tm.ReentryRemainingSeconds);

                    if (tm.IsRunning)
                        return "Timer: " + TimerManager.FormatTime(tm.RemainingSeconds);

                    // Timer fermo (appena scaduto e in attesa di reentry)
                    return "Timer: 00:00:000";
                }
            });

            // niente submenu in questa fase
            return list;
        }

        // Altrimenti (prima di avviare) evita doppioni se gia' presente
        if (list.Exists(o => o != null && o.title == menuTitle))
            return list;

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
            onInvoke = new UnityEngine.Events.UnityEvent()
        };
        start.onInvoke.AddListener(StartTimerFromThisPanel);
        submenu.subOptions.Add(start);

        list.Add(submenu);
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
        Debug.Log("[TimerBulletinAdapter] Timer partito");
        onTimerStarted?.Invoke();
        RefreshAllBoards();
    }

    private void HandleTimerCompleted()
    {
        Debug.Log("[TimerBulletinAdapter] Timer completato");
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
