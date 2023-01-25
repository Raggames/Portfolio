using Assets.BattleGame.Scripts.Controllers;
using RPGCharacterAnims;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Scripts.Control;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.Projectiles;
using SteamAndMagic.Systems.Targeting;
using System.Collections;
using System.Collections.Generic;
using Systems.Abilities.Interfaces;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class StaffFiller : DistantAbility, IProjectileAbility
    {
        #region Projectile

        public Projectile ProjectilePrefab;
        public Projectile projectilePrefab { get { return ProjectilePrefab; } set { ProjectilePrefab = value; } }

        public float ProjectileSpeed = 15;
        public float projectileSpeed { get { return ProjectileSpeed; } set { ProjectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }

        #endregion

        public int EnergyGenerated = 20;

        public override void OnEnable()
        {
            base.OnEnable();

            /*EffectSystem.OnReceivedEffect += EffectSystem_OnReceivedEffect;
            CharacterAnimationSystem.OnDrawedWeapon += OnUnsheathWeapon;
            CharacterAnimationSystem.OnSheathedWeapon += OnSheathWeapon;*/
        }

        public override void OnDisable()
        {
            base.OnDisable();

            /*EffectSystem.OnReceivedEffect -= EffectSystem_OnReceivedEffect;
            CharacterAnimationSystem.OnDrawedWeapon -= OnUnsheathWeapon;
            CharacterAnimationSystem.OnSheathedWeapon -= OnSheathWeapon;*/
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            Vector3 normalizedShotDirection = (target - leftHand.position).normalized;
            Debug.DrawRay(leftHand.position, normalizedShotDirection, Color.red);

            Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, leftHand.position).GetComponent<Projectile>();
            proj.Launch(owner, this, null, normalizedShotDirection);

            yield return waitForEndTime;

            EndAbility();
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();
        }

        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner || entity.Team == owner.Team)
            {
                return;
            }

            if (GameServer.IsMaster)
            {
                owner.abilityController.Request_StartHitLoopEntity(this, entity);
                owner.ressourceSystem.Request_AddRessource(Ressource.RessourceType.Energy, EnergyGenerated);
            }

            pObj.PlaySound(SFXs[0]);

            pObj.Die();
        }

        protected override IEnumerator HitLoop(Entity target)
        {
            if (VFXs.Length > 0)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[0], target.chestTransform.position, null);
            }

            ApplyDamageAndEffectsTo(target, Damages, Effects);

            yield return null;
        }

        protected override void OnDetectWorld(PhysicsObject pObj)
        {
            pObj.PlaySound(SFXs[1]);
            pObj.Die();
        }

        public void ProjectileEndTravel(Projectile proj, Entity target = null)
        {
            proj.PlaySound(SFXs[1]);
            proj.Die();
        }
    }
}
