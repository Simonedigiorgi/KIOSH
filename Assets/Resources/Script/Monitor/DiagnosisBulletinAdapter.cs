using System.Collections.Generic;
using UnityEngine;

public class DiagnosisBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public RoomDoor roomDoor;   // porta da monitorare

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions)
                                         : new List<BulletinController.MenuOption>();

        // Evita doppione "Diagnosi" se già presente nelle statiche
        bool hasDiagnosis = list.Exists(o => o != null && o.title == "Diagnosi");

        if (!hasDiagnosis)
        {
            var diagnosis = new BulletinController.MenuOption
            {
                title = "Diagnosi",
                action = BulletinController.MenuOption.MenuAction.OpenSubmenu,
                subOptions = new List<BulletinController.MenuOption>()
            };

            if (roomDoor != null)
            {
                string status = roomDoor.canBeOpened ? "Porta apribile" : "Porta bloccata";
                diagnosis.subOptions.Add(new BulletinController.MenuOption
                {
                    title = status,
                    action = BulletinController.MenuOption.MenuAction.Label
                });
            }

            list.Add(diagnosis);
        }

        return list;
    }
}
