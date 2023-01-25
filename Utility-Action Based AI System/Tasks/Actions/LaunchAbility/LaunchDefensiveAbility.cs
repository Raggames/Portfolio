using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    class LaunchDefensiveAbility  : LaunchAbilityAction
    {
        public float LifeRatioMultiplier = 3f;
        public int LifeThreshold = 60;
        public float LifeUnderThresholdMultiplier = 2;
        public float InRangeTargetsMultiplier = 1.25f;
        public int TargetCountDivier = 3;

        public override int ComputePriority()
        {
            if (!Ability.IsLaunchable())
                return 0;

            int count = 0;
            for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiTargets.Count; ++i)
            {
                if (aiAgentEntity.targetHandlingSubsystem.AiTargets[i].InRange)
                {
                    count++;
                }
            }

            float targetRatio = 1 + (float)count / (float)TargetCountDivier;
            targetRatio *= InRangeTargetsMultiplier;

            float lifeRatio = Mathf.Pow((100 - aiAgentEntity.CurrentLifePurcent) / 10f, 2);
            lifeRatio *= LifeRatioMultiplier;

            if (aiAgentEntity.CurrentLifePurcent < LifeThreshold)
            {
                lifeRatio *= LifeUnderThresholdMultiplier;
            }

            return Mathf.RoundToInt(targetRatio * lifeRatio); 
        }

        public override void Execute()
        {
            aiAgentEntity.abilityController.CurrentAbility = Ability;

            // Then we actually launch the ability
            aiAgentEntity.abilityController.Server_SelectAbility(
                 aiAgentEntity.abilityController.CurrentAbility,
                 null,
                 Vector3.zero);
        }
    }
}
