using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using System.Collections;
using Systems.Abilities.Interfaces;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class ChocDeFlammes : MeleeAbility, IHitboxAbility
    {
        public float KnockbackForce = .5f;
        //public HitBox HitBox;

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
        public override bool IsLaunchable()
        {
            return ownerCharacter.characterAnimationSystem.CanDoArmedAction
                && base.IsLaunchable();
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.playerControl.IsRootMotion = true;
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        protected override IEnumerator SkillLoop(Vector3 position)
        {
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentWeapon).yieldTimes[0]);

            LayerMask mask = LayerMask.GetMask("Entities");

            Collider[] colliders = Physics.OverlapSphere(owner.transform.position, EffectRange, mask);

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            owner.ExecuteVFXOnPositionRequest(this, VFXs[0], position, null);
            owner.PlaySoundRequest(SFXs[0]);

            CurrentHitBox.StartListenning(this); // Le trigger devient actif
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
            CurrentHitBox.EndListenning();

            EndAbility(AbilityEndMode.Classic);
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

            //entity.entityControl?.KnockBack(entity.transform.position - owner.transform.position, KnockBackForce);
            entity.entityControl?.KnockBack(entity.transform.position - owner.transform.position, KnockbackForce);
            this.PlayWeaponHitSoundRequest(entity);
            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], entity.chestTransform.position, null);

            ApplyDamageAndEffectsTo(entity, Damages, Effects);
        }

        public override Vector3 GetHitFeedbackIkPosition(Entity launcher, Entity target)
        {
            return (launcher.transform.position - target.transform.position).normalized + Vector3.down * 2;
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);
            ownerCharacter.playerControl.IsRootMotion = false;
        }
    }
}
