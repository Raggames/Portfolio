using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.IA
{
    class LaunchMeleeDirectionnalAbility : LaunchMeleeAbility
    {
        public override void Execute()
        {
            aiAgentEntity.targetHandlingSubsystem.ForceFocus(CurrentAiTarget.Entity);
            aiAgentEntity.aiControl.FixLookAt(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position);
            aiAgentEntity.abilityController.CurrentAbility = Ability;

            // Then we actually launch the ability
            aiAgentEntity.abilityController.Server_SelectAbility(
                 aiAgentEntity.abilityController.CurrentAbility,
                 null,
                 aiAgentEntity.targetHandlingSubsystem.Focus.Entity.chestTransform.position);
        }
    }
}
