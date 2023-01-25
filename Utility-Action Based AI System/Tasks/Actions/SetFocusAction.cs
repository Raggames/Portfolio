using System;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class SetFocusAction : GenericAiAction
    {
        public Utility CurrentLifeUtility;

        public Utility AiTargetThreathenUtility;
        public Utility AiTargetDistanceUtility;

        public override int ComputePriority()
        {
            if (brain.IsFighting && !aiAgentEntity.targetHandlingSubsystem.HasFocus)
            {
                return Mathf.RoundToInt(CurrentLifeUtility.ComputeResult(aiAgentEntity.CurrentLifePurcent, 100) * 100f);
            }

            return 0;
        }

        public override void Execute()
        {
            if (aiAgentEntity.targetHandlingSubsystem.AiTargets.Count > 1)
                aiAgentEntity.targetHandlingSubsystem.AiTargets.Sort((a, b) => Vector3.Distance(a.Entity.transform.position, aiAgentEntity.transform.position).CompareTo(Vector3.Distance(b.Entity.transform.position, aiAgentEntity.transform.position)));

            if (aiAgentEntity.targetHandlingSubsystem.AiTargets.Count > 0)
            {
                aiAgentEntity.targetHandlingSubsystem.Focus = aiAgentEntity.targetHandlingSubsystem.AiTargets[0];
            }
            else
            {
                aiAgentEntity.targetHandlingSubsystem.Focus = null;
            }

            if (!brain.IsFighting && aiAgentEntity.targetHandlingSubsystem.Focus != null)
            {
                aiAgentEntity.AiAgentEngageEnemyRequest(aiAgentEntity.targetHandlingSubsystem.Focus.Entity);
            }

            EndAction();
        }

    }
}
