using UnityEngine;
using System.Collections.Generic;

namespace SteamAndMagic.Systems.IA
{
    public class MoveToFocusAction : GenericAiAction
    {
        public Vector3 tempFocusPosition;
        public float MinFocusDistance = 1.5f;
        public float MaxFocusDistance = 6f;
        // Override threshol 100

        public Utility FocusDistanceUtility;
        public Utility OutOfRangeAbilitiesCountUtility;
        private List<Utility> utilities;

        private void Awake()
        {
            utilities = new List<Utility>() { FocusDistanceUtility, OutOfRangeAbilitiesCountUtility };
        }

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);
        }

        public override int ComputePriority()
        {
            CurrentPriority = 0;

            if (!aiAgentEntity.CanDoMovement)
                return 0;

            if (!aiAgentEntity.targetHandlingSubsystem.HasFocus)
            {
                return 0;
            }

            float distance = Vector3.Distance(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position, aiAgentEntity.transform.position);
            if(distance > MinFocusDistance)
            {
                FocusDistanceUtility.ComputeResult(distance, aiAgentEntity.targetHandlingSubsystem.VisionRange);

                // Calculer les compétences hors loss
                int totalOutOfRangeAbilities = 0;
                int totalOutOfRangeEntities = 0;
                for (int i = 0; i < brain.GenericAiActions.Count; ++i)
                {
                    if (brain.GenericAiActions[i] is LaunchAbilityAction)
                    {
                        LaunchAbilityAction action = brain.GenericAiActions[i] as LaunchAbilityAction;
                        if (action.IsOutOfAnyAiTargetRange)
                        {
                            totalOutOfRangeEntities += action.OutOfRangeTargetsCount;
                            totalOutOfRangeAbilities++;
                        }
                    }
                }

                // Comptage du nombre de cibles hors de portée par compétence sur le total de compétence * le total de cible
                // Si toute les cibles sont hors de portée pour toutes les compétences, OutOfRangeAbilitiesCOuntUtility renverra 1
                OutOfRangeAbilitiesCountUtility.ComputeResult(totalOutOfRangeEntities, aiAgentEntity.targetHandlingSubsystem.AiTargets.Count * totalOutOfRangeAbilities);

                return Mathf.RoundToInt(OptionEvaluator.GetAverage(utilities) * 100f);
            }

            return 0;
        }

        public override void Execute()
        {
            tempFocusPosition = aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position;

            aiAgentEntity.aiControl.MoveTo(
                aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                EndAction,
                "move to focus",
                aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                aiAgentEntity.aiControl.StrafeSpeed,
                MinFocusDistance);

            /*aiAgentEntity.aiControl.FollowFocus(
                            aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                            "follow focus",
                            EndAction, // Follow Focus will end when the Agent arrived at stopping distance for the first time. 
                            aiAgentEntity.aiControl.StrafeSpeed,
                            MinFocusDistance);*/
        }

        public override bool MatchInteruptionCondition()
        {
            return aiAgentEntity.targetHandlingSubsystem.Focus == null
                   || aiAgentEntity.targetHandlingSubsystem.Focus.Entity.IsDead
                   //|| Vector3.Distance(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position, tempFocusPosition) > 1f
                   || !aiAgentEntity.aiControl.IsMoving;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
