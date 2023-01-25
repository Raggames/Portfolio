using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class FollowFocusAction : GenericAiAction
    {
        public float FollowStoppingDistance = 0;

        public FollowFocusAction(float followStoppingDistance)
        {
            FollowStoppingDistance = followStoppingDistance;
        }

        public override void Execute()
        {
            aiAgentEntity.aiControl.FollowFocus(
                            aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                            "follow focus",
                            EndAction, // Follow Focus will end when the Agent arrived at stopping distance for the first time. 
                            aiAgentEntity.aiControl.Speed);
        }

        public override bool MatchInteruptionCondition()
        {
            return aiAgentEntity.targetHandlingSubsystem.Focus == null 
                || aiAgentEntity.targetHandlingSubsystem.Focus.Entity.IsDead 
                || !aiAgentEntity.targetHandlingSubsystem.Focus.InRange;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
