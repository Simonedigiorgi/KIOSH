using System;
using System.Collections;
using UnityEngine;

public class CookingStation : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public Transform fillCylinder;
    public Renderer fillRenderer;

    [Header("Liquid Jet")]
    [Tooltip("GameObject del getto liquido (es. cilindro con shader/particle).")]
    public GameObject liquidJet;
    [SerializeField] private float liquidFadeTime = 0.3f; // tempo animazione fade
    private Coroutine liquidRoutine;

    [Header("Times")]
    public float fillTime = 3f;
    public float cookTime = 5f;
    public float consumeAnimTime = 0.25f;

    [Header("Servings")]
    public int maxServings = 5;
    public int RemainingServings => remainingServings;
    private int remainingServings = 0;

    [Header("Fill Y Range (solo estetico)")]
    public float emptyY = 0.02f; // livello a vuoto
    public float fullY = 0.24f;  // livello a pieno

    private Coroutine currentRoutine;
    public float Progress01 => progress;
    private float progress = 0f;

    public enum State { Empty, Filling, Filled, Cooking, Cooked }
    public State CurrentState { get; private set; } = State.Empty;

    // Eventi per UI/adapter
    public static event Action OnStationStateChanged;
    public event Action OnStateChanged;              // di istanza (più fine)

    // Shader perf
    private static readonly int CookProgressID = Shader.PropertyToID("_CookProgress");
    private MaterialPropertyBlock mpb;

    // ---------- Lifecycle ----------
    private void Awake()
    {
        if (fillRenderer && mpb == null) mpb = new MaterialPropertyBlock();
        if (liquidJet) liquidJet.SetActive(false);
        SetCylinderHeight(emptyY);
        UpdateShader(0f);
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

        progress = 0f;
        remainingServings = 0;
        SetState(State.Empty);

        SetCylinderHeight(emptyY);
        UpdateShader(0f);
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

    // ---------- Interazione ----------
    public void Interact(PlayerInteractor player)
    {
        var held = player.HeldPickup;
        if (held == null) return;

        var dish = held.GetComponent<Dish>();
        if (dish == null) return;

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
        {
            ConsumeServing();
        }
    }

    // --- Inserimento ---
    public void InsertFood()
    {
        if (CurrentState != State.Empty) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FillRoutine());
    }

    private IEnumerator FillRoutine()
    {
        SetState(State.Filling);
        progress = 0f;

        SetLiquidJet(true); // accendi con fade

        float t = 0f;
        while (t < fillTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fillTime);
            SetCylinderHeight(Mathf.Lerp(emptyY, fullY, k));
            progress = k;
            yield return null;
        }

        progress = 1f;
        SetState(State.Filled);

        SetLiquidJet(false); // spegni con fade

        currentRoutine = null;
    }

    // --- Cottura ---
    public void StartCooking()
    {
        if (CurrentState != State.Filled) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(CookRoutine());
    }

    private IEnumerator CookRoutine()
    {
        SetState(State.Cooking);
        progress = 0f;
        UpdateShader(0f);

        float t = 0f;
        while (t < cookTime)
        {
            t += Time.deltaTime;
            progress = Mathf.Clamp01(t / cookTime);
            UpdateShader(progress);
            yield return null;
        }

        progress = 1f;
        UpdateShader(1f);

        remainingServings = Mathf.Max(1, maxServings);
        SetCylinderHeight(fullY);
        SetState(State.Cooked);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CookingStation] Cottura completata. Porzioni: {remainingServings}/{maxServings}");
#endif
        currentRoutine = null;
    }

    // --- Servings ---
    public bool CanServeDish() => CurrentState == State.Cooked && remainingServings > 0;

    public void ConsumeServing()
    {
        if (remainingServings <= 0) return;

        remainingServings = Mathf.Max(remainingServings - 1, 0);

        float ratio = (maxServings > 0) ? (float)remainingServings / maxServings : 0f;
        float targetY = Mathf.Lerp(emptyY, fullY, ratio);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(LerpCylinderY(targetY));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CookingStation] Porzione servita. Rimaste: {remainingServings}/{maxServings}");
#endif

        if (remainingServings <= 0)
            ResetStation();   // torna vuota quando finisce davvero
        else
            RaiseStateChanged();
    }

    private IEnumerator LerpCylinderY(float targetY)
    {
        float startY = fillCylinder ? fillCylinder.localPosition.y : 0f;
        float t = 0f;
        while (t < consumeAnimTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / consumeAnimTime);
            SetCylinderHeight(Mathf.Lerp(startY, targetY, k));
            yield return null;
        }
        SetCylinderHeight(targetY);
        currentRoutine = null;
    }

    // --- Helpers ---
    private void SetCylinderHeight(float y)
    {
        if (!fillCylinder) return;
        var p = fillCylinder.localPosition;
        fillCylinder.localPosition = new Vector3(p.x, y, p.z);
    }

    private void UpdateShader(float value)
    {
        if (!fillRenderer) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        fillRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(CookProgressID, value);
        fillRenderer.SetPropertyBlock(mpb);
    }

    private void ResetStation()
    {
        StopActiveRoutines();

        SetCylinderHeight(emptyY);
        progress = 0f;
        remainingServings = 0;
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
            tr.localScale = new Vector3(0f, tr.localScale.y, 0f); // raggio 0, altezza invariata
        }

        float t = 0f;
        Vector3 start = tr.localScale;
        Vector3 target = turnOn ? new Vector3(0.75f, start.y, 0.75f) : new Vector3(0f, start.y, 0f);

        while (t < liquidFadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / liquidFadeTime);
            var s = Vector3.Lerp(start, target, k);
            tr.localScale = new Vector3(s.x, start.y, s.z); // blocca Y
            yield return null;
        }

        tr.localScale = target;

        if (!turnOn) liquidJet.SetActive(false);
        liquidRoutine = null;
    }
}
