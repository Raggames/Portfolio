using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Targeting;
using System.Collections;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class SimpleRifleShot : RifleAbility
    {
        [Header("---- RIFLE FILLER ----")]
        public float ShotRadius = .35f;
        public int ShotsCount = 1;
        public float KnockbackForce = 0;

        public bool AimingShot = true;
        public bool UseAutoLock = true;

        private object[] shotData = new object[2];

        public override void StartAnimationAction()
        {
            if (AimingShot && owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = true;
        }

        public override void EndAnimationAction()
        {
            if (AimingShot && owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = false;
        }

        public override Vector3 GetFreeAimingPoint()
        {
            return ownerCharacter.targetingSystem.GetFreeAimingPoint(Camera.main.transform.position, Camera.main.transform.forward, CurrentRange + CameraOperator.Instance.CamToPlayerDistance, UseAutoLock);
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            //ownerCharacter.characterAnimationSystem.StartRifleAiming(target, CurrentRange);
            StartCoroutine(Shot(target, 0));

            if (owner.IsMine)
            {
                CameraShaker.Instance.Shake(camShakePreset);

                if(ShotsCount > 1)
                {
                    for (int i = 1; i < ShotsCount; ++i)
                    {
                        float timeBetweenHits = CurrentChannelTime / ShotsCount;
                        yield return new WaitForSeconds(timeBetweenHits);

                        Vector3 posTarget = GetFreeAimingPoint();

                        shotData[0] = i;
                        shotData[1] = posTarget;

                        Client_BroadcastNetworkMessage(shotData);
                    }
                }                
            }
        }

        public override void RPC_BroadcastedMessage(object[] data)
        {
            base.RPC_BroadcastedMessage(data);

            if (data != null && data.Length == 2)
            {
                int shotIndex = (int)data[0];
                Vector3 posTarget = (Vector3)data[1];

                StartCoroutine(Shot(posTarget, shotIndex));
            }
        }

        protected virtual IEnumerator Shot(Vector3 target, int shotIndex)
        {
            int mask = LayerMask.GetMask("Entities", "Walls", "Ground");

            owner.PlaySoundRequest(SFXs[0]);

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);

            Debug.DrawLine(GunEndMain.position, target - GunEndMain.position, Color.blue, 1);

            RaycastHit hit;
            if (Physics.SphereCast(GunEndMain.position, ShotRadius, target - GunEndMain.position, out hit, CurrentRange, mask))
            {
                Entity hitEntity = null;

                if (hit.collider.TryGetEntityFromCollider(out hitEntity))
                {
                    if (VFXs.Length > 0 && VFXs[0] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[0], GunEndMain.position, (fx) => fx.transform.LookAt(target));
                    }

                    // Shot time
                    //yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[1]);
                                       
                    DisplayShotVFX(GunEndMain.position, hit.point);

                    if (VFXs.Length > 1 && VFXs[1] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hit.point, null);
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
                else
                {
                    DisplayShotVFX(GunEndMain.position, target);

                    if (VFXs.Length > 1 && VFXs[1] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hit.point, null);
                    }
                }
            }
            else
            {
                DisplayShotVFX(GunEndMain.position, target);

                Debug.DrawRay(GunEndMain.position, GunEndMain.position + (target - GunEndMain.position).normalized * CurrentRange, Color.red, 1);
            }

            if (GameServer.IsMaster && shotIndex >= ShotsCount - 1)
            {
                yield return waitForEndTime;
                owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
            }
        }

        public override void OnAutoLaunchEnded()
        {
            base.OnAutoLaunchEnded();

            if (SFXs.Length > 1)
            {
                // Reload
                owner.PlaySoundRequest(SFXs[1]);
            }
        }
    }
}
