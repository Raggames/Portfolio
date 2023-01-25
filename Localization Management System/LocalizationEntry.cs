using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    [Serializable]
    public class LocalizationEntry
    {
        [HideInInspector] public LocalizationFamily LocalizationFamily;
        public string LocalizationKey;
        public List<string> Values = new List<string>();
    }
}
