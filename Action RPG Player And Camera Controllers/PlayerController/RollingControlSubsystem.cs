using Assets.SteamAndMagic.Scripts.Managers;
using Photon.Pun;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Photon;
using SteamAndMagic.Systems.EntitySubsystems;
using System;
using Systems.DamageEffect;
using UnityEngine;

namespace SteamAndMagic.Systems.Control
{
    public class RollingControlSubsystem : EntitySubSystem, IInputDownListenner
    {
        public PlayerControl playerControl = null;

        private bool _isRolling;

        public float RollDistance = 3;
        public float RollSpeed = 15;

        public float DodgeDistance = 3;
        public float DodgeSpeed = 12;

        public float Cooldown = .5f;
        public float CurrentCooldownTime = 0;

        public int StaminaCost = 10;

        protected override void Awake()
        {
            base.Awake();

            playerControl = Owner.GetComponent<PlayerControl>();
        }

        private void OnEnable()
        {
            InputsManager.AddCallbackTarget(this, null, null);
            RollingControlSubsystemEventHandler.GetRollingSubsystem += RollingControlSubsustemEventHandler_GetRollingSubsystem;
            RollingControlSubsystemEventHandler.OnRollingDodgeAttack += RollingControlSubsystemEventHandler_OnRollingDodgeAttack;
        }

        private void OnDisable()
        {
            InputsManager.RemoveCallbackTarget(this, null, null);
            RollingControlSubsystemEventHandler.GetRollingSubsystem -= RollingControlSubsustemEventHandler_GetRollingSubsystem;
            RollingControlSubsystemEventHandler.OnRollingDodgeAttack -= RollingControlSubsystemEventHandler_OnRollingDodgeAttack;
        }

        public bool IsRolling
        {
            get
            {
                return _isRolling;
            }
            set
            {
                if (NetworkServer.IsOffline)
                    _isRolling = value;
                else
                {
                    _isRolling = value;
                    RollingControlSubsystemEventHandler.RollingStateChangedRequest(Owner, _isRolling);

                    photonView.RPC("RPC_SyncRolling", RpcTarget.Others, value);
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }

        [PunRPC]
        private void RPC_SyncRolling(bool state)
        {
            _isRolling = state;

            RollingControlSubsystemEventHandler.RollingStateChangedRequest(Owner, _isRolling);
        }

        private void RollingControlSubsustemEventHandler_GetRollingSubsystem(Entity owner, Action<RollingControlSubsystem> resultCallback)
        {
            if (Owner == owner)
                resultCallback.Invoke(this);
        }

        public bool CanRollOrBlock()
        {
            return Owner.IsLocalCharacter
                && Owner.HasWeaponOut
                && playerControl.HasControl
                && !playerControl.IsFalling
                && !playerControl.IsJumping
                && !IsRolling
                && CurrentCooldownTime <= 0
                && Owner.CanDoMovement
                && !Owner.IsExecutingSkill // ??????????
                && !Owner.abilityController.IsPendingLaunch
                && Owner.ressourceSystem.CheckRessource(StaminaCost, Ressource.RessourceType.Stamina);
        }

        public void OnInputDown(InputAction inputAction)
        {
            if (inputAction.actionType != ActionType.RollsMode
                || !CanRollOrBlock())
            {
                return;
            }

            // TODO Server check pour les cancel d'ability en cast ou en execution

            IsRolling = true;

            Owner.ressourceSystem.ReservateRessource(StaminaCost, Ressource.RessourceType.Stamina);

            Vector2 inputs = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (playerControl.IsMoving)
            {
                float AX = Mathf.Abs(inputs.x);
                float AY = Mathf.Abs(inputs.y);

                // Front/Back
                if (AY > .1f && AX <= .1f)
                {
                    if (inputs.y > 0)
                    {
                        Vector3 dir = Camera.main.transform.forward;
                        dir.y = 0;
                        playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                        (Owner as Character).characterAnimationSystem.RollForward();
                        Owner.PlayVoiceShoutRequest(true);
                    }
                    else
                    {
                        Vector3 dir = -Camera.main.transform.forward;
                        dir.y = 0;
                        playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                        transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                        (Owner as Character).characterAnimationSystem.RollBackward();
                        Owner.PlayVoiceShoutRequest(true);
                    }
                }
                // Diags
                else if (AY > .1f && AX > .1f)
                {
                    // Front diags
                    if (inputs.y > 0)
                    {
                        Vector3 dir = Camera.main.transform.forward;
                        dir.y = 0;
                        dir = Quaternion.AngleAxis(inputs.x > 0 ? 45 : -45, Vector3.up) * dir;
                        playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                        (Owner as Character).characterAnimationSystem.RollForward();
                        Owner.PlayVoiceShoutRequest(true);
                    }
                    // Back diags
                    else
                    {
                        Vector3 dir = -Camera.main.transform.forward;
                        dir.y = 0;
                        dir = Quaternion.AngleAxis(inputs.x > 0 ? -45 : 45, Vector3.up) * dir;
                        playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                        transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                        (Owner as Character).characterAnimationSystem.RollBackward();
                        Owner.PlayVoiceShoutRequest(true);
                    }
                }
                // Rigth/Left
                else if (AY <= .1f && AX > .1f)
                {
                    // Dodge
                    if (Owner.IsBlocking)
                    {
                        // Right
                        if (inputs.x > 0)
                        {
                            Vector3 dir = Camera.main.transform.forward;
                            dir.y = 0;
                            dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
                            playerControl.RollTo(dir * DodgeDistance + transform.position, DodgeSpeed, EndRoll);
                            (Owner as Character).characterAnimationSystem.DodgeRight();
                            Owner.PlayVoiceShoutRequest(true);
                        }
                        // Left
                        else
                        {
                            Vector3 dir = Camera.main.transform.forward;
                            dir.y = 0;
                            dir = Quaternion.AngleAxis(-90, Vector3.up) * dir;
                            playerControl.RollTo(dir * DodgeDistance + transform.position, DodgeSpeed, EndRoll);
                            (Owner as Character).characterAnimationSystem.DodgeLeft();
                            Owner.PlayVoiceShoutRequest(true);
                        }
                    }
                    // Roll
                    else
                    {
                        // Right
                        if (inputs.x > 0)
                        {
                            Vector3 dir = Camera.main.transform.forward;
                            dir.y = 0;
                            dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
                            playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                            (Owner as Character).characterAnimationSystem.RollRight();
                            Owner.PlayVoiceShoutRequest(true);
                        }
                        // Left
                        else
                        {
                            Vector3 dir = Camera.main.transform.forward;
                            dir.y = 0;
                            dir = Quaternion.AngleAxis(-90, Vector3.up) * dir;
                            playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                            (Owner as Character).characterAnimationSystem.RollLeft();
                            Owner.PlayVoiceShoutRequest(true);
                        }
                    }
                }

            }
            else
            {
                Vector3 dir = -Camera.main.transform.forward;
                dir.y = 0;
                playerControl.RollTo(dir * RollDistance + transform.position, RollSpeed, EndRoll);
                transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                (Owner as Character).characterAnimationSystem.RollBackward();
                Owner.PlayVoiceShoutRequest(true);
            }
        }

        private void EndRoll()
        {
            Owner.ressourceSystem.UseReservatedRessource(Ressource.RessourceType.Stamina, StaminaCost);
            CurrentCooldownTime = Cooldown;

            IsRolling = false;
        }

        private void Update()
        {
            if (!Owner.IsLocalCharacter)
                return;

            if (CurrentCooldownTime > 0)
                CurrentCooldownTime -= WorldManager.characterLocalWorldDeltaTime;
        }

        private void RollingControlSubsystemEventHandler_OnRollingDodgeAttack(Entity context, Damage[] absorbedDamages, Effect[] absorbedEffects)
        {
            if (context == Owner)
            {
                Debug.LogError("Roll Dodged !");
            }
        }
    }

    public static class RollingControlSubsystemEventHandler
    {
        public delegate void OnRollingAbsorbedDamagesHandler(Entity context, Damage[] absorbedDamages, Effect[] absorbedEffects);
        public static event OnRollingAbsorbedDamagesHandler OnRollingDodgeAttack;
        public static void OnRollingDodgeAttackRequest(Entity context, Damage[] absorbedDamages, Effect[] absorbedEffects) => OnRollingDodgeAttack?.Invoke(context, absorbedDamages, absorbedEffects);

        public delegate void GetRollingSubsystemHandler(Entity owner, Action<RollingControlSubsystem> resultCallback);
        public static event GetRollingSubsystemHandler GetRollingSubsystem;
        public static void GetRollingSubsystemRequest(Entity owner, Action<RollingControlSubsystem> resultCallback) => GetRollingSubsystem?.Invoke(owner, resultCallback);

        public static event Action<Entity, bool> OnRollingStateChanged;
        public static void RollingStateChangedRequest(Entity owner, bool state) => OnRollingStateChanged?.Invoke(owner, state);
    }
}

