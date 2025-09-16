using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class CookingStation : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [Required] public Transform fillCylinder;

    [Header("Liquid Jet")]
    [Tooltip("GameObject del getto liquido (es. cilindro/particle).")]
    public GameObject liquidJet;
    [SerializeField, Min(0f)] private float liquidFadeTime = 0.3f;
    private Coroutine liquidRoutine;

    [Header("Times")]
    [Min(0.01f)] public float fillTime = 3f;
    [Min(0.01f)] public float cookTime = 5f;
    [Min(0f)] public float consumeAnimTime = 0.25f;

    [Header("Servings")]
    [Min(1)] public int maxServings = 5;
    public int RemainingServings => _remainingServings;
    private int _remainingServings;

    [Header("Fill Y Range (solo estetico)")]
    public float emptyY = 0.02f;
    public float fullY = 0.24f;

    [Header("Delivery Gate (OBBLIGATORIO)")]
    [Required, Tooltip("Blocca la produzione quando TotalDelivered >= deliveryGoal.")]
    [SerializeField] private DeliveryBox deliveryBox;

    // messaggio privato (non più public)
    private const string MSG_GOAL_REACHED = "Tutte le consegne sono state completate. Non è possibile preparare altro cibo.";

    private Coroutine currentRoutine;
    public float Progress01 => _progress;   // usato dalla UI per le percentuali
    private float _progress;

    public enum State { Empty, Filling, Filled, Cooking, Cooked }
    public State CurrentState { get; private set; } = State.Empty;

    // Eventi per UI/adapter
    public static event Action OnStationStateChanged;
    public event Action OnStateChanged;

    // ---------- Lifecycle ----------
    private void Awake()
    {
        if (!fillCylinder)
            Debug.LogError("[CookingStation] Assegna 'fillCylinder' nel Inspector.");

        if (!deliveryBox)
        {
            Debug.LogError("[CookingStation] 'deliveryBox' è OBBLIGATORIO. Disabilito la stazione.");
            enabled = false;
            return;
        }

        if (liquidJet) liquidJet.SetActive(false);
        SetCylinderHeight(emptyY);
        _progress = 0f;
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning)
            ResetToDefault(); // ogni mattino torna vuota
    }

    /// Reset completo allo stato di default.
    public void ResetToDefault()
    {
        StopActiveRoutines();

        _progress = 0f;
        _remainingServings = 0;
        SetState(State.Empty);

        SetCylinderHeight(emptyY);
        if (liquidJet) liquidJet.SetActive(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[CookingStation] Reset al mattino → stato: Empty");
#endif
        RaiseStateChanged();
    }

    private void StopActiveRoutines()
    {
        if (currentRoutine != null) { StopCoroutine(currentRoutine); currentRoutine = null; }
        if (liquidRoutine != null) { StopCoroutine(liquidRoutine); liquidRoutine = null; }
    }

    private void SetState(State s)
    {
        if (CurrentState == s) return;
        CurrentState = s;
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        OnStateChanged?.Invoke();
        OnStationStateChanged?.Invoke();
    }

    // ---------- Gate ----------
    private bool IsDeliveryGoalReached() => DeliveryBox.TotalDelivered >= deliveryBox.deliveryGoal;

    private bool DenyIfGoalReached()
    {
        if (IsDeliveryGoalReached())
        {
            HUDManager.Instance?.ShowDialog(MSG_GOAL_REACHED);
            return true;
        }
        return false;
    }

    // ---------- Interazione ----------
    public void Interact(PlayerInteractor player)
    {
        var held = player?.HeldPickup;
        if (!held) return;

        var dish = held.GetComponent<Dish>();
        if (!dish) return;

        if (dish.IsComplete)
        {
            HUDManager.Instance?.ShowDialog("Questo piatto è già pieno.");
            return;
        }
        if (!CanServeDish())
        {
            HUDManager.Instance?.ShowDialog("La pentola non è pronta.");
            return;
        }

        if (dish.TryAddFromStation(this))
            ConsumeServing();
    }

    // --- Inserimento ---
    public void InsertFood()
    {
        if (CurrentState != State.Empty) return;
        if (DenyIfGoalReached()) return;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FillRoutine());
    }

    private IEnumerator FillRoutine()
    {
        SetState(State.Filling);
        _progress = 0f;

        SetLiquidJet(true);

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, fillTime);
        while (t < fillTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t * inv);
            SetCylinderHeight(Mathf.Lerp(emptyY, fullY, k));
            _progress = k;
            yield return null;
        }

        _progress = 1f;
        SetState(State.Filled);

        SetLiquidJet(false);
        currentRoutine = null;
    }

    // --- Cottura ---
    public void StartCooking()
    {
        if (CurrentState != State.Filled) return;
        if (DenyIfGoalReached()) return;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(CookRoutine());
    }

    private IEnumerator CookRoutine()
    {
        SetState(State.Cooking);
        _progress = 0f;

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, cookTime);
        while (t < cookTime)
        {
            t += Time.deltaTime;
            _progress = Mathf.Clamp01(t * inv);
            yield return null;
        }

        _progress = 1f;

        _remainingServings = Mathf.Max(1, maxServings);
        SetCylinderHeight(fullY);
        SetState(State.Cooked);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CookingStation] Cottura completata. Porzioni: {_remainingServings}/{maxServings}");
#endif
        currentRoutine = null;
    }

    // --- Servings ---
    public bool CanServeDish() => CurrentState == State.Cooked && _remainingServings > 0;

    public void ConsumeServing()
    {
        if (_remainingServings <= 0) return;

        _remainingServings = Mathf.Max(_remainingServings - 1, 0);

        float ratio = (maxServings > 0) ? (float)_remainingServings / maxServings : 0f;
        float targetY = Mathf.Lerp(emptyY, fullY, ratio);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        bool isLast = _remainingServings <= 0;
        currentRoutine = StartCoroutine(LerpCylinderY(targetY, isLast));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CookingStation] Porzione servita. Rimaste: {_remainingServings}/{maxServings}");
#endif

        if (!isLast) RaiseStateChanged();
    }

    private IEnumerator LerpCylinderY(float targetY, bool resetAfter = false)
    {
        float startY = fillCylinder ? fillCylinder.localPosition.y : 0f;
        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, consumeAnimTime);
        while (t < consumeAnimTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t * inv);
            SetCylinderHeight(Mathf.Lerp(startY, targetY, k));
            yield return null;
        }
        SetCylinderHeight(targetY);
        currentRoutine = null;

        if (resetAfter)
        {
            yield return null; // lascia aggiornare la UI
            ResetStation();
        }
    }

    // --- Helpers ---
    private void SetCylinderHeight(float y)
    {
        if (!fillCylinder) return;
        var p = fillCylinder.localPosition;
        fillCylinder.localPosition = new Vector3(p.x, y, p.z);
    }

    private void ResetStation()
    {
        StopActiveRoutines();

        SetCylinderHeight(emptyY);
        _progress = 0f;
        _remainingServings = 0;
        if (liquidJet) liquidJet.SetActive(false);

        SetState(State.Empty);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[CookingStation] Pentola svuotata. Stato: Empty");
#endif
    }

    private void SetLiquidJet(bool active)
    {
        if (!liquidJet) return;
        if (liquidRoutine != null) StopCoroutine(liquidRoutine);
        liquidRoutine = StartCoroutine(LerpLiquidJet(active));
    }

    private IEnumerator LerpLiquidJet(bool turnOn)
    {
        var tr = liquidJet.transform;

        if (turnOn && !liquidJet.activeSelf)
        {
            liquidJet.SetActive(true);
            var s = tr.localScale;
            tr.localScale = new Vector3(0f, s.y, 0f); // raggio 0, Y invariata
        }

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, liquidFadeTime);
        Vector3 start = tr.localScale;
        Vector3 target = turnOn ? new Vector3(0.75f, start.y, 0.75f) : new Vector3(0f, start.y, 0f);

        while (t < liquidFadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t * inv);
            var s = Vector3.Lerp(start, target, k);
            tr.localScale = new Vector3(s.x, start.y, s.z); // blocca Y
            yield return null;
        }

        tr.localScale = target;

        if (!turnOn) liquidJet.SetActive(false);
        liquidRoutine = null;
    }
}
