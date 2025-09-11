using System;
using System.Collections;
using UnityEngine;

public class CookingStation : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public Transform fillCylinder;
    public Renderer fillRenderer;

    [Header("Times")]
    public float fillTime = 3f;
    public float cookTime = 5f;
    public float consumeAnimTime = 0.25f;

    [Header("Servings")]
    public int maxServings = 5;
    private int remainingServings = 0;

    [Header("Fill Y Range (solo estetico)")]
    public float emptyY = 0.02f; // livello a vuoto
    public float fullY = 0.24f;  // livello a pieno

    private Coroutine currentRoutine;
    private float progress = 0f;

    public enum State { Empty, Filling, Filled, Cooking, Cooked }
    public State CurrentState { get; private set; } = State.Empty;

    // 🔔 Eventi globali per refresh pannello
    public static event Action OnStationStateChanged;
    private void RaiseStateChanged() => OnStationStateChanged?.Invoke();

    // Shader perf
    private static readonly int CookProgressID = Shader.PropertyToID("_CookProgress");
    private MaterialPropertyBlock mpb;

    // ---------- Lifecycle ----------
    private void Awake()
    {
        if (fillRenderer && mpb == null) mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void OnPhaseChanged(int day, DayPhase phase)
    {
        // Reset SOLO al mattino
        if (phase == DayPhase.Morning)
            ResetToDefault();
    }

    /// <summary>
    /// Reset completo allo stato di default (vuota, progress 0, shader 0).
    /// Puoi anche richiamarlo da UnityEvent.
    /// </summary>
    public void ResetToDefault()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        progress = 0f;
        remainingServings = 0;
        CurrentState = State.Empty;

        SetCylinderHeight(emptyY);
        UpdateShader(0f);

        Debug.Log("[CookingStation] Reset al mattino → stato: Empty");
        RaiseStateChanged();
    }

    // ---------- Interazione ----------
    public void Interact(PlayerInteractor player)
    {
        var held = player.HeldPickup;
        if (held == null) return;

        var dish = held.GetComponent<Dish>();
        if (dish == null) return;

        // Feedback chiari
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
        CurrentState = State.Filling;
        progress = 0f;
        RaiseStateChanged();

        float t = 0f;
        while (t < fillTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fillTime);
            SetCylinderHeight(Mathf.Lerp(emptyY, fullY, k));
            progress = k;
            yield return null;
        }

        CurrentState = State.Filled;
        progress = 1f;
        RaiseStateChanged();
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
        CurrentState = State.Cooking;
        progress = 0f;
        RaiseStateChanged();

        UpdateShader(0f);

        float t = 0f;
        while (t < cookTime)
        {
            t += Time.deltaTime;
            progress = Mathf.Clamp01(t / cookTime);
            UpdateShader(progress);
            yield return null;
        }

        CurrentState = State.Cooked;
        progress = 1f;
        UpdateShader(1f);

        // ✅ porzioni pronte, SEMPRE uguali a maxServings
        remainingServings = Mathf.Max(1, maxServings);
        SetCylinderHeight(fullY);

        Debug.Log($"[CookingStation] Cottura completata. Porzioni: {remainingServings}/{maxServings}");
        RaiseStateChanged();
    }

    // --- Servings ---
    public bool CanServeDish() => CurrentState == State.Cooked && remainingServings > 0;

    public void ConsumeServing()
    {
        if (remainingServings <= 0) return;

        remainingServings = Mathf.Max(remainingServings - 1, 0);

        // Altezza puramente estetica, proporzionale alle porzioni rimaste
        float ratio = (maxServings > 0) ? (float)remainingServings / maxServings : 0f;
        float targetY = Mathf.Lerp(emptyY, fullY, ratio);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(LerpCylinderY(targetY));

        Debug.Log($"[CookingStation] Porzione servita. Rimaste: {remainingServings}/{maxServings}");

        if (remainingServings <= 0)
            ResetStation();   // torna vuota solo quando FINISCE davvero
        else
            RaiseStateChanged();
    }

    private IEnumerator LerpCylinderY(float targetY)
    {
        float startY = fillCylinder.localPosition.y;
        float t = 0f;
        while (t < consumeAnimTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / consumeAnimTime);
            SetCylinderHeight(Mathf.Lerp(startY, targetY, k));
            yield return null;
        }
        SetCylinderHeight(targetY);
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
        SetCylinderHeight(emptyY);
        progress = 0f;
        CurrentState = State.Empty;
        remainingServings = 0;
        Debug.Log("[CookingStation] Pentola svuotata. Stato: Empty");
        RaiseStateChanged();
    }

    // --- UI ---
    public string GetProgressText()
    {
        return CurrentState switch
        {
            State.Filling => $"Riempimento: {(progress * 100f):F0}%",
            State.Cooking => $"Cottura: {(progress * 100f):F0}%",
            State.Filled => "Cibo inserito, pronto a cucinare",
            State.Cooked => $"✅ Cibo pronto! Porzioni rimaste: {remainingServings}/{maxServings}",
            _ => "Vuoto"
        };
    }

    private void HandlePhaseChanged(int day, DayPhase phase)
    {
        if (phase == DayPhase.Morning)
        {
            // torna sempre allo stato di default al mattino
            ResetStation();
        }
        // se volessi behavior speciale di notte, gestiscilo qui
    }
}
