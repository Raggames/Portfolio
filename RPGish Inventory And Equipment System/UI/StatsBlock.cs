using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Attributes;
using System;
using TMPro;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class StatsBlock : UIBlock
    {
        public Character Character;

        public Transform CharacterStat;
        public Transform ArmorStat;
        public Transform SecondaryStats;

        public UIGameAttributeSlot UIGameAttributeSlot;

        public void Init(Character character)
        {
            Character = character;

            for(int i = 0; i < character.attributeSubsystem.Attributes.Count; ++i)
            {
                int statIndex = (int)character.attributeSubsystem.Attributes[i].stat;
                UIGameAttributeSlot uIGameAttributeSlot = null;
                if (statIndex < 10)
                {
                    uIGameAttributeSlot = Instantiate(UIGameAttributeSlot, CharacterStat);
                }
                else if(statIndex >= 10 && statIndex < 20)
                {
                    uIGameAttributeSlot = Instantiate(UIGameAttributeSlot, ArmorStat);
                }
                else
                {
                    uIGameAttributeSlot = Instantiate(UIGameAttributeSlot, SecondaryStats);
                }

                uIGameAttributeSlot.Initialize(character.attributeSubsystem.Attributes[i]);
            }
        }        
    }
}
