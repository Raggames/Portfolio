using RPGCharacterAnims;
using Sirenix.OdinInspector;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using SteamAndMagic.Scripts.Control;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.VisualAndSoundFX;
using System.Collections;
using System.Collections.Generic;
using Systems.Abilities.Interfaces;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class HammerFiller : MeleeAbility, IHitboxAbility
    {
        public int CommotionProcChancesPurcent = 25;

        [ShowInInspector, ReadOnly] private int comboIndex = 0;
        public int maxComboIndex = 3;

        public float[] vfxYieldTimes = new float[0];
        public float KnockBack = 5;

        public int EnergyGenerated = 5;
        public int EnergyBonusPerComboIndex = 3;

        public HitBox HitBox_prefab { get => hitbox_Prefab; }
        [SerializeField] private HitBox hitbox_Prefab;

        public HitBox CurrentHitBox { get; set; }

        private List<Damage> currentDamages = new List<Damage>();

        public override bool IsLaunchable()
        {
            if (owner.entityControl.IsFalling || owner.entityControl.IsJumping)
                return false;

            return base.IsLaunchable();
        }

        public override void Init(AbilityController skillbookComp, int skIndex, Entity owner)
        {
            base.Init(skillbookComp, skIndex, owner);
            comboIndex = 0;

            if (CurrentHitBox == null)
            {
                (this as IHitboxAbility).CreateHitBox(owner, this, HitBox_prefab, this.owner.transform, rightHand);
                CurrentHitBox.SetDimensions(new Vector3(EffectRange, 5, EffectRange));
            }
        }
                

        protected override IEnumerator SkillLoop(Vector3 position)
        {
            Debug.Log("0_" + comboIndex);                       

            WeaponVFXComponentEventHandler.ToggleWeaponTrailRequest(owner, true, 0);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[comboIndex]);

            if (owner.IsLocalCharacter)
                CameraShaker.Instance.Shake(camShakePreset);

            Debug.Log("1_" + comboIndex);

            if (comboIndex == 2)
            {
                owner.PlaySoundRequest(SFXs[0]);
            }

            CurrentHitBox.StartListenning(this); // Le trigger devient actif
            yield return new WaitForSeconds(.2f);
            CurrentHitBox.EndListenning();

            Debug.Log("2_" + comboIndex);

            WeaponVFXComponentEventHandler.ToggleWeaponTrailRequest(owner, false, 0);

            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[maxComboIndex + comboIndex]);

            Debug.Log("3_" + comboIndex);

            comboIndex++;
            if (comboIndex >= maxComboIndex)
            {
                comboIndex = 0;
            }

            EndAbility(AbilityEndMode.Classic);
        }

        /// <summary>
        /// Callback de la hitbox lorsqu'elle touche
        /// </summary>
        /// <param name="pObj"></param>
        /// <param name="entity"></param>
        protected override void OnDetectEntity(PhysicsObject pObj, Entity entity)
        {
            if (entity == owner || entity.Team == owner.Team)
                return;

            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            entity.entityControl?.KnockBack(entity.transform.position - owner.transform.position, KnockBack);
            this.PlayWeaponHitSoundRequest(entity);

            if(comboIndex < 2)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[0], entity.chestTransform.position, null);
            }
            else
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[1], entity.chestTransform.position, null);
            }

            if (GameServer.IsMaster)
            {
                currentDamages.Clear();
                currentDamages.Add(Damages[comboIndex]);

                int energyGenerated = EnergyGenerated + comboIndex * EnergyBonusPerComboIndex;

                if (Randomizer.RandPurcent(CommotionProcChancesPurcent) || comboIndex == 2)
                {
                    ApplyDamageAndEffectsTo(entity, currentDamages, Effects);
                }
                else
                {
                    ApplyDamageAndEffectsTo(entity, currentDamages, null);
                }

                owner.ressourceSystem.Request_AddRessource(Ressource.RessourceType.Energy, energyGenerated);
            }
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            StopAllCoroutines();
        }

        public override void StartAnimationAction()
        {
            /*animIndex++;
            if (animIndex > maxAnimIndex)
                animIndex = minAnimIndex;*/

            if (comboIndex == 0)
            {
                ownerCharacter.playerControl.IsRootMotion = true;
                // Trigger l'entrée dans la boucle de combo
                ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentWeapon).animTriggers[comboIndex], false, Side.None, false, true);
            }
            else
            {
                ownerCharacter.characterAnimationSystem.ContinueCombo(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentWeapon).animTriggers[comboIndex]);
            }
        }

        public override void OnAutoLaunchEnded()
        {
            base.OnAutoLaunchEnded();
            Debug.Log("Autolaunch ended");
            comboIndex = 0;
            ownerCharacter.playerControl.IsRootMotion = false;
            // Trigger la sortie de la boucle de combo
            ownerCharacter.characterAnimationSystem.AttackSpecialEnd();
        }
    }
}
