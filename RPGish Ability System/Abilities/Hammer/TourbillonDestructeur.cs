using Assets.SteamAndMagic.Scripts.Managers;
using RPGCharacterAnims;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using System.Collections;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class TourbillonDestructeur : MeleeAbility
    {
        [Header("Thunder Struck")]
        public float Rate = .25f;
        private float timer = 0;
        private float channelTimer = 0;
        public float KnockBack = 20;

        private VFX currentMainVfx;

        public override void StartAnimationAction()
        {
            switch (ownerCharacter.characterAnimationSystem.CurrentWeapon)
            {
                case Weapon.TwoHandSword:
                case Weapon.TwoHandSpear:
                case Weapon.TwoHandAxe:
                case Weapon.TwoHandStaff:
                case Weapon.LeftSword:
                case Weapon.RightSword:
                case Weapon.LeftMace:
                case Weapon.RightMace:
                case Weapon.LeftDagger:
                case Weapon.RightDagger:
                case Weapon.LeftItem:
                case Weapon.RightItem:
                case Weapon.RightSpear:
                case Weapon.Dual:
                    ownerCharacter.characterAnimationSystem.AttackSpecial();
                    break;
            }
        }

        public override void EndAnimationAction()
        {
            switch (ownerCharacter.characterAnimationSystem.CurrentWeapon)
            {
                case Weapon.TwoHandSword:
                case Weapon.TwoHandSpear:
                case Weapon.TwoHandAxe:
                case Weapon.TwoHandStaff:
                case Weapon.LeftSword:
                case Weapon.RightSword:
                case Weapon.LeftMace:
                case Weapon.RightMace:
                case Weapon.LeftDagger:
                case Weapon.RightDagger:
                case Weapon.LeftItem:
                case Weapon.RightItem:
                case Weapon.RightSpear:
                case Weapon.Dual:
                    ownerCharacter.characterAnimationSystem.AttackSpecialEnd();
                    break;
            }
        }

        protected override IEnumerator SkillLoop(Vector3 position)
        {
            channelTimer = 0;
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentWeapon).yieldTimes[0]);
            LayerMask mask = LayerMask.GetMask("Entities");
            //wner.PlaySoundRequest(WooshWooshClip)

            // Stopping ground check 
            if (owner.IsMine)
                StartCoroutine(LockGroundCheck());

            owner.ExecuteVFXRequest(this, VFXs[0], (vfx) => currentMainVfx = vfx);

            while (channelTimer < CurrentChannelTime)
            {
                channelTimer += WorldManager.characterLocalWorldDeltaTime;
                float currentStreamRate = Rate * hasteMultiplier;

                timer += WorldManager.characterLocalWorldDeltaTime;
                if (timer >= currentStreamRate)
                {
                    int rnd = Randomizer.Rand(0, SFXs.Length);
                    owner.PlaySoundRequest(SFXs[rnd]);

                    Collider[] colliders = Physics.OverlapSphere(owner.transform.position, EffectRange, mask);

                    for (int i = 0; i < colliders.Length; ++i)
                    {
                        Entity entity = colliders[i].gameObject.GetComponent<Entity>();
                        if (entity != null && entity != owner)
                        {
                            entity.entityControl?.KnockBack(entity.transform.position - owner.transform.position, KnockBack);
                            this.PlayWeaponHitSoundRequest(entity);
                            entity.ExecuteVFXOnPositionRequest(this, VFXs[1], entity.chestTransform.position, null);
                            ApplyDamageAndEffectsTo(entity, Damages, Effects);
                        }
                    }

                    timer = 0;
                }

                yield return null;
            }

            yield return waitForEndTime;

            EndAbility(AbilityEndMode.Classic);
        }

        public override void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
        {
            base.EndAbility(abilityEndMode, generateRessource);

            if (ownerCharacter.playerControl.LockGroundCheck)
                ownerCharacter.playerControl.LockGroundCheck = false;

            currentMainVfx?.Stop();
        }

        private IEnumerator LockGroundCheck()
        {
            ownerCharacter.playerControl.LockGroundCheck = true;
            yield return new WaitForSeconds(CurrentChannelTime);

            if (ownerCharacter.playerControl.LockGroundCheck)
                ownerCharacter.playerControl.LockGroundCheck = false;
        }
    }
}
