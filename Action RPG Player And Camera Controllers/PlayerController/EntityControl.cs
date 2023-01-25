using Assets.SteamAndMagic.Scripts.Managers;
using Photon.Pun;
using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using System;
using System.Collections;
using UnityEngine;

public enum MovementType
{
    Normal,
    Charge,
    Dash,
    Teleport,
    Jump,
    Fly,
    Roll
}

public class EntityControl : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("--------- ENTITY CONTROL ----------")]

    public Entity Owner;
    public CharacterController characterController;
    public AnimatorComponent animatorComponent;

    public bool IsDoingSkill
    {
        get { return Owner.IsExecutingSkill; }
    }

    [Header("--------- VARIABLES ----------")]
    public bool HasControl = true;

    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsArmed = true;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsMoving = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsStrafing = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsJumping = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsFalling = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsSwimming = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsBlocking = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsDashing = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool IsRolling = false;
    [FoldoutGroup("---- BOOLEAN VARIABLES ----")] public bool LockGroundCheck = false;

    /// <summary>
    /// Vitesse globale de l'animator, peut-être modifiée pour appliquer un effet de slow par exemple
    /// </summary>
    [FoldoutGroup("---- FLOAT VARIABLES ----")] public float AnimationSpeed = 1f;
    [FoldoutGroup("---- FLOAT VARIABLES ----")] public float LookAtSpeed = 5f;

    protected float animX;
    protected float animY;
    protected float impactDecay = 5;
    protected Vector3 impactVector;
    protected Vector3 currentImpactVector;

    #region Events

    public delegate void OnStartMoveHandler(Entity entity, MovementType movementType);
    public static event OnStartMoveHandler OnStartMoveEntity;

    public delegate void OnEndMoveHandler(Entity entity, MovementType movementType);
    public static event OnEndMoveHandler OnEndMoveEntity;

    #endregion

    protected virtual void Awake()
    {
        animatorComponent = GetComponent<AnimatorComponent>();
        characterController = GetComponent<CharacterController>();
    }

    protected virtual void LateUpdate()
    {
        impactVector = Vector3.SmoothDamp(impactVector, Vector3.zero, ref currentImpactVector, impactDecay * Time.deltaTime);
    }

    #region Event methods and callbacks

    protected void TriggerOnStartMoveEntity(MovementType movementType = MovementType.Normal)
    {
        OnStartMoveEntity?.Invoke(Owner, movementType);
    }

    protected void TriggerOnEndMoveEntity(MovementType movementType = MovementType.Normal, bool sync = false)
    {
        OnEndMoveEntity?.Invoke(Owner, movementType);

        if (sync)
        {
            photonView.RPC("RPC_OnEndMoveEntity", RpcTarget.All, (int)movementType);
            PhotonNetwork.SendAllOutgoingCommands();
        }
    }

    [PunRPC]
    private void RPC_OnEndMoveEntity(int movetype)
    {
        OnEndMoveEntity?.Invoke(Owner, (MovementType)movetype);
    }

    protected void TriggerOnStartDashEntity(bool sync = false)
    {
        Debug.LogError("Voir pour la synchro de ça...");

        OnStartMoveEntity?.Invoke(Owner, MovementType.Dash);

        if (sync)
        {

        }
    }

    protected void TriggerOnEndDashEntity(bool sync = false)
    {
        OnEndMoveEntity?.Invoke(Owner, MovementType.Dash);

        if (sync)
        {

        }
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)Math.Ceiling(RMath.Map(animX, -1, 1, 0, 255)));
            stream.SendNext((byte)Math.Ceiling(RMath.Map(animY, -1, 1, 0, 255)));
            stream.SendNext((byte)(IsMoving ? 1 : 0));
        }
        else
        {
            animX = (byte)stream.ReceiveNext();
            animY = (byte)stream.ReceiveNext();
            byte moving = (byte)stream.ReceiveNext();
            bool isMoving = moving == 1 ? true : false;

            animX = RMath.Map(animX, 0, 255, -1, 1);
            animY = RMath.Map(animY, 0, 255, -1, 1);

            if (animatorComponent)
            {
                animatorComponent.SetFloat("X", animX, 0, false);
                animatorComponent.SetFloat("Y", animY, 0, false);
                animatorComponent.SetBool("Moving", isMoving, false);
            }
        }
    }

    #endregion

    #region Actions

    public void JumpTo(Vector3 destination, float speed, float heigthRatio, float lag, Action endJumpCallback)
    {
        IsJumping = true;

        // Vérifier si la destination est atteignable avant.
        // Si non, trouver la plus lointaine position atteignable dans la meme direction ?
        Vector3[] currentTrajectoryPositions = Vector3Utils.GetBezierParabola(transform.position, destination, heigthRatio, 0, 15);
        StartCoroutine(JumpRoutine(currentTrajectoryPositions, speed, lag, endJumpCallback));
    }

    public virtual void EndJump(Action endJumpCallback)
    {
        IsJumping = false;

        endJumpCallback?.Invoke();
        OnEndMoveEntity?.Invoke(Owner, MovementType.Jump);
    }

    public void ChargeTo(Vector3 destination, float speed, Action onEndCallback)
    {
        StopAllCoroutines();
        StartCoroutine(ChargeToRoutine(destination, speed, onEndCallback));
    }

    public virtual void DashTo(Vector3 position, float speed)
    {

    }

    public virtual bool TeleportTo(Vector3 destination, bool los = true)
    {
        return false;
    }

    protected virtual IEnumerator JumpRoutine(Vector3[] positions, float speed, float lag, Action endJumpCallback)
    {
        float timer = 0;

        float trajectoryLength = Vector3Utils.GetTrajectoryDistance(positions);
        float totalTime = trajectoryLength / speed;
        totalTime -= lag; // on enlève le temps de latence du temps total pour rattraper le décalage serveur sur toute la durée du saut

        while (true)
        {
            timer += Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
            if (timer >= totalTime)
            {
                break;
            }
            
            transform.position = Vector3Utils.EvaluatePositionLerp(positions, timer / totalTime);

            yield return null;
        }

        EndJump(endJumpCallback);
    }

    protected virtual IEnumerator ChargeToRoutine(Vector3 destination, float speed, Action onEndCallback)
    {
        float distanceToGo = Vector3.Distance(transform.position, destination);
        float currentDistance = 0;
        Vector3 startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        Vector3 dir = destination - transform.position;
        Debug.DrawLine(startPosition, destination, Color.red, 2f);
        dir.Normalize();
        dir *= speed;

        IsDashing = true;

        // pour l'instant les mouvements sont en autorité client, on verra en prod sous Mirror pour faire ça correctement avec du Server Authority + Client side prediction etc...
        if (Owner.IsLocalCharacter)
        {
            HasControl = false;

            IsMoving = true;
            animatorComponent.SetBool("Sprint", true, true);
            animatorComponent.SetBool("Moving", true, false);

            TriggerOnStartMoveEntity(MovementType.Charge);

            while (currentDistance < distanceToGo)
            {
                animY = 1;
                animatorComponent.SetFloat("Y", animY, 0, false);

                Vector3 increment = dir * Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
                Vector3 move = increment;
                move.y = -10;
                characterController.Move(move);
                currentDistance += increment.magnitude;
                yield return null;
            }

            TriggerOnEndMoveEntity(MovementType.Charge);

            HasControl = true;

            IsMoving = false;
            animatorComponent.SetBool("Moving", IsMoving, false);
            animY = 0;
            animatorComponent.SetFloat("Y", animY, 0, false);
            animatorComponent.SetBool("Sprint", false, true);
        }

        IsDashing = false;

        onEndCallback?.Invoke();
    }

    #endregion

    public void LookTo(Vector3 position)
    {
        Vector3 dir = position - Owner.transform.position;
        dir.y = 0;
        transform.rotation = GetLookRotationTo(position);
    }

    public Quaternion GetLookRotationTo(Vector3 position)
    {
        Vector3 dir = position - Owner.transform.position;
        dir.y = 0;
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    [Button("KnockBack")]
    public virtual void KnockBack(Vector3 knockDirection, float force)
    {
        knockDirection.Normalize();

        impactVector += knockDirection * force;
    }
}

