using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class MoveInSightSpotForAbility : GenericAiAction
    {
        public Utility NumberOfOutSightAbilitiesUtility;
        public Utility CurrentChosenAbilityUtility;

        [ReadOnly] public LaunchAbilityAction CurrentChosenAbility;
        [ReadOnly] public AiTarget CurrentChosenAbilityTarget;
        [ReadOnly] public Vector3 tempDestination;

        private List<LaunchAbilityAction> tempActions = new List<LaunchAbilityAction>();
        private List<float> tempActionsPriorities = new List<float>();
        private List<Utility> utilities;

        private void Awake()
        {
            utilities = new List<Utility>() { NumberOfOutSightAbilitiesUtility, CurrentChosenAbilityUtility };
        }

        public override int ComputePriority()
        {
            if (!brain.IsFighting)
                return 0;

            tempActions.Clear();
            tempActionsPriorities.Clear();

            int totalAbilityAction = 0;
            for (int i = 0; i < brain.GenericAiActions.Count; ++i)
            {
                if (brain.GenericAiActions[i] is LaunchAbilityAction)
                {
                    LaunchAbilityAction action = brain.GenericAiActions[i] as LaunchAbilityAction;

                    if (action.IsCurrentTargetOutOfSight)
                    {
                        tempActions.Add(action);
                        tempActionsPriorities.Add(action.CurrentPriority);
                    }
                    else
                    {
                        totalAbilityAction++;
                    }
                }
            }

            if (tempActions.Count > 0)
            {
                // On choisit une des compétences indisponibles pour cause de Out Of Sight
                int index = Randomizer.Index(tempActionsPriorities);
                CurrentChosenAbility = tempActions[index];
                CurrentChosenAbilityTarget = CurrentChosenAbility.CurrentAiTarget;

                // On merge le nombre de compétence out of sight par rapport au nombre total de compétence pour intégrer la notion de "plus j'ai de compétences indisponibles pour cette raison, plus je devrais me déplacer à un spot in sight"
                NumberOfOutSightAbilitiesUtility.ComputeResult(tempActions.Count, totalAbilityAction);
                CurrentChosenAbilityUtility.ComputeResult(CurrentChosenAbility.CurrentPriority, 100f); // on intègre aussi la priorité de l'action de lancé de compétence à une utility

                return Mathf.RoundToInt(OptionEvaluator.GetAverage(utilities) * 100f);
            }
            else
            {
                return 0;
            }
        }

        public override void Execute()
        {
            Debug.Log("Go to in sight of " + CurrentChosenAbilityTarget.Entity);
            Vector3 moveSpot = Vector3.zero;

            if (NavigationHelper.FindInSightSpot(out moveSpot, aiAgentEntity.aiControl.navMeshAgent, CurrentChosenAbilityTarget.Entity.transform, (int)aiAgentEntity.targetHandlingSubsystem.VisionRange, 2, LayerMask.GetMask("Walls", "Ground"), true))
            {
                tempDestination = moveSpot;

                aiAgentEntity.aiControl.MoveTo(
                    moveSpot,
                    EndAction,
                    "move to in sight spot",
                    aiAgentEntity.targetHandlingSubsystem.HasFocus ? aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform : null,
                    aiAgentEntity.aiControl.StrafeSpeed,
                    0);
            }

        }

        public override bool MatchInteruptionCondition()
        {
            return CurrentChosenAbilityTarget.Entity == null
                    || CurrentChosenAbilityTarget.Entity.IsDead
                    || CurrentChosenAbilityTarget.InSight
                    || !aiAgentEntity.aiControl.IsMoving;
        }

        protected override void EndAction()
        {
            base.EndAction();

            CurrentChosenAbility = null;
            CurrentChosenAbilityTarget = null;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            CurrentChosenAbility = null;
            CurrentChosenAbilityTarget = null;

            aiAgentEntity.aiControl.Stop();
        }
    }
}
