using Sirenix.OdinInspector;
using SteamAndMagic.Systems.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    public enum LocalizationFamily
    {
        Gameplay,
        Effect,
        HUD,
        Tooltip,
        Resources,
        Mailing,
        Quests,
        GameMenus
    }

    public class LocalizationManager : Singleton<LocalizationManager>
    {
        public LocalizationLanguage LocalizationLanguage;
        public List<AbstractLocalizatedObject> localizedObjects = new List<AbstractLocalizatedObject>();

        private static LocalizationLanguage localizationLanguage => Instance.LocalizationLanguage;
        private static LocalizationSetting localizationSetting;
        // Famille => Clé / Language => Valeur 
        private static Dictionary<LocalizationFamily, Dictionary<string, Dictionary<LocalizationLanguage, string>>> cachedLocalization = new Dictionary<LocalizationFamily, Dictionary<string, Dictionary<LocalizationLanguage, string>>>(); 

        [Button("Initialize")]
        public void Initialize()
        {
            cachedLocalization = new Dictionary<LocalizationFamily, Dictionary<string, Dictionary<LocalizationLanguage, string>>>();

            localizationSetting = CoreManager.Settings.localizationSetting;

            localizedObjects = FindObjectsOfType<AbstractLocalizatedObject>(true).ToList();
            //localizedDictionnaries = new Dictionary<LocalizationFamily, Dictionary<string, AbstractLocalizatedObject>>();

            // Initialisation du dictionnaire de localization
            foreach (var valuepair in localizationSetting.LocalizationDatas)
            {
                cachedLocalization.Add(valuepair.Key, new Dictionary<string, Dictionary<LocalizationLanguage, string>>());

                foreach (var localisationEntry in localizationSetting.LocalizationDatas[valuepair.Key].Entries)
                {
                    cachedLocalization[valuepair.Key].Add(localisationEntry.LocalizationKey, new Dictionary<LocalizationLanguage, string>());

                    for(int i = 0; i < localisationEntry.Values.Count; ++i)
                    {
                        cachedLocalization[valuepair.Key][localisationEntry.LocalizationKey][(LocalizationLanguage)i] = localisationEntry.Values[i];
                    }                    
                }
            }

            // load dictionnaries from resources
            for (int i = 0; i < localizedObjects.Count; ++i)
            {
                localizedObjects[i].InitLocalizedObject();            
            }
        }

        public void ChangeLanguage(LocalizationLanguage localizationLanguage)
        {
            LocalizationLanguage = localizationLanguage;

            for (int i = 0; i < localizedObjects.Count; ++i)
            {
                localizedObjects[i].InitLocalizedObject();
            }
        }

        public static string GetLocalizedValue(string key, LocalizationFamily localizationFamily)
        {
            // En attendant d'intégrer le système on retournera simplement la clé
            //return key;
            string result = string.Empty;
            // MINDFUCK
            if (cachedLocalization[localizationFamily].ContainsKey(key))
            {
                result = cachedLocalization[localizationFamily][key][localizationLanguage];
            }
            
            if(result.Length > 0)
            {
                return result;
            }
            else
            {
                return $"?{key}?";
            }
        }

        public static string GetLocalizedAbilityDescription(Ability ability, int autolinebreak = -1)
        {
            // Paramètre de la méthode = valeur localisée quand ce sera prêt
            string localizedDescription = GetLocalizedValue(ability.Description, LocalizationFamily.Gameplay);
            return DynamicStringReader.ReadText(localizedDescription, ability, autolinebreak);
        }

        public static string GetLocalizedTalentDescription(Talent talent, int lineBreak = -1)
        {
            string localizedDescription = GetLocalizedValue(talent.Description, LocalizationFamily.Gameplay);
            return DynamicStringReader.ReadText(localizedDescription, talent, lineBreak);
        }

        public static string GetLocalizedRuneDescription(Rune rune)
        {
            string localizedDescription = GetLocalizedValue(rune.Description, LocalizationFamily.Gameplay);
            return DynamicStringReader.ReadText(localizedDescription, rune, -1);
        }
    }
}
