using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CookingBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public CookingStation station;

    [Header("UI Strings")]
    [TextArea] public string emptyText = "Stato della Cooking Staion: Operativo";
    [TextArea] public string fillingFormat = "Riempimento, Attendere: {0}%";
    [TextArea] public string filledText = "Cibo inserito, pronto a cucinare";
    [TextArea] public string cookingFormat = "Cottura, Attendere: {0}%";
    [TextArea] public string cookedFormat = "Cibo pronto! Porzioni rimaste: {0}/{1}";

    private BulletinController controller;

    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();
    }

    void OnEnable()
    {
        CookingStation.OnStationStateChanged += RefreshPanel;
        if (station) station.OnStateChanged += RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        CookingStation.OnStationStateChanged -= RefreshPanel;
        if (station) station.OnStateChanged -= RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(int day, DayPhase phase) => RefreshPanel();
    private void RefreshPanel() => controller?.RefreshNow();

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null)
            ? new List<BulletinController.MenuOption>(baseOptions)
            : new List<BulletinController.MenuOption>();

        if (!station)
        {
            list.Add(new BulletinController.MenuOption
            {
                title = "Nessuna stazione collegata",
                action = BulletinController.MenuOption.MenuAction.Label
            });
            return list;
        }

        // Stato live (testo generato qui)
        list.Add(new BulletinController.MenuOption
        {
            title = "",
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = BuildStatusText
        });

        // Inserisci cibo
        if (station.CurrentState == CookingStation.State.Empty)
        {
            list.Add(MakeInvoke("Inserisci cibo", station.InsertFood));
        }

        // Cucina cibo
        if (station.CurrentState == CookingStation.State.Filled)
        {
            list.Add(MakeInvoke("Cucina cibo", station.StartCooking));
        }

        return list;
    }

    private string BuildStatusText()
    {
        if (!station) return emptyText;

        switch (station.CurrentState)
        {
            case CookingStation.State.Filling:
                return string.Format(fillingFormat, Mathf.RoundToInt(station.Progress01 * 100f));
            case CookingStation.State.Filled:
                return filledText;
            case CookingStation.State.Cooking:
                return string.Format(cookingFormat, Mathf.RoundToInt(station.Progress01 * 100f));
            case CookingStation.State.Cooked:
                return string.Format(cookedFormat, station.RemainingServings, station.maxServings);
            default:
                return emptyText;
        }
    }

    private static BulletinController.MenuOption MakeInvoke(string title, UnityAction action)
    {
        var opt = new BulletinController.MenuOption
        {
            title = title,
            action = BulletinController.MenuOption.MenuAction.Invoke,
            onInvoke = new UnityEvent()
        };
        opt.onInvoke.AddListener(action);
        return opt;
    }
}
