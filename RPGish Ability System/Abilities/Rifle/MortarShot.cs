using SteamAndMagic.Entities;
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
    public class MortarShot : RifleAbility, IProjectileAbility
    {
        [Header("---- MORTIERS ---- ")]
        public int ProjectilesCount = 1;
        public float ExplodeRange = 3;

        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed;

        public Projectile projectilePrefab { get { return _projectilePrefab; } set { _projectilePrefab = value; } }
        public float projectileSpeed { get { return _projectileSpeed; } set { _projectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            if(ProjectilesCount == 1)
            {
                Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, GunEndMain.position).GetComponent<Projectile>();
                proj.Launch(owner, this, null, target);
            }
            else
            {
                for (int i = 0; i < ProjectilesCount; ++i)
                {
                    Vector2 random = UnityEngine.Random.insideUnitCircle * EffectRange / 2f;
                    Vector3 shotPosition = target + new Vector3(random.x, 0, random.y);

                    Projectile proj = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, GunEndMain.position).GetComponent<Projectile>();
                    proj.Launch(owner, this, null, shotPosition);
                }
            }

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
            base.UpdateTargetingArea(canBeLaunched, tarOrDir, noTargetAvalaible);

            ownerCharacter.characterAnimationSystem.UpdateAiming(tarOrDir, CurrentRange);
        }

    }
}
