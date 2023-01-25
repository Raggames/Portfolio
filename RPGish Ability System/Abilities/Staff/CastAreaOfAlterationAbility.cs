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
    public class CastAreaOfAlterationAbility  : DistantAbility
    {
        [Header("CAST AREA OF ALTERATION ----")]
        public AreaOfAlteration AreaOfAlteration_prefab;

        public override void StartAnimationAction()
        {
            if (VFXs.Length > 0)
            {
                owner.ExecuteVFXRequest(this, VFXs[0], null);
            }

            switch (GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0])
            {
                case 0:
                    ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);
                    break;
                case 1:
                    ownerCharacter.characterAnimationSystem.Boost(Weapon.TwoHandStaff);
                    break;
            }
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

            object[] launchData = new object[2];
            launchData[0] = owner.NetworkID;
            launchData[1] = this.controllerIndex;

            PoolManager.Instance.SpawnGo(AreaOfAlteration_prefab.gameObject, target, null, launchData);

            yield return waitForEndTime;

            EndAbility();
        }
    }
}
