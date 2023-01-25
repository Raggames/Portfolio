using Sirenix.OdinInspector;
using SteamAndMagic.Systems.LocalizationManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    [CreateAssetMenu(menuName = "ScriptableObjects/LocalizationSetting")]
    public class LocalizationSetting : SerializedScriptableObject
    {
        [ShowInInspector][SerializeField] public Dictionary<LocalizationFamily, LocalizationFamilyData> LocalizationDatas = new Dictionary<LocalizationFamily, LocalizationFamilyData>();

#if UNITY_EDITOR
        [Button("Update Families")]
        private void UpdateFamilies()
        {
            foreach (int i in Enum.GetValues(typeof(LocalizationFamily)))
            {
                if (!LocalizationDatas.ContainsKey((LocalizationFamily)i))
                {
                    LocalizationDatas.Add((LocalizationFamily)i, new LocalizationFamilyData() { LocalizationFamily = (LocalizationFamily)i });
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
