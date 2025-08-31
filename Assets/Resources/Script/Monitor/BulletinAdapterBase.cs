using System.Collections.Generic;
using UnityEngine;

public abstract class BulletinAdapterBase : MonoBehaviour
{
    public abstract List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions);
}
