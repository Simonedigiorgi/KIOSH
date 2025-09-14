using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CookingBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public CookingStation station;

    [Header("Dish / Interact")]
    [Tooltip("Oggetto da rendere cliccabile (Layer Interactable) quando selezioni 'Ottieni piatto'. " +
             "Se lasci vuoto, userà il GameObject del DishDispenser sottostante.")]
    public GameObject interactTarget;

    [Tooltip("Script che, quando clicchi (Interact), dà il piatto al giocatore.")]
    public DishDispenser dishDispenser;

    [Tooltip("DeliveryBox usata per spedire: serve a bloccare tutto quando il goal è raggiunto.")]
    public DeliveryBox deliveryBox;

    [Header("Layers")]
    [SerializeField] private string interactableLayerName = "Interactable";
    [SerializeField] private string disabledLayerName = "Default";

    private int _interactableLayer = -1;
    private int _disabledLayer = -1;

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
    [TextArea] public string msgNoDispenser = "Nessun dispenser collegato.";

    private BulletinController controller;

    // gating: “uno alla volta finché non lo spedisci”
    private bool hasUndeliveredDish = false;
    private int deliveredCountWhenTaken = 0;

    // blocco quando raggiungi il goal
    private bool isDeliveryGoalReached = false;

    private Coroutine armRoutine;

    void Awake()
    {
        controller = GetComponentInParent<BulletinController>();

        _interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        _disabledLayer = LayerMask.NameToLayer(disabledLayerName);
        if (_interactableLayer < 0) { _interactableLayer = 0; Debug.LogWarning("[CookingBulletinAdapter] Layer 'Interactable' non trovato, uso Default(0)."); }
        if (_disabledLayer < 0) { _disabledLayer = 0; }

        if (!interactTarget && dishDispenser) interactTarget = dishDispenser.gameObject;

        // assicura che la station conosca la deliveryBox per il gate (opzionale ma utile)
        if (station && deliveryBox && !station.deliveryBox)
            station.deliveryBox = deliveryBox;
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
        {
            isDeliveryGoalReached = true;
            ArmInteractTarget(false);
        }
    }

    void OnDisable()
    {
        CookingStation.OnStationStateChanged -= RefreshPanel;
        if (station) station.OnStateChanged -= RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;

        DeliveryBox.OnDeliveredCountChanged -= HandleDeliveredCountChanged;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted -= HandleAllDeliveriesCompleted;

        ArmInteractTarget(false);
    }

    private void HandlePhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning)
        {
            // ✅ nuovo giorno → pannello operativo
            isDeliveryGoalReached = false;        // sblocca UI
            hasUndeliveredDish = false;           // azzera gating "uno alla volta"
            deliveredCountWhenTaken = DeliveryBox.TotalDelivered; // sarà 0 dopo il reset
            ArmInteractTarget(false);             // sicurezza
        }
        RefreshPanel();
    }

    private void HandleDeliveredCountChanged(int newTotal)
    {
        if (hasUndeliveredDish && newTotal > deliveredCountWhenTaken)
        {
            hasUndeliveredDish = false;
        }

        if (deliveryBox && newTotal >= deliveryBox.deliveryGoal)
        {
            isDeliveryGoalReached = true;
            ArmInteractTarget(false);
        }

        RefreshPanel();
    }

    private void HandleAllDeliveriesCompleted()
    {
        isDeliveryGoalReached = true;
        ArmInteractTarget(false);
        RefreshPanel();
    }

    private void RefreshPanel() => controller?.RefreshNow();

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null)
            ? new List<BulletinController.MenuOption>(baseOptions)
            : new List<BulletinController.MenuOption>();

        if (!station)
        {
            list.Add(new BulletinController.MenuOption { title = "Nessuna stazione collegata", action = BulletinController.MenuOption.MenuAction.Label });
            return list;
        }

        bool goalReachedNow = isDeliveryGoalReached
                              || (deliveryBox && DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal);

        // Stato live
        list.Add(new BulletinController.MenuOption
        {
            title = "",
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = BuildStatusText
        });

        // Se goal raggiunto, mostra un messaggio e basta
        if (goalReachedNow)
        {
            list.Add(new BulletinController.MenuOption
            {
                title = goalReachedText,
                action = BulletinController.MenuOption.MenuAction.Label
            });
            return list;
        }

        // Inserisci cibo
        if (station.CurrentState == CookingStation.State.Empty)
            list.Add(MakeInvoke("Inserisci cibo", station.InsertFood));

        // Cucina cibo
        if (station.CurrentState == CookingStation.State.Filled)
            list.Add(MakeInvoke("Cucina cibo", station.StartCooking));

        // Ottieni piatto → mostra solo se: pentola pronta + non stai aspettando una spedizione
        if (station.CurrentState == CookingStation.State.Cooked &&
            station.CanServeDish() &&
            !hasUndeliveredDish)
        {
            list.Add(MakeInvoke(getDishLabel, HandleArmGetDish));
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

    // ====== “Ottieni piatto” → arma/disarma il target Interactable ======
    private void HandleArmGetDish()
    {
        // safeguard se nel frattempo hai raggiunto il goal
        if (deliveryBox && DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal)
        {
            isDeliveryGoalReached = true;
            ArmInteractTarget(false);
            RefreshPanel();
            return;
        }

        if (!station || !station.CanServeDish())
        {
            HUDManager.Instance?.ShowDialog(msgNoStationReady);
            return;
        }
        if (!dishDispenser || !interactTarget)
        {
            HUDManager.Instance?.ShowDialog(msgNoDispenser);
            Debug.LogWarning("[CookingBulletinAdapter] Dispenser o InteractTarget non assegnato.");
            return;
        }

        // 1) Rendi cliccabile il target
        ArmInteractTarget(true);

        // 2) Nascondi subito la voce finché non spedisci
        hasUndeliveredDish = true;
        deliveredCountWhenTaken = DeliveryBox.TotalDelivered;

        // 3) Anti-spam: quando il player prende qualcosa in mano → disarma
        if (armRoutine != null) StopCoroutine(armRoutine);
        armRoutine = StartCoroutine(WaitUntilPlayerHoldsSomethingThenDisarm());

        RefreshPanel();
    }

    private IEnumerator WaitUntilPlayerHoldsSomethingThenDisarm()
    {
        var player = FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (!player) yield break;

        while (!player.IsHoldingObject())
            yield return null;

        ArmInteractTarget(false);
        armRoutine = null;
    }

    private void ArmInteractTarget(bool on)
    {
        var go = interactTarget ? interactTarget : (dishDispenser ? dishDispenser.gameObject : null);
        if (!go) return;

        SetLayerRecursively(go, on ? _interactableLayer : _disabledLayer);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (!go) return;
        go.layer = layer;
        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}
