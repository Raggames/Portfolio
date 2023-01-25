using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    public abstract class AbstractLocalizatedObject : MonoBehaviour
    {
        public string LocalizedKey = "";
        public LocalizationFamily LocalizationFamily;

        public abstract void InitLocalizedObject();
    }
}
