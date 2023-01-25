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
    public class Revigoration : DistantAbility
    {
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
            //ownerCharacter.characterAnimationSystem.EndCast();
        }

        protected override IEnumerator SkillLoop(Entity target)
        {
            ownerCharacter.characterAnimationSystem.EndCast();
            //ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            target.ExecuteVFXRequest(this, VFXs[1], null);
            ApplyDamageAndEffectsTo(target, Damages, Effects);
            yield return waitForEndTime;

            EndAbility();
        }

    }
}
