using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    [Serializable]
    public class LocalizationEntryCreator
    {
        [HideInInspector] public LocalizationFamily LocalizationFamily;
        [HideInInspector] public LocalizationEntry LocalizationEntry = new LocalizationEntry();
    }
}
