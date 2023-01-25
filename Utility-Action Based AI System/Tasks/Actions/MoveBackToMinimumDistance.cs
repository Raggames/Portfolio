using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class MoveBackToMinimumDistance : GenericAiAction
    {
        public float MinimumDistance = 2;
        private Vector3 tempTargetPosition;

        public override int ComputePriority()
        {
            CurrentPriority = 0;

            if (!aiAgentEntity.CanDoMovement)
                return 0;

            if (!aiAgentEntity.targetHandlingSubsystem.HasFocus)
            {
                return 0;
            }

            Vector3 direction = aiAgentEntity.transform.position - aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position;
            tempTargetPosition = aiAgentEntity.transform.position + direction.normalized * MinimumDistance;

            float distance = Vector3.SqrMagnitude(direction);
            float ratio = 1 - distance / (MinimumDistance * MinimumDistance);

            return Mathf.RoundToInt(ratio * 100f);
        }

        public override void Execute()
        {
            aiAgentEntity.aiControl.MoveTo(
                tempTargetPosition,
                EndAction,
                "move to minimum distance",
                aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                aiAgentEntity.aiControl.StrafeSpeed,
                0);
        }

        public override bool MatchInteruptionCondition()
        {
            return aiAgentEntity.targetHandlingSubsystem.Focus == null
                   || aiAgentEntity.targetHandlingSubsystem.Focus.Entity.IsDead
                   || !aiAgentEntity.aiControl.IsMoving;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
