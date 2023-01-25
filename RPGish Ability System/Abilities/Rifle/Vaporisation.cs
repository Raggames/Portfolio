using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.Projectiles;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems.Abilities.Interfaces;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class Vaporisation : RifleAbility, IProjectileAbility
    {
        [Header("---- VAPORISATION ----")]
        public float ExplodeRange = 5f;

        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed;

        public Projectile projectilePrefab { get { return _projectilePrefab; } set { _projectilePrefab = value; } }
        public float projectileSpeed { get { return _projectileSpeed; } set { _projectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }


        public override void StartAnimationAction()
        {
            if (owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = true;

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            if (owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = false;
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            Vector3 normalizedShotDirection = (target - GunEndMain.position).normalized;
            Debug.DrawRay(GunEndMain.position, normalizedShotDirection, Color.red);

            Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, GunEndMain.position).GetComponent<Projectile>();
            proj.Launch(owner, this, null, normalizedShotDirection);

            yield return waitForEndTime;

            EndAbility();
        }

        public void ProjectileEndTravel(Projectile proj, Entity target = null)
        {
            if (GameServer.IsMaster)
            {
                abilityController.Request_StartHitLoopPosition(this, proj.transform.position);
            }

            proj.Die();
        }

        protected override void OnDetectWorld(PhysicsObject pObj)
        {
            if (GameServer.IsMaster)
            {
                abilityController.Request_StartHitLoopPosition(this, pObj.transform.position);
            }

            pObj.Die();
        }

        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner || entity.Team == owner.Team)
                return;

            if (GameServer.IsMaster)
            {
                abilityController.Request_StartHitLoopPosition(this, pObj.transform.position);
            }

            pObj.Die();
        }

        protected override IEnumerator HitLoop(Vector3 target)
        {
            owner.ExecuteVFXOnPositionRequest(this, VFXs[0], target, null);

            List<Entity> targets = GetOverlapEntitiesFrom(target, ExplodeRange, false);

            //WorldManager.Instance.GenerateShake(proj.transform.position, EffectRange, 3);

            for (int i = 0; i < targets.Count; ++i)
            {
                ApplyDamageAndEffectsTo(targets[i], Damages, Effects);
            }

            yield return null;
        }

        public override void UpdateTargetingArea(bool canBeLaunched, Vector3 tarOrDir, bool noTargetAvalaible = false)
        {
            TargetingArea.transform.position = owner.transform.position;
            TargetingArea.DisplayArea(new Vector3(1, EffectRange * 2, 8), (tarOrDir - owner.transform.position).normalized);

            TargetingArea.gameObject.SetActive(true);
        }
    }
}
