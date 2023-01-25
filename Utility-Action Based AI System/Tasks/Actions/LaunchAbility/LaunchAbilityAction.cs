using SteamAndMagic.Entities;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using SteamAndMagic.Systems.Abilities;

namespace SteamAndMagic.Systems.IA
{
    public class LaunchAbilityAction : GenericAiAction
    {
        public Ability Ability;

        [ReadOnly] public AiTarget CurrentAiTarget;

        #region Accessors
        /// <summary>
        /// Doit retourner vrai si aucune cible n'est disponible pour cette compétence
        /// </summary>
        public virtual bool IsOutOfAnyAiTargetRange
        {
            get
            {
                return false;
            }
        }

        public virtual int OutOfRangeTargetsCount
        {
            get
            {
                return 0;
            }
        }

        public virtual bool IsCurrentTargetOutOfSight
        {
            get
            {
                if(CurrentAiTarget == null)
                {
                    return false;
                }

                return  CurrentAiTarget.InRange && !CurrentAiTarget.InSight;
            }
        }
        #endregion

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);

            for (int i = 0; i < context.abilityController.Abilities.Length; ++i)
            {
                if (context.abilityController.Abilities[i] != null && context.abilityController.Abilities[i].corekey == Ability.corekey)
                {
                    Ability = context.abilityController.Abilities[i] as AiAbility;
                    return;
                }
            }

            Debug.LogError("Cannot bind " + Ability.corekey);
        }

        public override int ComputePriority()
        {
            return 0;
        }

        public override void Execute()
        {
            aiAgentEntity.targetHandlingSubsystem.ForceFocus(CurrentAiTarget.Entity);
            aiAgentEntity.aiControl.FixLookAt(aiAgentEntity.targetHandlingSubsystem.Focus.Entity.transform.position);
            aiAgentEntity.abilityController.CurrentAbility = Ability;

            // Then we actually launch the ability
            aiAgentEntity.abilityController.Server_SelectAbility(
                 aiAgentEntity.abilityController.CurrentAbility,
                 aiAgentEntity.targetHandlingSubsystem.Focus.Entity,
                 Vector3.zero);
        }

        public override void EnableAction()
        {
            Ability.OnAbilityCancel += Ability_OnAbilityCancel;
            Ability.OnAbilityEnd += Ability_OnAbilityEnd;
            Ability.OnAbilityInterrupt += Ability_OnAbilityInterrupt;
        }

        public override void DisableAction()
        {
            Ability.OnAbilityCancel -= Ability_OnAbilityCancel;
            Ability.OnAbilityEnd -= Ability_OnAbilityEnd;
            Ability.OnAbilityInterrupt -= Ability_OnAbilityInterrupt;
        }

        private void Ability_OnAbilityInterrupt(Entity launcher, Ability ability) => AbilityPendingEnd(launcher, ability);

        private void Ability_OnAbilityCancel(Entity launcher, Ability ability) => AbilityPendingEnd(launcher, ability);

        private void Ability_OnAbilityEnd(Entity launcher, Ability ability) => AbilityPendingEnd(launcher, ability);

        private void AbilityPendingEnd(Entity launcher, Ability ability)
        {
            if (brain != null
                && brain.CurrentAction == this
                && launcher == aiAgentEntity
                && ability == Ability)
            {
                EndAction();
            }           
        }
    }
}
