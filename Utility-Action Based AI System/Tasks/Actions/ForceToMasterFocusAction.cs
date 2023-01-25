using SteamAndMagic.Entities;
using System;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class ForceToMasterFocusAction : GenericAiAction
    {
        public Entity SlaveMaster;

        public override void Execute()
        {
            AiTarget aiTarget = aiAgentEntity.targetHandlingSubsystem.AiTargets.FindItem(t => t.Entity == aiAgentEntity.CurrentEntityTarget);

            if (aiTarget != null)
            {
                aiAgentEntity.targetHandlingSubsystem.Focus = aiTarget;

                if (!brain.IsFighting)
                {
                    aiAgentEntity.AiAgentEngageEnemyRequest(aiAgentEntity.targetHandlingSubsystem.Focus.Entity);
                }
            }

            EndAction();
        }

        public override void DisableAction()
        {
        }

        public override void EnableAction()
        {
        }
    }
}
