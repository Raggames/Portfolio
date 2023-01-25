using Sirenix.OdinInspector;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class MoveToAnyTargetRange : GenericAiAction
    {
        [Header("---- MOVE TO ANY TARGET IN RANGE ----")]
        public float MinFocusDistance = 1.5f;

        public Utility OutOfRangeAbilitiesCountUtility;
        [Header("---- RUNTIME ----")]
        [ReadOnly] public Vector3 tempFocusPosition;
        [ReadOnly] public AiTarget MoveTarget;


        public override int ComputePriority()
        {
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

            return 0;
        }
        
        public override void Execute()
        {
            tempFocusPosition = aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position;

            aiAgentEntity.aiControl.MoveTo(
                aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position,
                EndAction,
                "move to focus",
                aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform,
                aiAgentEntity.aiControl.StrafeSpeed,
                MinFocusDistance);
        }

        public override bool MatchInteruptionCondition()
        {
            return aiAgentEntity.targetHandlingSubsystem.Focus == null
                   || aiAgentEntity.targetHandlingSubsystem.Focus.Entity.IsDead
                   || Vector3.Distance(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position, tempFocusPosition) > 1f
                   || !aiAgentEntity.aiControl.IsMoving;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
