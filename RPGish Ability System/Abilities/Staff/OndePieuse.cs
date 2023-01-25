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
    public class OndePieuse : DistantAbility
    {
        [Header("---- ONDE PIEUSE ----")]
        public int SelfHealChancesPurcent = 50;
        public List<Damage> SelfHeal = new List<Damage>();

        public override void StartAnimationAction()
        {            
            ownerCharacter.characterAnimationSystem.StartCast(GetWeaponParameter(Weapon.TwoHandStaff).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.EndCast();
        }

        protected override IEnumerator SkillLoop(Entity target)
        {
            owner.ExecuteVFXRequest(this, VFXs[0], null);

            if (GameServer.IsMaster)
            {
                WaitForSeconds waitBetweenDistances = new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);
                int heal = 0;
                for (int i = 1; i <= EffectRange; ++i)
                {
                    List<Entity> toDamage = GetOverlapEntitiesFrom(owner.transform.position, i, false, owner.Team);

                    for(int j = 0; j < toDamage.Count; ++j)
                    {
                        if (!hitted_entities_temp.Contains(toDamage[j]))
                        {
                            ApplyDamageAndEffectsTo(toDamage[j], Damages, Effects);
                            heal += Randomizer.RandPurcent(SelfHealChancesPurcent) ? SelfHeal[0].value : 0;
                        }
                    }

                    yield return waitBetweenDistances;
                }

                if (heal != 0)
                {
                    Damage healDam = new Damage(SelfHeal[0]);
                    healDam.value = Mathf.RoundToInt(heal);
                    ApplyDamageAndEffectsTo(owner, new List<Damage>() { healDam }, null);
                }
            }

            yield return waitForEndTime;

            EndAbility();
        }
    }
}
