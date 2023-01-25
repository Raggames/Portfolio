using System;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class SelectFirstLaunchableAbilityAction : GenericAiAction
    {
        public override void Execute()
        {
            for (int i = 0; i < aiAgentEntity.abilityController.Abilities.Length; ++i)
            {
                if (aiAgentEntity.abilityController.Abilities[i] != null
                    && aiAgentEntity.abilityController.Abilities[i].IsLaunchable())
                {
                    aiAgentEntity.abilityController.CurrentAbility = aiAgentEntity.abilityController.Abilities[i];
                    break;
                }
            }

            EndAction();
        }

    }
}
