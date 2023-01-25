using RPGCharacterAnims;
using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.Inventory;
using SteamAndMagic.Systems.Items;
using SteamAndMagic.Systems.Projectiles;
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
    /*
     * Envoie votre arme vers une cible à 20 mètres maximum et lui inflige 500%WD en la touchant. Rappelle ensuite votre arme à vous.
     */
    public class LancePuissant : MeleeAbility, IProjectileAbility
    {
        #region Projectile
        public Projectile ProjectilePrefab;
        public Projectile projectilePrefab { get { return ProjectilePrefab; } set { ProjectilePrefab = value; } }

        public float ProjectileSpeed = 15;
        public float projectileSpeed { get { return ProjectileSpeed; } set { ProjectileSpeed = value; } }
        public float weightFactor { get; set; }
        public float impactWeigth { get; set; }
        #endregion

        [ReadOnly] public GameObject RightWeaponModel;
        [ReadOnly] public Projectile CurrentProjectile;

        public override void OnEnable()
        {
            base.OnEnable();

            EquipmentSystemEventHandler.OnEquip += EquipmentSystemEventHandler_OnEquip;
            EquipmentSystemEventHandler.OnUnequip += EquipmentSystemEventHandler_OnUnequip;
            EquipmentSystemEventHandler.OnEquipmentSystemInitialized += EquipmentSystemEventHandler_OnEquipmentSystemInitialized;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            EquipmentSystemEventHandler.OnEquip -= EquipmentSystemEventHandler_OnEquip;
            EquipmentSystemEventHandler.OnUnequip -= EquipmentSystemEventHandler_OnUnequip;
            EquipmentSystemEventHandler.OnEquipmentSystemInitialized -= EquipmentSystemEventHandler_OnEquipmentSystemInitialized;
        }

        public override bool IsLaunchable()
        {
            return RightWeaponModel != null && IsFocusInRange(Range) && base.IsLaunchable();
        }

        private void SetWeaponModel()
        {
            if (ownerCharacter.equipmentSystem.RightWeapon != null)
            {
                RightWeaponModel = ownerCharacter.equipmentSystem.RightWeapon.Model;
            }
            else
            {
                RightWeaponModel = null;
            }
        }

        private void EquipmentSystemEventHandler_OnEquipmentSystemInitialized(EquipmentSystem context)
        {
            if (ownerCharacter != null && ownerCharacter != null && context == ownerCharacter.equipmentSystem && controllerIndex > 0)
            {
                SetWeaponModel();
            }
        }

        private void EquipmentSystemEventHandler_OnEquip(EquipmentSystem context, Items.ItemSetting setting)
        {
            if (ownerCharacter != null && ownerCharacter != null && context == ownerCharacter.equipmentSystem && controllerIndex > 0)
            {
                SetWeaponModel();
            }
        }

        private void EquipmentSystemEventHandler_OnUnequip(EquipmentSystem owner, ItemSetting setting)
        {
            if (ownerCharacter != null && owner == ownerCharacter.equipmentSystem && controllerIndex > 0)
            {
                SetWeaponModel();
            }
        }

        protected override IEnumerator SkillLoop(Entity target)
        {
            if (owner.IsMoving)
                yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
            else
                yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            CurrentProjectile = PoolManager.Instance.SpawnGo(projectilePrefab.gameObject, rightHand.position).GetComponent<Projectile>();

            CurrentProjectile.LookAtTarget = true;
            CurrentProjectile.LookAtTargetReverse = false;

            CurrentProjectile.Launch(owner, this, target, Vector3.zero);
            Transform[] childs = CurrentProjectile.GetComponentsInChildren<Transform>();

            bool hasweapon = false;
            for (int i = 0; i < childs.Length; ++i)
            {
                if (childs[i].name == RightWeaponModel.name)
                {
                    hasweapon = true;
                    break;
                }
            }

            if (!hasweapon)
            {
                GameObject weaponModel = PoolManager.Instance.SpawnGo(RightWeaponModel, Vector3.zero, CurrentProjectile.transform);
                weaponModel.name = RightWeaponModel.name;
            }

            ownerCharacter.characterAnimationSystem.RIGHT_WEAPON.SetActive(false);
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);

            CurrentProjectile?.Die();
            ownerCharacter.characterAnimationSystem.RIGHT_WEAPON.SetActive(true);
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            return base.SkillLoop(target);
        }

        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {

        }

        protected override void OnDetectWorld(PhysicsObject pObj)
        {
        }

        public void ProjectileEndTravel(Projectile proj, Entity target = null)
        {
            if (target == owner)
            {
                CurrentProjectile?.Die();
                ownerCharacter.characterAnimationSystem.RIGHT_WEAPON.SetActive(true);

                if (owner.IsLocalCharacter)
                {
                    CameraShaker.Instance.Shake(camShakePreset);
                }
            }

            if (!GameServer.IsMaster)
                return;

            if (target == owner)
            {
                abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }
            else
            {
                if (target.IsAI)
                {
                    target.Taunt(owner);
                }

                ApplyDamageAndEffectsTo(target, Damages, Effects);
                abilityController.Request_StartHitLoopEntity(this, target);
            }
        }

        protected override IEnumerator HitLoop(Entity target)
        {
            owner.ExecuteVFXOnPositionRequest(this, VFXs[0], target.chestTransform.position, null);

            CurrentProjectile.LookAtTarget = false;
            CurrentProjectile.LookAtTargetReverse = true;
            CurrentProjectile.Launch(owner, this, owner, Vector3.zero, rightHand);
            yield return null;
        }

        public override void StartAnimationAction()
        {
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
    }
}
