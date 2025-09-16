using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class CookingBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    [Required] public CookingStation station;

    [Header("Delivery Gate")]
    [Required, Tooltip("DeliveryBox per conoscere il deliveryGoal e bloccare tutto a goal raggiunto.")]
    public DeliveryBox deliveryBox;

    [Header("Dish Spawn")]
    [Required, Tooltip("Prefab del piatto da spawnare quando selezioni 'Ottieni piatto'.")]
    public GameObject dishPrefab;

    [Required, Tooltip("Pivot (Transform) dove spawnare il piatto.")]
    public Transform dishSpawnPivot;

    [Header("UI Strings")]
    [TextArea] public string emptyText = "Stato della Cooking Staion: Operativo";
    [TextArea] public string fillingFormat = "Riempimento, Attendere: {0}%";
    [TextArea] public string filledText = "Cibo inserito, pronto a cucinare";
    [TextArea] public string cookingFormat = "Cottura, Attendere: {0}%";
    [TextArea] public string cookedFormat = "Cibo pronto! Porzioni rimaste: {0}/{1}";
    [TextArea] public string getDishLabel = "Ottieni piatto";
    [TextArea] public string goalReachedText = "Consegne completate — produzione chiusa.";

    [Header("Feedback")]
    [TextArea] public string msgNoStationReady = "La pentola non è pronta o non ha porzioni disponibili.";
    [TextArea] public string msgMissingPrefabOrPivot = "Configurazione mancante: assegna il prefab del piatto e un pivot di spawn.";

    private BulletinController controller;

    // “uno alla volta finché non lo spedisci”
    private bool hasUndeliveredDish = false;
    private int deliveredCountWhenTaken = 0;

    // gate “giornaliero” quando arrivi al goal
    private bool isDeliveryGoalReached = false;

    // ----------------- Lifecycle -----------------
    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();

        // Odin [Required] già segnala in Inspector, qui solo log safety
        if (!station) Debug.LogError("[CookingBulletinAdapter] 'station' non assegnata.");
        if (!deliveryBox) Debug.LogError("[CookingBulletinAdapter] 'deliveryBox' non assegnata.");
        if (!dishPrefab) Debug.LogError("[CookingBulletinAdapter] 'dishPrefab' non assegnato.");
        if (!dishSpawnPivot) Debug.LogError("[CookingBulletinAdapter] 'dishSpawnPivot' non assegnato.");
    }

    void OnEnable()
    {
        CookingStation.OnStationStateChanged += RefreshPanel;
        if (station) station.OnStateChanged += RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;

        DeliveryBox.OnDeliveredCountChanged += HandleDeliveredCountChanged;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;

        if (deliveryBox && DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal)
            isDeliveryGoalReached = true;
    }

    void OnDisable()
    {
        CookingStation.OnStationStateChanged -= RefreshPanel;
        if (station) station.OnStateChanged -= RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;

        DeliveryBox.OnDeliveredCountChanged -= HandleDeliveredCountChanged;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;
    }

    // ----------------- Events -----------------
    private void HandlePhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning) ResetDailyGate();
        RefreshPanel();
    }

    private void HandleDeliveredCountChanged(int newTotal)
    {
        if (PlayerDispatchedSinceTaken(newTotal)) hasUndeliveredDish = false;
        if (ReachedGoal(newTotal)) MarkGoalReached();
        RefreshPanel();
    }

    private void HandleAllDeliveriesCompleted()
    {
        MarkGoalReached();
        RefreshPanel();
    }

    // ----------------- Build Options -----------------
    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = CloneOrNew(baseOptions);

        if (!station)
        {
            list.Add(MakeLabel("Nessuna stazione collegata"));
            return list;
        }

        // Stato live
        list.Add(new BulletinController.MenuOption
        {
            title = "",
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = BuildStatusText
        });

        // Goal raggiunto → info e stop
        if (GoalReachedNow)
        {
            list.Add(MakeLabel(goalReachedText));
            return list;
        }

        // Inserisci / Cucina
        if (station.CurrentState == CookingStation.State.Empty)
            AddInvoke(list, "Inserisci cibo", station.InsertFood);

        if (station.CurrentState == CookingStation.State.Filled)
            AddInvoke(list, "Cucina cibo", station.StartCooking);

        // Ottieni piatto (spawn diretto)
        if (ShouldShowGetDish())
            AddInvoke(list, getDishLabel, HandleSpawnDish);

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

    // ----------------- Actions -----------------
    private void HandleSpawnDish()
    {
        if (GoalReachedNow) { MarkGoalReached(); RefreshPanel(); return; }
        if (!station || !station.CanServeDish()) { HUDManager.Instance?.ShowDialog(msgNoStationReady); return; }
        if (!dishPrefab || !dishSpawnPivot)
        {
            HUDManager.Instance?.ShowDialog(msgMissingPrefabOrPivot);
            Debug.LogWarning("[CookingBulletinAdapter] Mancano dishPrefab e/o dishSpawnPivot.");
            return;
        }

        // Spawn in world-space sul pivot
        var go = Object.Instantiate(dishPrefab, dishSpawnPivot.position, dishSpawnPivot.rotation, null);

        // set come pickup liberamente raccoglibile
        var pickup = go.GetComponent<PickupObject>();
        if (pickup) { pickup.canBePickedUp = true; pickup.isHeld = false; }

        // Gate “uno alla volta”
        hasUndeliveredDish = true;
        deliveredCountWhenTaken = DeliveryBox.TotalDelivered;

        RefreshPanel();
    }

    // ----------------- Helpers -----------------
    private void RefreshPanel() => controller?.RefreshNow();

    private bool GoalReachedNow =>
        isDeliveryGoalReached || (deliveryBox && DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal);

    private static List<BulletinController.MenuOption> CloneOrNew(List<BulletinController.MenuOption> src)
        => (src != null) ? new List<BulletinController.MenuOption>(src) : new List<BulletinController.MenuOption>();

    private static void AddInvoke(List<BulletinController.MenuOption> list, string title, UnityAction action)
        => list.Add(MakeInvoke(title, action));

    private static BulletinController.MenuOption MakeLabel(string text)
        => new BulletinController.MenuOption { title = text, action = BulletinController.MenuOption.MenuAction.Label };

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

    private bool ShouldShowGetDish()
        => station.CurrentState == CookingStation.State.Cooked && station.CanServeDish() && !hasUndeliveredDish && !GoalReachedNow;

    private bool PlayerDispatchedSinceTaken(int newTotal) => hasUndeliveredDish && newTotal > deliveredCountWhenTaken;

    private bool ReachedGoal(int total) => deliveryBox && total >= deliveryBox.deliveryGoal;

    private void MarkGoalReached()
    {
        isDeliveryGoalReached = true;
        hasUndeliveredDish = false;
    }

    private void ResetDailyGate()
    {
        isDeliveryGoalReached = false;
        hasUndeliveredDish = false;
        deliveredCountWhenTaken = DeliveryBox.TotalDelivered;
    }
}
