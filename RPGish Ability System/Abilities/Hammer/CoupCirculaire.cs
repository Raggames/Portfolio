using RPGCharacterAnims;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using SteamAndMagic.Systems.DamagePhysics;
using System.Collections;
using System.Collections.Generic;
using Systems.Abilities.Interfaces;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    /*A simple cleave attack that deals 150% of WD (Sharp) to the enemies in a cone of 180° and 3 meters in front of the launcher. */
    public class CoupCirculaire : MeleeAbility, IHitboxAbility
    {
        [Header("Metal Strike")]
        public int EffectProcPurcent = 10;

        public HitBox HitBox_prefab { get => hitbox_Prefab; }
        [SerializeField] private HitBox hitbox_Prefab;

        public HitBox CurrentHitBox { get; set; }

        public override void Init(AbilityController skillbookComp, int skIndex, Entity owner)
        {
            base.Init(skillbookComp, skIndex, owner);

            if (CurrentHitBox == null)
            {
                (this as IHitboxAbility).CreateHitBox(owner, this, HitBox_prefab, this.owner.transform, rightHand);
                CurrentHitBox.SetDimensions(new Vector3(EffectRange, 5, EffectRange));
            }
        }

        protected override IEnumerator SkillLoop(Vector3 position)
        {
            //ownerCharacter.playerControl.IsRootMotion = true;

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            CurrentHitBox.StartListenning(this); // Le trigger devient actif
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
            CurrentHitBox.EndListenning();

            EndAbility(AbilityEndMode.Classic);
            //ownerCharacter.playerControl.IsRootMotion = false;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            //ownerCharacter.playerControl.IsRootMotion = false;
        }

        protected override IEnumerator VFXLoop()
        {
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[2]);
            owner.ExecuteVFXRequest(this, VFXs[0], null); // (fx) => fx.transform.rotation = owner.chestTransform.rotation
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

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            //entity.entityControl?.KnockBack(entity.transform.position - owner.transform.position, KnockBackForce);

            this.PlayWeaponHitSoundRequest(entity);
            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], entity.chestTransform.position, null);

            ApplyDamageAndEffectsTo(entity, Damages, Effects);
        }

        protected override void OnDetectWorld(PhysicsObject pObj)
        {
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.playerControl.IsRootMotion = true;

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
            /*switch (ownerCharacter.characterAnimationSystem.CurrentMainWeapon)
            {
                case Weapon.TwoHandSword:
                case Weapon.TwoHandSpear:
                case Weapon.TwoHandAxe:
                case Weapon.TwoHandStaff:
                    break;
                case Weapon.RightSword:
                case Weapon.RightMace:
                case Weapon.RightDagger:
                case Weapon.RightItem:
                case Weapon.RightSpear:
                    ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0], false, Side.Right, false, false);
                    break;
            }*/
        }

        public override void EndAnimationAction()
        {
            base.EndAnimationAction();
            ownerCharacter.playerControl.IsRootMotion = false;
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);

            ownerCharacter.playerControl.IsRootMotion = false;
        }
    }
}
