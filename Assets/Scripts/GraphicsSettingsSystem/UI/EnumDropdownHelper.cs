using System.Collections.Generic;
using UnityEngine.UI;

namespace GameSettings.UI
{
    /// <summary>
    /// Небольшой помощник, чтобы не дублировать код заполнения TMP_Dropdown/Dropdown
    /// подписями enum-значений в каждом биндере.
    /// </summary>
    public static class EnumDropdownHelper
    {
        public static void Fill(Dropdown dropdown, string[] labels)
        {
            dropdown.ClearOptions();
            var options = new List<string>(labels);
            dropdown.AddOptions(options);
        }
    }
}
