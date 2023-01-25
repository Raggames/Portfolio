using SteamAndMagic.Systems.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems
{
    public class DynamicStringReader
    {

        /// <summary>
        /// Génère dynamiquement une description en fonction des données de localisation et en détectant les variables via le caractère '$'.
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static string ReadText(string inputText, object target, int autoLineBreak = -1)
        {
            string result = "";
            int autolineBreakCounter = 0;
            bool inMarkup = false;
            bool breakOnNextSpace = false;
            //int nextLineBreakIndex = inputText.IndexOf("\n");

            for (int i = 0; i < inputText.Length; i++)
            {                
                if (inputText[i] == '<')
                {
                    inMarkup = true;
                }
                else if (inputText[i] == '>')
                {
                    inMarkup = false;
                }

                if (inputText[i] == ' ' && breakOnNextSpace)
                {
                    result += "<br>";
                    autolineBreakCounter = 0;
                    breakOnNextSpace = false;
                }

                if (inputText[i] != '$')
                {
                    result += inputText[i];

                    if (!inMarkup)
                        autolineBreakCounter++;
                }
                else
                {
                    int indexOffset = 0;
                    result += ReadDynamicValue(target, inputText, i + 1, autoLineBreak, out indexOffset);
                    i += indexOffset;

                    if (!inMarkup)
                        autolineBreakCounter += indexOffset;
                }

              /*  if (i >= nextLineBreakIndex)
                {
                    nextLineBreakIndex = inputText.IndexOf("\n");
                    autolineBreakCounter = 0;
                }
               
                if (autoLineBreak != -1 && autolineBreakCounter > autoLineBreak)
                {
                    breakOnNextSpace = true;
                }*/
            }

            return result;
        }

        public static string ReadDynamicValue(object target, string input, int startIndex, int autolinebreak, out int rodeLenght)
        {
            int readIndex = 0;
            string readField = "";

            for (int i = startIndex; i < input.Length; ++i)
            {
                if (input[i] == '$')
                {
                    readIndex++;
                    break;
                }

                readField += input[i];
                readIndex++;
            }

            string result = "";

            Type type = target.GetType();
            FieldInfo[] talentFields = type.GetFields();

            for (int i = 0; i < talentFields.Length; ++i)
            {
                if (talentFields[i].Name == readField)
                {
                    result = "<color=green>" + talentFields[i].GetValue(target).ToString() + "</color>";
                    break;
                }
                else if (readField.Contains(talentFields[i].Name))
                {
                    if (talentFields[i].FieldType == typeof(List<Damage>))
                    {
                        var substringBefore = readField.Substring(0, readField.IndexOf('['));

                        if (substringBefore == talentFields[i].Name)
                        {
                            int idx = ReadCollectionFieldIndex(readField);
                            var collection = talentFields[i].GetValue(target) as List<Damage>;
                            result = "<color=green> " + Mathf.Abs(collection[idx].value).ToString() + "%</color><color=purple> de Dégâts d'Arme</color>" + " <color=red>(" + collection[idx].damageNature.ToString() + ")</color>";
                        }
                        else
                        {
                            Debug.Log("Read field : " + readField + " //  Sub string : " + substringBefore + " // Talent field name : " + talentFields[i].Name);
                        }
                    }
                    else if (talentFields[i].FieldType == typeof(List<Effect>))
                    {
                        int idx = ReadCollectionFieldIndex(readField);
                        var collection = talentFields[i].GetValue(target) as List<Effect>;

                        var substringBefore = readField.Substring(0, readField.IndexOf('['));

                        if (!readField.Contains("_"))
                        {
                            if (substringBefore == talentFields[i].Name)
                            {
                                result = "<color=purple>" + collection[idx].effectName + "</color>";
                                break;
                            }
                            else
                            {
                                Debug.Log("Read field : " + readField + " //  Sub string : " + substringBefore + " // Talent field name : " + talentFields[i].Name);
                            }
                        }
                        else
                        {
                            if (substringBefore == talentFields[i].Name)
                            {
                                if (readField.Contains("_Description"))
                                {
                                    result = "<color=purple><b><u>" + collection[idx].effectName + " : </b></u></color> <br><i>" + ReadText(collection[idx].effectDescription, collection[idx], autolinebreak) + "</i>";
                                    break;
                                }
                                else if (readField.Contains("_Duration"))
                                {
                                    result = "<color=green>" + collection[idx].Duration.ToString() + "</color>";
                                    break;
                                }
                            }
                            else
                            {
                                Debug.Log("Read field : " + readField + " //  Sub string : " + substringBefore + " // Talent field name : " + talentFields[i].Name);
                            }
                        }
                    }
                    else if (talentFields[i].FieldType == typeof(List<AttributeModifier>))
                    {
                        result = ReadAttributeModifier(target, readField, talentFields[i]);
                        break;
                    }
                }
            }

            rodeLenght = readIndex;
            return result;
        }

        private static string ReadAttributeModifier(object target, string readField, FieldInfo talentField)
        {
            string result;
            int idx = ReadCollectionFieldIndex(readField);
            var collection = talentField.GetValue(target) as List<AttributeModifier>;

            if (collection[idx].value < 0)
            {
                result = "réduit le ";
            }
            else
            {
                result = "augmente le ";
            }

            result += "<color=blue>" + collection[idx].target + " " + collection[idx].stat + "</color> de <color=green>" + Mathf.Abs(collection[idx].value) + "</color>";

            if (collection[idx].mode == ModifierMode.Purcentage)
            {
                result += "<color=green> %</color>";
            }

            return result;
        }

        private static int ReadCollectionFieldIndex(string input)
        {
            bool read = false;
            string val = "";

            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == '[')
                {
                    read = true;
                    continue;
                }

                if (input[i] == ']')
                {
                    read = false;
                    break;
                }

                if (read)
                    val += input[i];
            }

            return Int32.Parse(val);
        }


        private static List<char> AuthorizedChars = new List<char>()
        {
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','0','1','2','3','4','5','6','7','8','9',
        };

        public static List<char> GetForbiddenCharacters(string inputText)
        {
            List<char> unauthorizedChars = new List<char>();

            for (int i = 0; i < inputText.Length; i++)
            {
                if (AuthorizedChars.Contains(inputText[i]))
                    unauthorizedChars.Add(inputText[i]);
            }
            return unauthorizedChars;
        }
    }
}
