using Sirenix.OdinInspector;
using SteamAndMagic.Systems.LocalizationManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    [Serializable]
    public class LocalizationFamilyData
    {
        [ReadOnly] public LocalizationFamily LocalizationFamily;

        /// <summary>
        /// Key is for Localization Key.
        /// </summary>
        public List<LocalizationEntry> Entries = new List<LocalizationEntry>();
    }
}
