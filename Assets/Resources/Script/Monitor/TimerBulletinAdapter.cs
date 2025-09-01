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

        // iscrizione agli eventi globali
        TimerManager.OnTimerStartedGlobal += HandleTimerStarted;
        TimerManager.OnTimerCompletedGlobal += HandleTimerCompleted;
    }

    void OnDestroy()
    {
        TimerManager.OnTimerStartedGlobal -= HandleTimerStarted;
        TimerManager.OnTimerCompletedGlobal -= HandleTimerCompleted;
    }

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = baseOptions != null ? new List<BulletinController.MenuOption>(baseOptions)
                                       : new List<BulletinController.MenuOption>();

        // Se il timer è in esecuzione → mostra solo la riga live in alto
        if (TimerManager.Instance != null && TimerManager.Instance.IsRunning)
        {
            list.Insert(0, new BulletinController.MenuOption
            {
                action = BulletinController.MenuOption.MenuAction.LiveLabel,
                dynamicTextProvider = () => "Timer: " + TimerManager.FormatTime(TimerManager.Instance.RemainingSeconds)
            });

            return list; // niente submenu
        }

        // Evita doppioni se già presente
        if (list.Exists(o => o != null && o.title == menuTitle))
            return list;

        // Submenu per avvio timer
        var submenu = new BulletinController.MenuOption
        {
            title = menuTitle,
            action = BulletinController.MenuOption.MenuAction.OpenSubmenu,
            subOptions = new List<BulletinController.MenuOption>()
        };

        // Riga stato
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

            // Torna subito al menu principale
            if (controller)
                controller.RefreshNow();
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

    private void RefreshAllBoards()
    {
        var controllers = FindObjectsOfType<BulletinController>();
        foreach (var c in controllers)
        {
            c.RefreshNow();
        }
    }
}
