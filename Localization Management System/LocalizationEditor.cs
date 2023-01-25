#if UNITY_EDITOR
using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace SteamAndMagic.Systems.LocalizationManagement
{
    class LocalizationEditor : OdinMenuEditorWindow
    {
        private static LocalizationSetting localizationSetting;
        private static Vector2 localizationEntryScrollViewValue;
        private static LocalizationEntry currentLocalizationEntry;

        [MenuItem("Steam And Magic/Localization Editor")]
        private static void OpenWindow()
        {
            GetWindow<LocalizationEditor>().Show();
            localizationSetting = EditorHelper.GetAsset<LocalizationSetting>();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            OdinMenuTree tree = new OdinMenuTree();
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.Config.DrawSearchToolbar = true;

            foreach (var family in localizationSetting.LocalizationDatas)
            {
                tree.Add($"{family.Key.ToString()}", new LocalizationEntryCreator() { LocalizationFamily = family.Key });

                foreach (var entry in family.Value.Entries)
                {
                    entry.LocalizationFamily = family.Key;
                    tree.Add(family.Key.ToString() + "/" + entry.LocalizationKey, entry);
                }
            }

            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            OdinMenuTreeSelection selection = this.MenuTree.Selection;
            OdinMenuItem selected = selection.FirstOrDefault();

            /*if (selected == null)
                base.TrySelectMenuItemWithObject();*/

            if (selected == null)
            {
                return;
            }

            LocalizationEntryCreator newEntryCreator = selection.SelectedValue as LocalizationEntryCreator;
            if (newEntryCreator != null)
            {
                SirenixEditorGUI.Title("Ajouter une nouvelle clé", "", TextAlignment.Center, true, true);                              

                SirenixEditorGUI.BeginHorizontalToolbar();
                {
                    newEntryCreator.LocalizationEntry.LocalizationKey = EditorGUILayout.TextField("Key", newEntryCreator.LocalizationEntry.LocalizationKey, GUILayout.MinWidth(200));
                    newEntryCreator.LocalizationEntry.LocalizationFamily = newEntryCreator.LocalizationFamily;

                    /*if (SirenixEditorGUI.ToolbarButton("Add Language"))
                    {
                        newEntryCreator.LocalizationEntry.Values.Add(LocalizationLanguage.French, "");
                    }*/

                    if (SirenixEditorGUI.ToolbarButton("Ajouter"))
                    {
                        for(int j = 0; j < localizationSetting.LocalizationDatas[newEntryCreator.LocalizationFamily].Entries.Count; ++j)
                        {
                            if(localizationSetting.LocalizationDatas[newEntryCreator.LocalizationFamily].Entries[j].LocalizationKey == newEntryCreator.LocalizationEntry.LocalizationKey)
                            {
                                Debug.LogError($"Une clé {newEntryCreator.LocalizationEntry.LocalizationKey} existe déjà dans cette famille.");
                                return;
                            }
                        }

                        foreach (int i in Enum.GetValues(typeof(LocalizationLanguage)))
                        {
                            newEntryCreator.LocalizationEntry.Values.Add(string.Empty);
                        }

                        localizationSetting.LocalizationDatas[newEntryCreator.LocalizationFamily].Entries.Add(newEntryCreator.LocalizationEntry);

                        ForceMenuTreeRebuild();

                        EditorUtility.SetDirty(localizationSetting);
                        AssetDatabase.SaveAssets();

                        newEntryCreator.LocalizationEntry = new LocalizationEntry();
                    }
                }
                SirenixEditorGUI.EndHorizontalToolbar();                
            }
            else
            {
                LocalizationEntry localizationEntry = selection.SelectedValue as LocalizationEntry;
                if (localizationEntry != null)
                {                  
                    if(currentLocalizationEntry != null)
                    {
                        if (localizationEntryScrollViewValue != Vector2.zero)
                        {
                            localizationEntryScrollViewValue = Vector2.zero;
                        }
                    }
                    currentLocalizationEntry = localizationEntry;

                    SirenixEditorGUI.Title(localizationEntry.LocalizationKey, "", TextAlignment.Center, true, true);
                    SirenixEditorGUI.BeginHorizontalToolbar();
                    {
                        localizationEntry.LocalizationKey = EditorGUILayout.TextField("Editer la clé :", localizationEntry.LocalizationKey, GUILayout.MinWidth(200));

                        if (SirenixEditorGUI.ToolbarButton("Sauvegarder"))
                        {
                            ForceMenuTreeRebuild();

                            EditorUtility.SetDirty(localizationSetting);
                            AssetDatabase.SaveAssets();
                        }

                        if (SirenixEditorGUI.ToolbarButton("Supprimer"))
                        {
                            localizationSetting.LocalizationDatas[localizationEntry.LocalizationFamily].Entries.Remove(localizationEntry);

                            EditorUtility.SetDirty(localizationSetting);
                            AssetDatabase.SaveAssets();
                        }
                    }
                    SirenixEditorGUI.EndHorizontalToolbar();

                    localizationEntryScrollViewValue = EditorGUILayout.BeginScrollView(localizationEntryScrollViewValue);
                    {
                        for (int index = 0; index < localizationEntry.Values.Count; ++index)
                        {
                            string language = ((LocalizationLanguage)index).ToString();
                            localizationEntry.Values[index] = EditorGUILayout.TextField(language, localizationEntry.Values[index], GUILayout.MinWidth(200), GUILayout.MinHeight(100));
                        }
                    }
                    EditorGUILayout.EndScrollView();                    
                }
            }
            
        }

        protected override void OnEndDrawEditors()
        {
            base.OnEndDrawEditors();

        }
    }
}
#endif
