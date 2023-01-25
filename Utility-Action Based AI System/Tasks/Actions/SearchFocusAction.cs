using System.Collections;
using UnityEngine;
using System;

namespace SteamAndMagic.Systems.IA
{
    public class SearchFocusAction : GenericAiAction
    {
        public int CanSearchPriority = 50;

        public float MaxFocusDistance = 5;
        public float MinFocusDistance = 1.5f;
        public float CantSearchCooldownTime = 10f;
        public float MaxDistanceBeforeRandomSearch = 8;
        public float SearchDistanceAroundFocusLastKnownPosition = 4;

        private Coroutine SearchTimoutRoutine;
        private Coroutine CanSearchCooldownRoutine;

        public SearchFocusAction(float maxFocusDistance, float minFocusDistance, float cantSearchCooldownTime, float maxDistanceBeforeRandomSearch, float searchDistanceAroundFocusLastKnownPosition)
        {
            MaxFocusDistance = maxFocusDistance;
            MinFocusDistance = minFocusDistance;
            CantSearchCooldownTime = cantSearchCooldownTime;
            MaxDistanceBeforeRandomSearch = maxDistanceBeforeRandomSearch;
            SearchDistanceAroundFocusLastKnownPosition = searchDistanceAroundFocusLastKnownPosition;
        }

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);

            brain.CanSearchTarget = true;
        }

        public override int ComputePriority()
        {
            CurrentPriority = 0;

            if (!aiAgentEntity.CanDoMovement)
                return 0;

            if (brain.CanSearchTarget && aiAgentEntity.targetHandlingSubsystem.HasFocus)
            {
                CurrentPriority = CanSearchPriority;
            }

            return CurrentPriority;
        }

        public override void Execute()
        {
            SearchTimoutRoutine = brain.StartCoroutine(SearchTimeout());

            if (Vector3.Distance(aiAgentEntity.transform.position, aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position) <= MaxDistanceBeforeRandomSearch)
            {
                aiAgentEntity.aiControl.MoveTo(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position,
                    () =>
                    {
                        if (SearchTimoutRoutine != null)
                        {
                            brain.StopCoroutine(SearchTimoutRoutine);
                        }
                        EndAction();
                    },
                    "search focus",
                    null,
                    aiAgentEntity.aiControl.Speed);
            }
            else
            {
                if (PathComputerSubsystem.FindRandomGroundPosition(aiAgentEntity.targetHandlingSubsystem.Focus.LastKnownPosition, SearchDistanceAroundFocusLastKnownPosition, LayerMask.GetMask("Ground", "Walls", "Entities"), LayerMask.NameToLayer("Ground"), 30, out Vector3 result))
                {
                    aiAgentEntity.aiControl.MoveTo(result,
                        () =>
                        {
                            if (SearchTimoutRoutine != null)
                                brain.StopCoroutine(SearchTimoutRoutine);

                            EndAction();
                        },
                        "search focus",
                        null,
                        aiAgentEntity.aiControl.Speed);
                }
            }
        }

        public override bool MatchInteruptionCondition()
        {
            return aiAgentEntity.targetHandlingSubsystem.Focus == null
                   || aiAgentEntity.targetHandlingSubsystem.Focus.Entity == null
                   || aiAgentEntity.targetHandlingSubsystem.Focus.Entity.IsDead
                   || aiAgentEntity.targetHandlingSubsystem.Focus.InSight;
        }

        private IEnumerator SearchTimeout()
        {
            yield return new WaitForSeconds(10);

            if (brain.CurrentAction is SearchFocusAction)
            {
                brain.InterruptCurrentAction();
                brain.CanSearchTarget = false;

                CanSearchCooldownRoutine = brain.StartCoroutine(CanSearchCooldown());
            }
        }

        private IEnumerator CanSearchCooldown()
        {
            yield return new WaitForSeconds(CantSearchCooldownTime);

            brain.CanSearchTarget = true;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            if (SearchTimoutRoutine != null)
            {
                brain.StopCoroutine(SearchTimoutRoutine);
            }

            aiAgentEntity.aiControl.Stop();
        }
    }
}
