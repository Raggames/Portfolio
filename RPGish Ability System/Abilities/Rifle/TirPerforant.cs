using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Targeting;
using System.Collections;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class TirPerforant : SimpleRifleShot
    {
        protected override IEnumerator Shot(Vector3 target, int shotIndex)
        {
            int mask = LayerMask.GetMask("Entities", "Walls", "Ground");

            owner.PlaySoundRequest(SFXs[0]);

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);

            RaycastHit[] hits = Physics.SphereCastAll(GunEndMain.position, ShotRadius, target - GunEndMain.position, CurrentRange, mask);
            for (int i = 0; i < hits.Length; ++i)
            {
                Entity hitEntity = null;

                if (hits[i].collider.TryGetEntityFromCollider(out hitEntity))
                {
                    if (VFXs.Length > 0 && VFXs[0] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[0], GunEndMain.position, (fx) => fx.transform.LookAt(target));
                    }

                    if (VFXs.Length > 1 && VFXs[1] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hits[i].point, null);
                    }

                    if (GameServer.IsMaster)
                    {
                        if (KnockbackForce > 0)
                        {
                            hitEntity.entityControl.KnockBack(hitEntity.transform.position - owner.transform.position, KnockbackForce);
                        }

                        ApplyDamageAndEffectsTo(hitEntity, Damages, Effects);
                    }
                }
            }

            //DisplayShotVFX(GunEndMain.position, target);

            if (GameServer.IsMaster && shotIndex >= ShotsCount - 1)
            {
                yield return waitForEndTime;

                owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);

            owner.PlaySoundRequest(SFXs[1]);
        }
    }
}
