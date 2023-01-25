using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class GrosseMitraille : SimpleRifleShot
    {
        [Header("---- GROSSE MITRAILLE----")]
        public int ShotAngle = 60;
       
        protected override IEnumerator SkillLoop(Vector3 target)
        {
            //ownerCharacter.characterAnimationSystem.StartRifleAiming(target, CurrentRange);
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);

            if (owner.IsMine)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            owner.ExecuteVFXOnPositionRequest(this, VFXs[0], GunEndMain.position, (vfx) => vfx.transform.LookAt(target));

            List<Entity> hittedEntities = GetOverlapConeEntitiesFrom(GunEndMain.position, (target - GunEndMain.position).normalized, CurrentRange, ShotAngle, true, owner.Team);
            for (int i = 0; i < hittedEntities.Count; ++i)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hittedEntities[i].chestTransform.position, (vfx) => vfx.transform.LookAt(GunEndMain.position));

                if (GameServer.IsMaster)
                {
                    if (KnockbackForce > 0)
                    {
                        hittedEntities[i].entityControl.KnockBack(hittedEntities[i].transform.position - owner.transform.position, KnockbackForce);
                    }

                    ApplyDamageAndEffectsTo(hittedEntities[i], Damages, Effects);
                }
            }

            yield return waitForEndTime;

            EndAbility();
        }
    }
}
