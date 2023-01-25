using Sirenix.OdinInspector;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using System.Collections;
using System.Collections.Generic;
using Systems.Abilities.Interfaces;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class DoublePeine : MeleeAbility, IHitboxAbility
    {
        [Header("---- DOUBLE PEINE ----")]
        public HitBox Hitbox_Prefab;
        public float KnockbackForce = 1;

        public HitBox HitBox_prefab { get => Hitbox_Prefab; }
        public HitBox CurrentHitBox { get; set; }

        [Header("---- RUNTIME ----")]
        [ShowInInspector, ReadOnly] private int attackIndex = 0;

        public override void Init(AbilityController skillbookComp, int skIndex, Entity owner)
        {
            base.Init(skillbookComp, skIndex, owner);

            if (CurrentHitBox == null)
            {
                (this as IHitboxAbility).CreateHitBox(owner, this, HitBox_prefab, this.owner.transform, rightHand);
                CurrentHitBox.SetDimensions(new Vector3(EffectRange, 5, EffectRange));
            }
        }

        protected override IEnumerator VFXLoop()
        {
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[4]);

            owner.PlaySoundRequest(SFXs[0]);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[5]);

            owner.PlaySoundRequest(SFXs[1]);
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            attackIndex = 0;
            ownerCharacter.playerControl.IsRootMotion = true;

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            CurrentHitBox.StartListenning(this); // Le trigger devient actif
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
            CurrentHitBox.EndListenning();

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[2]);

            attackIndex = 1;

            ownerCharacter.characterAnimationSystem.ContinueCombo(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[1]);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[3]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            CurrentHitBox.StartListenning(this); // Le trigger devient actif
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
            CurrentHitBox.EndListenning();

            yield return waitForEndTime;

            EndAbility(AbilityEndMode.Classic);
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);
            ownerCharacter.playerControl.IsRootMotion = false;
        }

        /// <summary>
        /// Callback de la hitbox lorsqu'elle touche
        /// </summary>
        /// <param name="pObj"></param>
        /// <param name="entity"></param>
        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner || entity.Team == owner.Team)
                return;
                     
            this.PlayWeaponHitSoundRequest(entity);
            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], entity.chestTransform.position, null);

            if (attackIndex == 0)
            {
                ApplyDamageAndEffectsTo(entity, Damages, Effects);
            }
            else
            {
                if(entity.effectSystem != null && entity.effectSystem.HasDebuffsOfClass(EffectClass.Commotion, owner, false))
                {
                    ApplyDamageAndEffectsTo(entity, Damages, Effects);
                    entity.entityControl.KnockBack(entity.transform.position - owner.transform.position, KnockbackForce);
                }
                else
                {
                    ApplyDamageAndEffectsTo(entity, Damages, null);
                }
            }
        }
    }
}
