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
    public class Flagellation : DistantAbility
    {
        [Header("---- FLAGELLATION ---- ")]
        public float ArcAngle = 90;

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        protected override IEnumerator SkillLoop(Vector3 position)
        {
            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            if (GameServer.IsMaster)
            {
                List<Entity> potentialTargets = GetOverlapConeEntitiesFrom(owner.transform.position, owner.transform.forward, EffectRange, ArcAngle, false, owner.Team);
                for (int i = 0; i < potentialTargets.Count; ++i)
                {
                    owner.abilityController.Request_StartHitLoopEntity(this, potentialTargets[i]);
                    yield return null;
                }

                yield return waitForEndTime;

                owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }
        }

        protected override IEnumerator HitLoop(Entity target)
        {
            object[] spawnData = new object[3]
            {
                (int)LineRendererFXMode.Transform,
                GunEndMain,
                target.chestTransform
            };

            // Thunder
            AbilityVFXComponentEventHandler.ExecuteVFXOnPositionRequest(owner, this, VFXs[0], GunEndMain.position, null, spawnData);

            if (GameServer.IsMaster)
            {
                ApplyDamageAndEffectsTo(target, Damages, Effects);
            }

            yield return null;
        }
    }
}
