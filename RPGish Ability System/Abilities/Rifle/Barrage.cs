using Assets.BattleGame.Scripts.Controllers;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.Abilities
{
    public class Barrage : SimpleRifleShot
    {
        public int BarrageShotsCout = 9;
        public int IncrementAngle = 6;

        public override void StartAnimationAction()
        {
            ownerCharacter.characterAnimationSystem.Attack(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).animTriggers[0]);
        }

        public override void EndAnimationAction()
        {
        }

        protected override IEnumerator SkillLoop(Vector3 target)
        {            
            if (owner.IsLocalCharacter)
            {
                CameraShaker.Instance.Shake(camShakePreset);
            }

            int mask = LayerMask.GetMask("Entities", "Walls", "Ground");

            // POUR FAIRE PLUSIEURS TIRS, PASSER LA POSITION VISEE DU CLIENT AU SERVEUR (voir Simple Rifle Shot)

            //for (int i = 0; i < ShotsCount; ++i)
            //{
            owner.PlaySoundRequest(SFXs[0]);

            int angleStart = -Mathf.RoundToInt((float)(BarrageShotsCout * IncrementAngle) / 2f);

            List<Entity> hittedTargets = new List<Entity>();
            List<Vector3> hittedPoints = new List<Vector3>();

            for (int j = 0; j < BarrageShotsCout; ++j)
            {
                Vector3 axis = target - GunEndMain.position;
                //Vector3 direction = axis.RotateVector(Vector3.up, angleStart + j * IncrementAngle);
                Vector3 direction = Quaternion.AngleAxis(angleStart + j * IncrementAngle, Vector3.up) * axis;
                Debug.DrawRay(GunEndMain.position, direction, Color.yellow);

                RaycastHit hit;
                if (Physics.SphereCast(GunEndMain.position, ShotRadius, direction, out hit, CurrentRange, mask))
                {
                    Debug.DrawLine(GunEndMain.position, hit.point, Color.green);

                    Entity hitEntity = null;

                    if (hit.collider.TryGetEntityFromCollider(out hitEntity))
                    {
                        hittedTargets.Add(hitEntity);
                        hittedPoints.Add(hit.point);
                    }
                }
                else
                {
                    Debug.DrawRay(GunEndMain.position, target - GunEndMain.position, Color.red);
                }

                DisplayShotVFX(GunEndMain.position, direction.normalized * CurrentRange + GunEndMain.position);
            }

            if (hittedTargets.Count > 0)
            {
                StartCoroutine(Hit(hittedTargets, hittedPoints));
            }
           
            yield return waitForEndTime;

            EndAbility();
        }

        private IEnumerator Hit(List<Entity> hittedTargets, List<Vector3> hittedPoints)
        {
            yield return new WaitForSeconds(GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[0]);

            for (int t = 0; t < hittedTargets.Count; ++t)
            {
                owner.ExecuteVFXOnPositionRequest(this, VFXs[0], hittedPoints[t], null);
                ApplyDamageAndEffectsTo(hittedTargets[t], Damages, Effects);
            }
        }
    }
}
