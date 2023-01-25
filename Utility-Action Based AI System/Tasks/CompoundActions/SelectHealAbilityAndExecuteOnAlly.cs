using SteamAndMagic.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class SelectHealAbilityAndExecuteOnAlly : CompoundAction
    {
        public int AllyMaxRange = 0;
        public int AllyLifeThreshold = 30;
        public Entity CurrentAllyToHeal;

        public MoveToFocusAction MoveToFocusAction;
        public LaunchAbilityAction LaunchAbilityAction;

        private Action OnSelectAbilityEnded;

        public override int ComputePriority()
        {
            if (CurrentAllyToHeal == null)
            {
                float purcent = float.MaxValue;

                for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiAllies.Count; ++i)
                {
                    if (aiAgentEntity.targetHandlingSubsystem.AiAllies[i].Entity.CurrentLifePurcent <= AllyLifeThreshold && aiAgentEntity.targetHandlingSubsystem.AiAllies[i].Entity.CurrentLifePurcent < purcent)
                    {
                        CurrentAllyToHeal = aiAgentEntity.targetHandlingSubsystem.AiAllies[i].Entity;
                        purcent = CurrentAllyToHeal.CurrentLifePurcent;
                    }
                }

                if (CurrentAllyToHeal == null)
                {
                    return 0;
                }
            }

            return Mathf.RoundToInt((AllyLifeThreshold - CurrentAllyToHeal.CurrentLifePurcent) / 5f) + BasePriority;
        }

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);

            OnSelectAbilityEnded = () =>
            {
                if (aiAgentEntity.abilityController.CurrentAbility != null)
                {
                    if (aiAgentEntity.abilityController.CurrentAbility.IsFocusInRange(aiAgentEntity.abilityController.CurrentAbility.Range))
                    {
                        brain.StartAction(LaunchAbilityAction, LaunchAbilityAction.MatchInteruptionCondition, EndAction);
                    }
                    else
                    {
                        brain.StartAction(MoveToFocusAction, MoveToFocusAction.MatchInteruptionCondition, OnSelectAbilityEnded);
                    }                      
                }
                else
                {
                    EndAction();
                }
            };
        }

        public override void Execute()
        {
            SequenceIndex = 0;
            aiAgentEntity.targetHandlingSubsystem.ForceFocus(CurrentAllyToHeal);
            //brain.StartAction(SelectAbilityAction, null, OnSelectAbilityEnded);
        }
    }
}
