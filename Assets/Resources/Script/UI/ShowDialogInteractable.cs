using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// Aggancialo a un oggetto "cliccabile" (che il tuo PlayerInteractor sa usare).
/// Mostra le righe in sequenza (premi E per avanzare), chiude a fine lista.
/// Si appoggia a HUDManager: niente duplicazione di logica.
[AddComponentMenu("Interaction/Show Dialog Interactable")]
public class ShowDialogInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialog (editabile in Inspector)")]
    [Tooltip("Righe mostrate in sequenza. Premi E per avanzare.")]
    [TextArea(2, 5)] public string[] lines;

    [Header("Events")]
    public UnityEvent onShown;
    public UnityEvent onCompleted;

    // ---------- IInteractable ----------
    public void Interact(PlayerInteractor interactor)
    {
        var hud = HUDManager.Instance;
        if (!hud || hud.IsDialogOpen) return;          // evita sovrapposizioni
        if (lines == null || lines.Length == 0) return;

        hud.ShowDialogLines(lines);
        onShown?.Invoke();

        // attendo la chiusura per notificare onCompleted
        StartCoroutine(WaitDialogEnd());
    }

    public string GetInteractionName() =>
        TryGetComponent<InteractableName>(out var n) && !string.IsNullOrEmpty(n.displayName)
        ? n.displayName
        : "Interagisci";

    // ---------- Helpers ----------
    private IEnumerator WaitDialogEnd()
    {
        // aspetta almeno un frame perché HUDManager imposta IsDialogOpen nel prossimo Update
        yield return null;

        var hud = HUDManager.Instance;
        while (hud && hud.IsDialogOpen)
            yield return null;

        onCompleted?.Invoke();
    }
}
