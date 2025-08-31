using System.Collections.Generic;
using UnityEngine;

public abstract class BulletinAdapterBase : MonoBehaviour
{
    /// <summary>
    /// Riceve le opzioni di base (statiche) e restituisce una nuova lista
    /// con le proprie voci aggiunte (non modificare la lista in ingresso).
    /// </summary>
    public abstract List<BulletinController.MenuOption> BuildOptions(
        List<BulletinController.MenuOption> baseOptions);
}
