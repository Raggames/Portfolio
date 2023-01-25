using Assets.BattleGame.Scripts.Controllers;
using RPGCharacterAnims;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Targeting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class VoeuxDeSante : DistantAbility
    {
        public override void StartAnimationAction()
        {
            owner.ExecuteVFXRequest(this, VFXs[0], (vfx) => CurrentCastVFX = vfx);
            CurrentCastVFX.FollowTransform = leftHand;

            ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();

        }
        protected override IEnumerator SkillLoop(Vector3 target)
        {
            CurrentCastVFX.Stop();

            if (owner.IsLocalCharacter)
            {
                Client_BroadcastNetworkMessage(new object[] { TargetingSystem.GetAimingPoint(owner.transform.position, CurrentRange, EffectRange)});
            }

            yield return waitForEndTime;
        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            owner.ExecuteVFXOnPositionRequest(this, VFXs[1], (Vector3)data[0], null);
            List<Entity> targets = GetOverlapEntitiesFrom((Vector3)data[0], EffectRange, false);

            for (int i = 0; i < targets.Count; ++i)
            {
                ApplyDamageAndEffectsTo(targets[i], Damages, Effects);
            }

            EndAbility();
        }
    }
}
