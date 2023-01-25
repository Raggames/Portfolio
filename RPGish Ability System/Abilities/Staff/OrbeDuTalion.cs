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
    public class OrbeDuTalion : DistantAbility, IProjectileAbility
    {
        #region Projectile
        [Header("---- ORBE DU TALION ----")]
        public Projectile ProjectilePrefab;
        public Projectile projectilePrefab { get { return ProjectilePrefab; } set { ProjectilePrefab = value; } }

        public float ProjectileSpeed = 15;
        public float projectileSpeed { get { return ProjectileSpeed; } set { ProjectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }

        #endregion

        public List<Damage> MainTargetAllyDamages = new List<Damage>();
        public List<Effect> AllyEffects = new List<Effect>();

        public List<Damage> MainTargetEnemyDamages = new List<Damage>();
        public List<Effect> EnemyEffects = new List<Effect>();

        private object[] shotData = new object[1];

        public override void StartAnimationAction()
        {
            owner.ExecuteVFXRequest(this, VFXs[0], null);

            ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            ownerCharacter.characterAnimationSystem.EndCast();

            yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[0]);

            ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);

            yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[1]);

            owner.PlaySoundRequest(SFXs[2]);

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);

                shotData[0] = GetFreeAimingPoint();
                Client_BroadcastNetworkMessage(shotData);
            }

            yield return null;

        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            if (data != null && data.Length == 1)
            {
                Vector3 posTarget = (Vector3)data[0];

                Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, leftHand.position).GetComponent<Projectile>();
                proj.Launch(owner, this, null, (posTarget - leftHand.position).normalized);

                EndAbility();
            }
        }


        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner)
            {
                return;
            }

            if (GameServer.IsMaster)
            {
                owner.abilityController.Request_StartHitLoopEntity(this, entity);
            }

            pObj.PlaySound(SFXs[0]);

            pObj.Die();
        }

        protected override IEnumerator HitLoop(Entity target)
        {            
            if(target.Team == owner.Team)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[1], target.chestTransform.position, null);

                yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[2]);

                ApplyDamageAndEffectsTo(target, MainTargetAllyDamages, AllyEffects);
            }
            else
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[2], target.chestTransform.position, null);

                yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[3]);
                
                ApplyDamageAndEffectsTo(target, MainTargetEnemyDamages, EnemyEffects);
            }
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
