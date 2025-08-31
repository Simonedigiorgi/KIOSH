using System.Collections.Generic;
using UnityEngine;

public class DiagnosisBulletinAdapter : BulletinAdapterBase
{
    [Header("Refs")]
    public RoomDoor roomDoor;   // porta da monitorare

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = new List<BulletinController.MenuOption>();

        // 1. Manteniamo quello che già c’è
        if (baseOptions != null)
            list.AddRange(baseOptions);

        // 2. Aggiungiamo Diagnosi (una sola volta)
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

        // appendiamo solo se non c’è già
        if (!list.Exists(opt => opt.title == diagnosis.title))
            list.Add(diagnosis);

        return list;
    }
}
