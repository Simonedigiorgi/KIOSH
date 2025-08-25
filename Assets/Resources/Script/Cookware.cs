using System.Collections;
using UnityEngine;

public class Cookware : MonoBehaviour
{
    public CookingToolType toolType;
    public Transform cookTarget;

    [Header("Audio")]
    public AudioClip loopSound;

    private AudioSource loopAudioSource;

    private GameObject currentCookingInstance;
    private bool isCooking = false;
    private float timer = 0f;
    private float targetCookTime;
    private Ingredient currentIngredient;

    void Start()
    {
        loopAudioSource = GetComponent<AudioSource>();
        if (loopAudioSource == null)
            loopAudioSource = gameObject.AddComponent<AudioSource>();

        loopAudioSource.spatialBlend = 1f;
        loopAudioSource.loop = true;
        loopAudioSource.playOnAwake = false;
        loopAudioSource.volume = 0.5f;

        if (loopSound != null)
            loopAudioSource.clip = loopSound;
    }

    void Update()
    {
        if (isCooking)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / targetCookTime);

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

        if (IngredientManager.Instance.IsIngredientActive(ingredient.ingredientID))
        {
            Debug.Log("❌ Questo ingrediente è già attivo in un'altra cookware.");
            return false;
        }

        currentIngredient = new GameObject("TempIngredient").AddComponent<Ingredient>();
        currentIngredient.compatibleTool = ingredient.compatibleTool;
        currentIngredient.cookedPrefab = ingredient.cookedPrefab;
        currentIngredient.cookTime = ingredient.cookTime;
        currentIngredient.dishPrefab = ingredient.dishPrefab;
        currentIngredient.ingredientID = ingredient.ingredientID;

        Destroy(pickup.gameObject);
        targetCookTime = currentIngredient.cookTime;


        currentCookingInstance = Instantiate(
            ingredient.cookedPrefab,
            cookTarget.position,
            cookTarget.rotation,
            cookTarget
        );

        isCooking = true;
        timer = 0f;

        if (loopAudioSource != null && loopSound != null)
            loopAudioSource.Play();

        IngredientManager.Instance.RegisterIngredient(ingredient.ingredientID);
        return true;
    }

    void OnCookComplete()
    {
        isCooking = false;

        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(1f)); // ⏳ Fade-out in 1 secondo
        }
    }

    IEnumerator FadeOutAudio(float duration)
    {
        float startVolume = loopAudioSource.volume;
        float time = 0f;

        while (time < duration)
        {
            loopAudioSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        loopAudioSource.Stop();
        loopAudioSource.volume = startVolume; // Reset volume per futuri utilizzi
    }

    public void ClearCookedIngredient()
    {
        if (currentIngredient != null)
            IngredientManager.Instance.UnregisterIngredient(currentIngredient.ingredientID);

        if (currentCookingInstance)
            Destroy(currentCookingInstance);

        currentCookingInstance = null;
        currentIngredient = null;
        isCooking = false;
        timer = 0f;

        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(1f));
        }
    }


    public bool HasCookedIngredient()
    {
        return !isCooking && currentCookingInstance != null;
    }

    public Ingredient GetCurrentIngredient()
    {
        return isCooking ? null : currentIngredient;
    }

    public void OnPlacedInReceiver() { }
}
