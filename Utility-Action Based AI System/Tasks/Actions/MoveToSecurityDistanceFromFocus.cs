using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class MoveToSecurityDistanceFromFocus : GenericAiAction
    {
        public float DistanceThreshold = 3;
        public float SecurityDistanceFromFocus = 5;
        public float RandomArea = 2.5f;

        public float LifeMultiplier = 2f;
        public float DistanceMultiplier = 10;

        public Vector3 CurrentPosition;
        public float CurrentDistanceFromFocus = 0;

        [ReadOnly] public Entity toAvoid; 

        public override int ComputePriority()
        {
            if (!aiAgentEntity.CanDoMovement)
                return 0;

            if (aiAgentEntity.targetHandlingSubsystem.HasFocus)
            {
                toAvoid = aiAgentEntity.targetHandlingSubsystem.Focus.Entity;

                CurrentDistanceFromFocus = aiAgentEntity.targetHandlingSubsystem.DistanceFromFocus();
                if (CurrentDistanceFromFocus < DistanceThreshold)
                {
                    float lifeDiff = 100f - aiAgentEntity.CurrentLifePurcent;

                    float distDiff = DistanceThreshold - CurrentDistanceFromFocus;
                    return Mathf.RoundToInt(Mathf.Pow(distDiff, 2) * DistanceMultiplier) + Mathf.RoundToInt(lifeDiff * LifeMultiplier);
                }
            }
            // Else in security from other entities? 

            CurrentDistanceFromFocus = -1;

            return 0;
        }

        public override void Execute()
        {
            Vector3 dir = aiAgentEntity.transform.position - aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position;
            if (dir.magnitude < 2)
            {
                CurrentPosition = NavigationHelper.RandomNavmeshLocation(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position, RandomArea);
            }
            else
            {
                Vector3 aim = dir.normalized * SecurityDistanceFromFocus + aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position;
                CurrentPosition = NavigationHelper.RandomNavmeshLocation(aim, RandomArea);
            }

            aiAgentEntity.aiControl.MoveTo(
               CurrentPosition,
               EndAction,
               "move to security distance",
               aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
               aiAgentEntity.aiControl.StrafeSpeed,
               aiAgentEntity.aiControl.StoppingDistance);
        }

        public override bool MatchInteruptionCondition()
        {
            return !aiAgentEntity.targetHandlingSubsystem.HasFocus
                || aiAgentEntity.targetHandlingSubsystem.DistanceFromFocus() > SecurityDistanceFromFocus
                || !aiAgentEntity.aiControl.IsMoving;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
