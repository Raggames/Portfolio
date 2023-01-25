using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.DamagePhysics;
using System.Collections;
using System.Collections.Generic;
using Systems.Abilities.Interfaces;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class BroyageDesOs : MeleeAbility
    {
        protected override IEnumerator SkillLoop(Vector3 target)
        {
            owner.ExecuteVFXRequest(this, VFXs[0], null);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            owner.PlaySoundRequest(SFXs[0]);

            List<Entity> hittedEntities = GetOverlapConeEntitiesFrom(owner.transform.position, owner.transform.forward, EffectRange, 180, false, owner.Team);
            for (int i = 0; i < hittedEntities.Count; ++i)
            {
                ApplyDamageAndEffectsTo(hittedEntities[i], Damages, Effects);
                hittedEntities[i].ExecuteVFXRequest(this, VFXs[1], null);
            }

            yield return waitForEndTime;

            EndAbility(AbilityEndMode.Classic);
            //ownerCharacter.playerControl.IsRootMotion = false;
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        /*public override void UpdateTargetingArea(bool canBeLaunched, Vector3 tarOrDir, bool noTargetAvalaible = false)
        {
            TargetingArea.gameObject.SetActive(true);
            TargetingArea.transform.position = owner.transform.position;
            TargetingArea.DisplayCircleArea(EffectRange == 0 ? 1 : EffectRange);
        }*/
    }
}
