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
    public class AttaquePieuse : DistantAbility, IProjectileAbility
    {
        #region Projectile

        public Projectile ProjectilePrefab;
        public Projectile projectilePrefab { get { return ProjectilePrefab; } set { ProjectilePrefab = value; } }

        public float ProjectileSpeed = 15;
        public float projectileSpeed { get { return ProjectileSpeed; } set { ProjectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }

        #endregion

        public override void StartAnimationAction()
        {
            if (VFXs.Length > 0)
            {
                owner.ExecuteVFXRequest(this, VFXs[0], null);
            }

            ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            ownerCharacter.characterAnimationSystem.EndCast();
            //ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);
            ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);

                Vector3 normalizedShotDirection = (ownerCharacter.targetingSystem.GetFreeAimingPoint(Camera.main.transform.position, Camera.main.transform.forward, CurrentRange + CameraOperator.Instance.CamToPlayerDistance, false) - leftHand.position).normalized;
                Debug.DrawRay(leftHand.position, normalizedShotDirection, Color.red);

                Client_BroadcastNetworkMessage(new object[] { normalizedShotDirection });
            }
        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, leftHand.position).GetComponent<Projectile>();
            proj.Launch(owner, this, null, (Vector3)data[0]);

            EndAbility();
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
            }

            pObj.PlaySound(SFXs[0]);
        }

        protected override IEnumerator HitLoop(Entity target)
        {
            if (VFXs.Length > 1)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[1], target.chestTransform.position, null);
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
