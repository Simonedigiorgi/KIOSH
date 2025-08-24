using UnityEngine;

public class Cookware : MonoBehaviour
{
    public CookingToolType toolType;
    public Transform cookTarget;

    [Header("Audio")]
    public AudioSource loopAudioSource;
    public AudioClip loopSound;

    private GameObject currentCookingInstance;
    private bool isCooking = false;
    private float timer = 0f;
    private float targetCookTime;
    private Ingredient currentIngredient;

    void Start()
    {
        // Setup audio dinamicamente se non già assegnato
        if (loopAudioSource == null)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.spatialBlend = 1f;
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;
            loopAudioSource.volume = 0.5f;
        }

        if (loopSound != null)
        {
            loopAudioSource.clip = loopSound;
        }
    }

    void Update()
    {
        if (isCooking)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / targetCookTime);

            // Aggiorna lo shader CookProgress
            if (currentCookingInstance != null)
            {
                var renderer = currentCookingInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.SetFloat("_CookProgress", progress);
                }
            }

            if (timer >= targetCookTime)
            {
                OnCookComplete();
            }
        }
    }

    public bool TryAddIngredient(PickupObject pickup)
    {
        if (isCooking || pickup.type != PickupType.Ingredient) return false;

        Ingredient ingredient = pickup.GetComponent<Ingredient>();
        if (ingredient == null || ingredient.cookedPrefab == null) return false;
        if (ingredient.compatibleTool != toolType) return false;

        Destroy(pickup.gameObject);

        currentIngredient = ingredient;
        targetCookTime = ingredient.cookTime;

        currentCookingInstance = Instantiate(
            ingredient.cookedPrefab,
            cookTarget.position,
            cookTarget.rotation,
            cookTarget
        );

        isCooking = true;
        timer = 0f;

        // Avvia suono
        if (loopAudioSource != null && loopSound != null)
            loopAudioSource.Play();

        return true;
    }

    void OnCookComplete()
    {
        isCooking = false;

        // Ferma il suono
        if (loopAudioSource != null && loopAudioSource.isPlaying)
            loopAudioSource.Stop();

        // Nessuna modifica al colore: è gestito dallo shader via CookProgress
    }

    public void ClearCookedIngredient()
    {
        if (currentCookingInstance)
            Destroy(currentCookingInstance);

        currentCookingInstance = null;
        currentIngredient = null;
        isCooking = false;
        timer = 0f;

        if (loopAudioSource != null && loopAudioSource.isPlaying)
            loopAudioSource.Stop();
    }

    public bool HasCookedIngredient()
    {
        return !isCooking && currentCookingInstance != null;
    }

    public void OnPlacedInReceiver() { }
}
