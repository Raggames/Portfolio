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
    public class EclairDivin : DistantAbility
    {
        [Header("---- ECLAIR DIVIN ----")]
        public float ShotRadius = .45f;

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

            yield return null;

            if (owner.IsMine)
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
                StartCoroutine(Shot(posTarget));
            }
        }

        protected IEnumerator Shot(Vector3 target)
        {
            int mask = LayerMask.GetMask("Entities", "Walls", "Ground");

            owner.PlaySoundRequest(SFXs[0]);

            ownerCharacter.characterAnimationSystem.AttackCast(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[1]);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);
            // Line
            owner.ExecuteVFXOnPositionRequest(this, VFXs[2], leftHand.position, null, new object[] { (int)LineRendererFXMode.Point, target ,leftHand.position }); // (fx) => fx.transform.rotation = owner.chestTransform.rotation

            RaycastHit[] hits = Physics.SphereCastAll(GunEndMain.position, ShotRadius, target - GunEndMain.position, CurrentRange, mask);
            for (int i = 0; i < hits.Length; ++i)
            {
                Entity hitEntity = null;

                if (hits[i].collider.TryGetEntityFromCollider(out hitEntity))
                {
                    // Hit
                    owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hits[i].point, null);

                    if (GameServer.IsMaster)
                    {
                        ApplyDamageAndEffectsTo(hitEntity, Damages, Effects);
                    }
                }
            }
            
            if (owner.IsMine)
            {
                yield return waitForEndTime;

                owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }
        }
    }
}
