using System.Collections.Generic;
using TMPro;

namespace GameSettings.UI
{
    /// <summary>
    /// Небольшой помощник, чтобы не дублировать код заполнения TMP_Dropdown
    /// подписями enum-значений в каждом биндере.
    /// </summary>
    public static class EnumDropdownHelper
    {
        public static void Fill(TMP_Dropdown dropdown, string[] labels)
        {
            dropdown.ClearOptions();
            var options = new List<string>(labels);
            dropdown.AddOptions(options);
        }
    }
}
