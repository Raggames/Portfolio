using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Targeting;
using System.Collections;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class RifleFiller : RifleAbility
    {
        [Header("---- RIFLE FILLER ----")]
        public int EnergyGenerated = 15;
        public float ShotRadius = .35f;

        public override void StartAnimationAction()
        {
            if (owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = true;

            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
            base.EndAnimationAction();

            if (owner.IsMine)
                ownerCharacter.aimingSubsystem.IsAiming = false;
        }

        public override Vector3 GetFreeAimingPoint()
        {
            return ownerCharacter.targetingSystem.GetFreeAimingPoint(Camera.main.transform.position, Camera.main.transform.forward, CurrentRange + CameraOperator.Instance.CamToPlayerDistance, true);
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {
            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            int mask = LayerMask.GetMask("Entities", "Walls", "Ground");

            RaycastHit hit;
            if (Physics.SphereCast(GunEndMain.position, ShotRadius, target - GunEndMain.position, out hit, CurrentRange, mask))
            {
                Debug.DrawLine(GunEndMain.position, hit.point, Color.green);
                Entity hitEntity = null;

                if (hit.collider.TryGetEntityFromCollider(out hitEntity))
                {
                    if (VFXs.Length > 0 && VFXs[0] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[0], GunEndMain.position, (fx) => fx.transform.LookAt(target));
                    }

                    DisplayShotVFX(GunEndMain.position, hit.point);

                    if (VFXs.Length > 1 && VFXs[1] != null)
                    {
                        owner.ExecuteVFXOnPositionRequest(this, VFXs[1], hit.point, null);
                    }

                    if (GameServer.IsMaster)
                    {
                        ApplyDamageAndEffectsTo(hitEntity, Damages, Effects);
                        owner.ressourceSystem.Request_AddRessource(Ressource.RessourceType.Energy, EnergyGenerated);
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

                Debug.DrawRay(GunEndMain.position, target - GunEndMain.position, Color.red);
            }

            // Ending time
            yield return waitForEndTime;

            EndAbility();
        }
    }
}

