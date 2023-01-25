using Assets.BattleGame.Scripts.Controllers;
using RPGCharacterAnims;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
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
    public class Souffle : DistantAbility
    {
        [Header("---- SOUFFLE ----")]
        public float SouffleAngle = 90;
        public float TickRate;
        public int ExpiationProcChancesPurcent = 40;

        private float timer = 0;
        private float channelTimer = 0;

        private LookAtTargetVFX flameThrow;
        private Entity target;

        public override void StartAnimationAction()
        {
            //owner.ExecuteVFXRequest(this, VFXs[0], null);

            ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();
        }
               
        protected override IEnumerator SkillLoop(Vector3 target)
        {
            if (owner.IsLocalCharacter)
            {
                ownerCharacter.aimingSubsystem.IsAiming = true;
            }

            ownerCharacter.characterAnimationSystem.EndCast();
            yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[0]);
            ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[1]);

            yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[1]);
            owner.ExecuteVFXRequest(this,
                  VFXs[0],
                  (result) => flameThrow = result as LookAtTargetVFX);                            

            timer = 0;
            channelTimer = 0;

            if (owner.IsLocalCharacter)
            {
                StartCoroutine(ChannelingRoutine());
            }

            while (channelTimer < CurrentChannelTime)
            {
                Vector3 aimedPosition = ownerCharacter.transform.forward + leftHand.position;

                Debug.DrawLine(owner.chestTransform.position, aimedPosition, Color.blue);

                flameThrow.transform.LookAt(aimedPosition);

                channelTimer += owner.entityDeltaTime;
                float currentStreamRate = TickRate * hasteMultiplier;

                timer += owner.entityDeltaTime;
                if (timer >= currentStreamRate)
                {
                    List<Entity> hitted = GetOverlapConeEntitiesFrom(leftHand.position, owner.transform.forward, EffectRange, SouffleAngle, false, owner.Team);
                    for (int i = 0; i < hitted.Count; ++i)
                    {
                        if (hitted[i] == owner)
                            continue;

                        Debug.DrawLine(owner.chestTransform.position, hitted[i].chestTransform.position, Color.blue);

                        StartCoroutine(DamageSequence(hitted[i]));
                    }

                    timer = 0;
                }

                yield return null;
            }

            yield return waitForEndTime;

            EndAbility();
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);

            if (owner.IsLocalCharacter)
            {
                ownerCharacter.aimingSubsystem.IsAiming = false;
            }

            if (flameThrow)
                flameThrow.Stop();
        }

        protected IEnumerator DamageSequence(Entity hitted)
        {
            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hitted.chestTransform.position, null);

            yield return new WaitForSeconds(GetWeaponParameter(Weapon.TwoHandStaff).yieldTimes[2]);

            if (Randomizer.RandPurcent(ExpiationProcChancesPurcent))
            {
                ApplyDamageAndEffectsTo(hitted, Damages, Effects);
            }
            else
            {
                ApplyDamageAndEffectsTo(hitted, Damages, null);
            }
        }
    }
}
