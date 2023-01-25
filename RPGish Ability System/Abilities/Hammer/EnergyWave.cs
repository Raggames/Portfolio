using RPGCharacterAnims;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.Projectiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SteamAndMagic.GameplayFX;
using Systems.DamageEffect;
using Systems.Abilities.Interfaces;

namespace SteamAndMagic.Systems.Abilities
{
    public class EnergyWave : MeleeAbility, IShockwaveBoxAbility, IProjectileAbility
    {
        public ShockwaveBox ShockwaveBox_prefab;
        public int SlashesCount = 1;
        public int IncrementAngle = 10;
        public int ProjectileSpeed = 25;

        public bool Talent_EnergicReturn = false;

        public bool Talent_Explosive = false;
        public List<Damage> Talent_Explosive_Damage = new List<Damage>();

        protected List<ShockwaveBox> returningWaves = new List<ShockwaveBox>();

        public Projectile projectilePrefab => ShockwaveBox_prefab;

        public float projectileSpeed { get { return ProjectileSpeed; } set { ProjectileSpeed = (int)value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }

        protected override IEnumerator SkillLoop(Vector3 directionnal)
        {
            returningWaves.Clear();

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);
            // target is the direction vector ??

            Vector3 axis = (directionnal - owner.transform.position).normalized;

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Impulse(camShakePreset, -axis);
            }

            int angleStart = -Mathf.RoundToInt((float)(SlashesCount * IncrementAngle) / 2f);

            for (int i = 0; i < SlashesCount; ++i)
            {

                Vector3 position = axis.RotateVector(Vector3.up, angleStart + i * IncrementAngle) * CurrentRange + owner.transform.position;

                ShockwaveBox proj = PoolManager.Instance.SpawnGo(ShockwaveBox_prefab.gameObject, owner.chestTransform.position).GetComponent<ShockwaveBox>();
                proj.Launch(owner.transform.position, position, Vector3.one, CurrentRange, ProjectileSpeed, this);
            }

            //EndAbility();
        }

        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner || !GameServer.IsMaster || entity.Team == owner.Team)
                return;

            if (hitted_entities_temp.Contains(entity))
                return;

            ApplyDamageAndEffectsTo(entity, Damages, Effects);
            abilityController.Request_StartHitLoopEntity(this, entity);
        }

        // Hit VFX
        protected override IEnumerator HitLoop(Entity target)
        {
            owner.ExecuteVFXOnPositionRequest(this, VFXs[0], target.chestTransform.position, null);
            yield return null;
        }

        // Talent Explosive VFX
        protected override IEnumerator HitLoop(Vector3 target)
        {
            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], target, null);
            yield return null;
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.playerControl.IsRootMotion = true;

            switch (ownerCharacter.characterAnimationSystem.CurrentMainWeapon)
            {
                case Weapon.TwoHandSword:
                case Weapon.TwoHandSpear:
                case Weapon.TwoHandAxe:
                case Weapon.TwoHandStaff:
                    ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
                    break;
                case Weapon.RightSword:
                case Weapon.RightMace:
                case Weapon.RightDagger:
                case Weapon.RightItem:
                case Weapon.RightSpear:
                    ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0], false, Side.Right, false, false);
                    break;
            }
        }

        public void OnShockwaveBoxEndTravel(ShockwaveBox shBox)
        {
            if (Talent_EnergicReturn && !returningWaves.Contains(shBox))
            {
                returningWaves.Add(shBox);
                //shBox.Launch(shBox.transform.position, owner.transform.position, Vector3.one, Range, ProjectileSpeed, this);
                shBox.Launch(owner, this, ownerCharacter, Vector3.zero, owner.transform);
            }
            else if (Talent_Explosive)
            {
                if (GameServer.IsMaster)
                {
                    // Do explosion
                    List<Entity> targets = GetOverlapEntitiesFrom(shBox.transform.position, 3, false, owner.Team);
                    for (int i = 0; i < targets.Count; ++i)
                    {
                        ApplyDamageAndEffectsTo(targets[i], Talent_Explosive_Damage, null);
                        abilityController.Request_StartHitLoopEntity(this, targets[i]);
                        abilityController.Request_StartHitLoopPosition(this, shBox.transform.position);
                    }
                }

                shBox.Die();
            }
            else
            {
                shBox.Die();
            }

            if (isRunning)
                EndAbility();
        }

        public void ProjectileEndTravel(Projectile proj, Entity target = null)
        {
            proj.Die();
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
