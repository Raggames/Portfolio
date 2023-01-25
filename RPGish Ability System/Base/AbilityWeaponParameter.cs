using RPGCharacterAnims;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    /// <summary>
    /// Classe pour définir des propriétés liées aux armes dans les compétences (trigger d'anim, temps d'attente, etc...
    /// </summary>
    [Serializable]
    public class AbilityWeaponParameter
    {
        public Weapon weapon;

        public int StaticCastTime;
        public int MovingCastTime;

        [HorizontalGroup("X")] [ListDrawerSettings(CustomAddFunction = "OnAddAnim")] public List<int> animTriggers = new List<int>();
        [HorizontalGroup("X")] [ListDrawerSettings(CustomAddFunction = "OnAddAnim")] public List<string> animTriggersComments = new List<string>();

        [HorizontalGroup("Y")] [ListDrawerSettings(CustomAddFunction = "OnAddYield")] public List<float> yieldTimes = new List<float>();
        [HorizontalGroup("Y")] [ListDrawerSettings(CustomAddFunction = "OnAddYield")] public List<string> yieldTimesComments = new List<string>();

        public List<GameObject> gameObjects = new List<GameObject>();

        private void OnAddYield()
        {
            yieldTimes.Add(0);
            yieldTimesComments.Add("");
        }

        private void OnAddAnim()
        {
            animTriggers.Add(-1);
            animTriggersComments.Add("");
        }
    }
}
