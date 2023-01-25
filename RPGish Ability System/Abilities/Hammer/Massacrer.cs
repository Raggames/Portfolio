using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class Massacrer : MeleeAbility
    {
        [Header("----MASSACRER----")]
        public float DashDistanceFromTarget = 2.5f;
        public float DashSpeed = 20;

        public override bool IsLaunchableInternal()
        {
            if (base.IsLaunchableInternal())
            {
                return owner.CurrentEntityTarget != null && owner.CurrentEntityTarget.effectSystem != null && owner.CurrentEntityTarget.effectSystem.HasDebuffsOfClass(EffectClass.Commotion, owner, false);
            }

            return false;
        }

        protected override IEnumerator SkillLoop(Entity target)
        {
            owner.ExecuteVFXRequest(this, VFXs[0], null);

            if (owner.IsMine)
            {
                // Calcul avant au cas où l'effet disparaisse entre temps
                Effect[] commotions = target.effectSystem.GetDebuffsOfClass(EffectClass.Commotion, owner, false);

                int stacks = 0;
                for (int i = 0; i < commotions.Length; ++i)
                {
                    stacks += commotions[i].stacks + 1; // stacks commence à 0
                }

                float damValue = Damages[0].value;

                // On peut ne plus avoir de stacks au moment du lancé avec les temps serveur-client, le minimum de dégat c'est 1 * damage value
                if (stacks != 0)
                    damValue *= stacks;

                bool endedJump = false;
                Vector3 dir = owner.transform.position - target.transform.position;
                Vector3 dashTarget = dir.normalized * DashDistanceFromTarget + target.transform.position;
                ownerCharacter.playerControl.JumpTo(dashTarget, DashSpeed, 1f, 0, () => endedJump = true);
                float startTime = Time.time;

                yield return new WaitUntil(() => endedJump == true);

                CameraShaker.Instance.Shake(camShakePreset);

                float duration = Time.time - startTime;
                float wait = GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0] - duration;

                if (wait > 0)
                    yield return new WaitForSeconds(wait);

                Client_BroadcastNetworkMessage(new object[] { target.NetworkID, Mathf.RoundToInt(damValue) });
            }
        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            Entity target = GameServer.Instance.GetEntityFromID(Convert.ToInt16(data[0]));

            if (target != null)
            {
                StartCoroutine(EndMassacrer(target, (int)data[1]));
            }
            else
            {
                ownerCharacter.characterAnimationSystem.ContinueCombo(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[1]);
                owner.PlaySoundRequest(SFXs[0]);
                owner.PlaySoundRequest(SFXs[1]);
                EndAbility();
            }
        }

        private IEnumerator EndMassacrer(Entity target, int damageValue)
        {
            ownerCharacter.characterAnimationSystem.ContinueCombo(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[1]);

            Damage damage = new Damage(Damages[0]);
            damage.value = damageValue;

            target.ExecuteVFXRequest(this, VFXs[1], null);
            owner.PlaySoundRequest(SFXs[0]);
            owner.PlaySoundRequest(SFXs[1]);

            if (GameServer.IsMaster)
            {
                Effect[] commotions = target.effectSystem.GetDebuffsOfClass(EffectClass.Commotion, owner, false);

                if (target.effectSystem.HasDebuffsOfClass(EffectClass.Stun, owner, false))
                {
                    ApplyDamageAndEffectsTo(target, new List<Damage>() { damage }, Effects);
                }
                else
                {
                    ApplyDamageAndEffectsTo(target, new List<Damage>() { damage }, null);
                }

                for (int i = 0; i < commotions.Length; ++i)
                {
                    target.effectSystem.Request_Remove(commotions[i]);
                }
            }

            yield return waitForEndTime;

            EndAbility();
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);
            ownerCharacter.playerControl.IsRootMotion = false;
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.playerControl.IsRootMotion = true;
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        public override void UpdateTargetingArea(bool canBeLaunched, Vector3 tarOrDir, bool noTargetAvalaible = false)
        {
            TargetingArea.gameObject.SetActive(true);

            TargetingArea.transform.position = owner.transform.position;
            TargetingArea.DisplayCircleArea(EffectRange == 0 ? 1 : EffectRange);
        }

    }
}
