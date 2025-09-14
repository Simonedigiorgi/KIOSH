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

    [Tooltip("DeliveryBox usata per spedire: serve a bloccare 'Ottieni piatto' quando il goal è raggiunto.")]
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

        // risolvi i layer
        _interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        _disabledLayer = LayerMask.NameToLayer(disabledLayerName);
        if (_interactableLayer < 0) { _interactableLayer = 0; Debug.LogWarning("[CookingBulletinAdapter] Layer 'Interactable' non trovato, uso Default(0)."); }
        if (_disabledLayer < 0) { _disabledLayer = 0; }

        // fallback: se non setti il target, usa il GO del dispenser
        if (!interactTarget && dishDispenser) interactTarget = dishDispenser.gameObject;
    }

    void OnEnable()
    {
        CookingStation.OnStationStateChanged += RefreshPanel;
        if (station) station.OnStateChanged += RefreshPanel;
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;

        // 🔔 ascolta spedizioni
        DeliveryBox.OnDeliveredCountChanged += HandleDeliveredCountChanged;
        DeliveryBulletinAdapter.OnAllDeliveriesCompleted += HandleAllDeliveriesCompleted;

        // stato iniziale goal (in caso si ricarichi con progresso)
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

        // sicurezza: torna layer disattivo
        ArmInteractTarget(false);
    }

    private void HandlePhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning)
        {
            // NON resetto isDeliveryGoalReached (il goal è per la giornata corrente)
            hasUndeliveredDish = false;
            deliveredCountWhenTaken = DeliveryBox.TotalDelivered;
            ArmInteractTarget(false);
        }
        RefreshPanel();
    }

    private void HandleDeliveredCountChanged(int newTotal)
    {
        // appena spedisci il piatto → riabilita la voce
        if (hasUndeliveredDish && newTotal > deliveredCountWhenTaken)
        {
            hasUndeliveredDish = false;
        }

        // se hai raggiunto il goal → blocca definitivamente
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

        // stato corrente del goal (fallback se l'evento non è ancora arrivato)
        bool goalReachedNow = isDeliveryGoalReached
                              || (deliveryBox && DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal);

        // Stato live
        list.Add(new BulletinController.MenuOption
        {
            title = "",
            action = BulletinController.MenuOption.MenuAction.LiveLabel,
            dynamicTextProvider = BuildStatusText
        });

        // Inserisci cibo
        if (station.CurrentState == CookingStation.State.Empty)
            list.Add(MakeInvoke("Inserisci cibo", station.InsertFood));

        // Cucina cibo
        if (station.CurrentState == CookingStation.State.Filled)
            list.Add(MakeInvoke("Cucina cibo", station.StartCooking));

        // Ottieni piatto → mostra solo se: pentola pronta + non stai aspettando una spedizione + goal non raggiunto
        if (station.CurrentState == CookingStation.State.Cooked &&
            station.CanServeDish() &&
            !hasUndeliveredDish &&
            !goalReachedNow)
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
        // blocco se goal raggiunto (ulteriore safeguard)
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

        // 3) Anti-spam: appena il player prende qualcosa in mano,
        //    disattiviamo di nuovo il target per evitare più piatti in fila.
        if (armRoutine != null) StopCoroutine(armRoutine);
        armRoutine = StartCoroutine(WaitUntilPlayerHoldsSomethingThenDisarm());

        // Aggiorna UI
        RefreshPanel();
    }

    private IEnumerator WaitUntilPlayerHoldsSomethingThenDisarm()
    {
        var player = FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (!player) yield break;

        // aspetta che prenda qualcosa (tipicamente il piatto dal dispenser)
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
