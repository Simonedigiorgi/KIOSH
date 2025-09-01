using System.Collections.Generic;
using UnityEngine;

public class DiagnosisBulletinAdapter : BulletinAdapterBase
{
    [System.Serializable]
    public class DiagnosticSystem
    {
        public string name;
        public bool isOperational = true;

        public string GetStatusText()
        {
            return $"{name}: {(isOperational ? "operativo" : "non operativo")}";
        }
    }

    [Header("Sistemi di diagnostica (mock)")]
    public List<DiagnosticSystem> systems = new List<DiagnosticSystem>
    {
        new DiagnosticSystem { name = "Tubo pneumatico", isOperational = true },
        new DiagnosticSystem { name = "Luci", isOperational = true },
        new DiagnosticSystem { name = "Corrente", isOperational = true },
        new DiagnosticSystem { name = "Sistema informatico", isOperational = true },
        new DiagnosticSystem { name = "Ventilazione", isOperational = false }
    };

    public override List<BulletinController.MenuOption> BuildOptions(List<BulletinController.MenuOption> baseOptions)
    {
        var list = (baseOptions != null) ? new List<BulletinController.MenuOption>(baseOptions)
                                         : new List<BulletinController.MenuOption>();

        // Evita doppione "Diagnosi" se già presente
        bool hasDiagnosis = list.Exists(o => o != null && o.title == "Diagnosi");

        if (!hasDiagnosis)
        {
            var diagnosis = new BulletinController.MenuOption
            {
                title = "Diagnosi",
                action = BulletinController.MenuOption.MenuAction.OpenSubmenu,
                subOptions = new List<BulletinController.MenuOption>()
            };

            foreach (var sys in systems)
            {
                var opt = new BulletinController.MenuOption
                {
                    title = sys.GetStatusText(),
                    action = BulletinController.MenuOption.MenuAction.Label
                };

                // 🔴 Se non operativo → usa colore rosso
                if (!sys.isOperational)
                {
                    opt.customColor = Color.red;
                }

                diagnosis.subOptions.Add(opt);
            }

            list.Add(diagnosis);
        }

        return list;
    }
}
