using Sirenix.OdinInspector;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using SteamAndMagic.Systems.Targeting;
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
    public class FrappeElectrique : MeleeAbility
    {
        [Header("---- FRAPPE DE FOUDRE ----")]
        public int DamagePurcentPerSpread = 60;
        public int SpreadRange = 5;
        public TargetingArea RangeArea_prefab;
        protected TargetingArea RangeArea;

        [ShowInInspector] private int runningRoutinesCount = 0;

        public override void Init(AbilityController skillbookComp, int skIndex, Entity owner)
        {
            base.Init(skillbookComp, skIndex, owner);

            RangeArea = Instantiate(RangeArea_prefab);
            RangeArea.gameObject.SetActive(false);
            RangeArea.Owner = owner.gameObject.transform;
        }

        protected override IEnumerator SkillLoop(Entity target)
        {
            owner.ExecuteVFXRequest(this, VFXs[2], null);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            target.ExecuteVFXRequest(this, VFXs[3], null);

            owner.PlaySoundRequest(SFXs[0]);

            if (GameServer.IsMaster)
            {
                runningRoutinesCount = 0;

                // DO HIT
                ApplyDamageAndEffectsTo(target, Damages, Effects);
                StartCoroutine(SpreadElectricDamageFromTarget(target, new List<Entity>() { target }, Damages[0].value));

                yield return new WaitUntil(() => runningRoutinesCount == 0);

                yield return waitForEndTime;

                owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }

            //ownerCharacter.playerControl.IsRootMotion = false;
        }

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        private IEnumerator SpreadElectricDamageFromTarget(Entity target, List<Entity> hittedTargets, int damageValue)
        {
            runningRoutinesCount++;

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);

            float damValue = (float)damageValue * DamagePurcentPerSpread / 100f;
            List<Entity> inSpreadRange = GetOverlapEntitiesFrom(target.transform.position, SpreadRange, false, owner.Team);
            for (int i = 0; i < inSpreadRange.Count; ++i)
            {
                if (inSpreadRange[i].Team != owner.Team && !hittedTargets.Contains(inSpreadRange[i]))
                {
                    Damage dam = new Damage(Damages[0]);
                    dam.value = Mathf.RoundToInt(damValue);

                    ApplyDamageAndEffectsTo(inSpreadRange[i], new List<Damage>() { dam }, Effects);

                    // Trigger effects
                    Client_BroadcastNetworkMessage(new object[] { target.chestTransform.position, inSpreadRange[i].chestTransform.position });

                    hittedTargets.Add(inSpreadRange[i]);

                    // Continue spread
                    StartCoroutine(SpreadElectricDamageFromTarget(target, hittedTargets, dam.value));
                }
            }

            runningRoutinesCount--;
        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            ExecuteThunderVFX((Vector3)data[0], (Vector3)data[1]);
        }

        private void ExecuteThunderVFX(Vector3 start, Vector3 end)
        {
            object[] spawnData = new object[3]
            {
                (int)LineRendererFXMode.Point,
                start,
                end
            };

            // Thunder
            AbilityVFXComponentEventHandler.ExecuteVFXOnPositionRequest(owner, this, VFXs[0], end, null, spawnData); // (fx) => fx.transform.rotation = owner.chestTransform.rotation
            // Hit
            AbilityVFXComponentEventHandler.ExecuteVFXOnPositionRequest(owner, this, VFXs[1], end, null, null);
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            StopAllCoroutines();
        }

        public override void OnEndedByPlayer()
        {
            base.OnEndedByPlayer();

            StopAllCoroutines();
        }

        public override void UpdateTargetingArea(bool canBeLaunched, Vector3 tarOrDir, bool noTargetAvalaible = false)
        {
            if (canBeLaunched)
            {
                TargetingArea.transform.position = tarOrDir;
                TargetingArea.DisplayCircleArea(EffectRange == 0 ? 1 : EffectRange);
                TargetingArea.gameObject.SetActive(true);
                RangeArea.gameObject.SetActive(false);
            }
            else
            {
                RangeArea.transform.position = owner.transform.position;
                RangeArea.DisplayCircleArea(CurrentRange == 0 ? 1 : CurrentRange);
                RangeArea.gameObject.SetActive(true);
                TargetingArea.gameObject.SetActive(false);
            }
        }

        public override void HideTargetingArea()
        {
            base.HideTargetingArea();
            RangeArea.gameObject.SetActive(false);
        }
    }
}
