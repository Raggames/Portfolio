using Assets.BattleGame.Scripts.Controllers;
using Assets.SteamAndMagic.Scripts.Managers;
using Photon.Pun;
using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.RiftChat;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections;
using UnityEngine;

public class PlayerControl : EntityControl, IInputDownListenner, IInputUpListenner
{
    public GameObject sub;

    #region Paramétrages
    [Header("---- PARAMETERS ---- ")]
    [FoldoutGroup("----PARAMETERS----")] public float Speed = 6.5f;
    [FoldoutGroup("----PARAMETERS----")] public float SprintSpeed = 10f;
    [FoldoutGroup("----PARAMETERS----")] public float BackRunSpeed = 4f;
    [FoldoutGroup("----PARAMETERS----")] public float Gravity = 14;
    [FoldoutGroup("----PARAMETERS----")] public float JumpSpeed = 5f;
    [FoldoutGroup("----PARAMETERS----")] public float turnSmoothTime = 0.25f;
    [FoldoutGroup("----PARAMETERS----")] public float playerStaticRotationSmoothness = 7f;
    [FoldoutGroup("----PARAMETERS----")] public float playerKeyboardRotationSmoothness = 2f;
    [FoldoutGroup("----PARAMETERS----")] public float stopingDistance = 1f;
    [FoldoutGroup("----PARAMETERS----")] public float inputSmoothness = .1f;
    [FoldoutGroup("----PARAMETERS----")] public float fallingAltitudeThreshold = .3f;
    [FoldoutGroup("----PARAMETERS----")] public float slopeLimit = 60; // degrees
    [FoldoutGroup("----PARAMETERS----")] public float leanSmoothTime = .1f;
    [FoldoutGroup("----PARAMETERS----")] public float leanRatio = 5;
    [FoldoutGroup("----PARAMETERS----")] public float maxLeanAngle = 4;
    [FoldoutGroup("----PARAMETERS----")] public float staminaStopWaitingThreshold = 30;
    [FoldoutGroup("----PARAMETERS----")] public float sprintingStaminaCostPerSecond = 20;

    #endregion

    public bool UpdateGravityOnly = false;

    public bool IsRootMotion
    {
        get
        {
            return isRootMotion;
        }
        set
        {
            isRootMotion = value;

            Owner.rootMotionSubsystem.IsActive = value;
            LockGroundCheck = value;
        }
    }

    #region Variables   

    private Character ownerCharacter;
    private Transform cam;
    [ShowInInspector] [ReadOnly] private float currentSpeed;
    [ShowInInspector] [ReadOnly] private float speedCorrectionRatio = 0;
    [ShowInInspector] [ReadOnly] private float horizontal = 0f;
    [ShowInInspector] [ReadOnly] private float vertical = 0f;
    [ShowInInspector] [ReadOnly] private float currentSlopeAngle = 0;
    [ShowInInspector] [ReadOnly] private float targetAngle = 0f;
    private float tempYAngle;
    private float curAngle;
    private float angleDelta;
    private float lastAngleDelta;
    [ShowInInspector] [ReadOnly] private Vector2 smoothInput;
    [ShowInInspector] [ReadOnly] private Vector3 moveDirection = Vector3.zero;
    private bool isFallingRecovery = false;
    private bool isSprinting = false;
    private bool isWaitingForEndurance = false;
    private bool isWallStop = false;
    private bool isSlopeStop = false;
    private bool isRunningBack = false;
    private bool sprinting_temp = false;
    private bool isJumpingToPosition = false;
    [ShowInInspector] private bool isRootMotion = false;
    private float refTargetLeanAngle;
    private float verticalSpeed = 0f;
    private float distFromGround = 0;
    private float previousdistFromGround = 0;
    private float turnSmoothVelocity;
    private Vector2 smoothInputDamp;
    private float leanAngleDifference;
    private int movementType = 0;
    private int stopingMask = 0;

    private float CurrentSpeed
    {
        get
        {
            return currentSpeed * (1f + Owner.CurrentSpeedAttribute / 100f);
        }
        set
        {
            currentSpeed = value;
        }
    }

    private float currentMaxSpeed { get; set; }

    public float MaxStamina
    {
        get
        {
            return Owner.ressourceSystem.GetGameRessource(SteamAndMagic.Systems.Ressource.RessourceType.Stamina).Max;
        }
    }

    [ReadOnly]
    public float Stamina
    {
        get
        {
            return Owner.ressourceSystem.GetGameRessource(SteamAndMagic.Systems.Ressource.RessourceType.Stamina).Current;
        }
    }

    #endregion


    public override void OnEnable()
    {
        base.OnEnable();
        InputsManager.AddCallbackTarget(this, this, null);
        FallingStateBehaviour.IsFalling += FallingStateBehaviour_IsFalling;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        InputsManager.RemoveCallbackTarget(this, this, null);
        FallingStateBehaviour.IsFalling -= FallingStateBehaviour_IsFalling;
    }

    private void Start()
    {
        CurrentSpeed = Speed;
        stopingMask = LayerMask.GetMask("Walls");

        Owner = GetComponent<Entity>();
        ownerCharacter = Owner as Character;
        cam = Camera.main.transform;

        ownerCharacter.characterAnimationSystem.Request_DoSimpleAction(SteamAndMagic.Scripts.Control.SimpleAction.ToggleArmed, new object[] { true });

        this.enabled = true;
    }

    private void Update()
    {
        // Server authority, will see later
        if (Owner.IsLocalCharacter && characterController.enabled == true)
        {
            // Routine Jump To executing
            if (isJumpingToPosition)
            {
                return;
            }

            if (UpdateGravityOnly)// || IsRootMotion)
            {
                characterController.Move(Vector3.down * WorldManager.Instance.Gravity);
            }
            else
            {
                if (!IsRolling && !IsDashing && !Owner.IsDead && !LockGroundCheck)
                {
                    //Handle falling and land animations 
                    GroundCheck();
                }

                if (InGameChatController.InChat)
                    return;

                if (!IsRolling)
                {
                    // All the control loop
                    UpdateControl();
                }
            }
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        animatorComponent.animator.speed = AnimationSpeed * Owner.EntityTimeScale;

        bool wasMoving = IsMoving;

        if (characterController && !IsDashing && !IsRootMotion)
        {
            IsMoving = characterController.velocity.magnitude > .1f;

            if (!wasMoving && IsMoving)
            {
                TriggerOnStartMoveEntity();
            }

            if (wasMoving && !IsMoving)
            {
                TriggerOnEndMoveEntity();
            }
        }
        else
        {
            if (wasMoving)
            {
                TriggerOnEndMoveEntity();
                IsMoving = false;
            }
        }
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)Math.Ceiling(RMath.Map(animX, -1, 1, 0, 255)));
            stream.SendNext((byte)Math.Ceiling(RMath.Map(animY, -1, 1, 0, 255)));

            stream.SendNext((byte)(IsMoving ? 1 : 0));
            stream.SendNext((byte)(IsStrafing ? 1 : 0));
            stream.SendNext((byte)(isSprinting ? 1 : 0));
            stream.SendNext((byte)(IsBlocking ? 1 : 0));
        }
        else
        {
            animX = (byte)stream.ReceiveNext();
            animY = (byte)stream.ReceiveNext();
            // Remapping the value from 0?255 to -1/1
            animX = RMath.Map(animX, 0, 255, -1, 1);
            animY = RMath.Map(animY, 0, 255, -1, 1);

            IsMoving = (byte)stream.ReceiveNext() == 1 ? true : false;
            IsStrafing = (byte)stream.ReceiveNext() == 1 ? true : false;
            isSprinting = (byte)stream.ReceiveNext() == 1 ? true : false;
            IsBlocking = (byte)stream.ReceiveNext() == 1 ? true : false;

            if (animatorComponent)
            {
                animatorComponent.SetFloat("X", animX, 0, false);
                animatorComponent.SetFloat("Y", animY, 0, false);
                animatorComponent.SetBool("Moving", IsMoving, false);
                animatorComponent.SetBool("Blocking", IsStrafing, false);
                animatorComponent.SetBool("Sprint", isSprinting, false);
                animatorComponent.SetBool("Blocking", IsBlocking, false);
            }
        }
    }

    private void FallingStateBehaviour_IsFalling(Animator obj, bool state)
    {
        if (obj == animatorComponent.animator)
        {
            isFallingRecovery = state;
        }
    }

    private void GroundCheck()
    {
        int layer_mask = LayerMask.GetMask("Ground");

        RaycastHit hitInfo;

        if (characterController.isGrounded)
        {
            if (IsFalling || isFallingRecovery)
            {
                //Debug.Log("Land");

                StartLand();
                IsFalling = false;
                isFallingRecovery = false;
            }
        }
        else
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hitInfo, layer_mask))
            {
                distFromGround = Vector3.Distance(transform.position, hitInfo.point);

                if (!IsFalling && distFromGround >= fallingAltitudeThreshold || distFromGround < previousdistFromGround)
                {
                    //Debug.Log("Fall");

                    StartFall();
                    IsFalling = true;
                }

                previousdistFromGround = distFromGround;
            }
        }

        // SI !IsFlying et que ground check dit l'inverse, on passe inflying et on trigger le flying loop
    }

    private void UpdateControl()
    {
        //Cursor.visible = true;
        //SetCursorVisibility();

        float timeFactor = WorldManager.Instance.GetTimeFactor(Owner.Team);

        isRunningBack = false;
        movementType = -1;

        //SetCursorVisibility();

        // Getting inputs *********************************************
        GettingInputs();

        // Handling sprint endurance etc **************************************
        HandleSprint();

        // Vertical speed and gravity *********************************************************************
        if (!characterController.isGrounded)
            verticalSpeed -= WorldManager.Instance.Gravity * timeFactor * Time.deltaTime;

        // Computing inputs modes *************************************************************************
        ComputingMovement();

        // Computing force stop conditions *****************************************************************
        UpdateWallStop();

        // Preparing animator values depending on mode ******************************************************
        UpdatingAnimationVariables();

        /// Applying movement ***************************************************
        ApplyingMovementToController();

        // Updating lean angle
        UpdatingLeanAngle();
    }

    private void GettingInputs()
    {
        if (!HasControl || !Owner.CanDoMovement || IsRootMotion)
        {
            horizontal = 0;
            vertical = 0;
            smoothInput = Vector2.zero;

            return;
        }

        // ********************
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        smoothInput = Vector2.SmoothDamp(smoothInput, new Vector2(horizontal, vertical), ref smoothInputDamp, inputSmoothness);

        if (Input.GetButton("Jump"))
        {
            if (characterController.isGrounded && !IsJumping && !IsDoingSkill)
            {
                ownerCharacter.characterAnimationSystem.DoStartJump(true);
                IsJumping = true;
            }
        }
    }

    private void HandleSprint()
    {
        if (!HasControl || !Owner.CanDoMovement || IsRootMotion)
            return;

        if (!isWaitingForEndurance)
        {
            isSprinting = Input.GetKey(KeyCode.LeftShift);
        }
        else
        {
            if (isSprinting)
            {
                if (Input.GetKeyUp(KeyCode.LeftShift))
                    isSprinting = false;
            }
        }

        if (Stamina < 10)
        {
            isWaitingForEndurance = true;
        }

        if (Stamina <= 5)
            isSprinting = false;

        if (isSprinting)
        {
            if (IsMoving)
            {
                if (!sprinting_temp)
                {
                    CameraOperator.Instance.SetFowTween(80, .2f);
                    sprinting_temp = true;
                }

                if (Stamina > 0)
                {
                    // Pas ouf, revoir en utilisation le OnStream
                    Owner.ressourceSystem.Request_UseRessource(SteamAndMagic.Systems.Ressource.RessourceType.Stamina, WorldManager.characterLocalWorldDeltaTime * sprintingStaminaCostPerSecond);
                }
            }
        }
        else
        {
            if (sprinting_temp)
            {
                CameraOperator.Instance.SetFowTween(CameraOperator.Instance.normalFow, .2f);
                sprinting_temp = false;
            }

            if (isWaitingForEndurance && Stamina > staminaStopWaitingThreshold)
                isWaitingForEndurance = false;
        }
    }

    private void UpdatingLeanAngle()
    {
        curAngle = RMath.WrapAngle(transform.localEulerAngles.y);
        angleDelta = tempYAngle - curAngle;
        angleDelta = Mathf.Clamp(angleDelta, -maxLeanAngle, maxLeanAngle);
        tempYAngle = curAngle;
        // Angle du personnage lorsqu'il tourne
        // Multiplier = 0 si statique, 1 si run forward, -1 si run back
        float ratio = CurrentSpeed / currentMaxSpeed;

        angleDelta = Mathf.SmoothDamp(lastAngleDelta, angleDelta, ref refTargetLeanAngle, leanSmoothTime);
        lastAngleDelta = angleDelta;

        float angle = angleDelta * leanRatio * ratio;

        if (Mathf.Abs(smoothInput.x) > 0.02f)
        {
            if (Mathf.Abs(smoothInput.y) > .02f)
            {
                if (smoothInput.y >= 0.02f || isSprinting)
                {
                    sub.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(smoothInput.y) * angle);
                }
                else
                {
                    sub.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(smoothInput.y) * -angle);
                }
            }
            else
            {
                sub.transform.localRotation = Quaternion.Euler(smoothInput.x * angle, 0, 0);
            }
        }
        else
        {
            if (smoothInput.y >= 0.02f || isSprinting)
            {
                sub.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(smoothInput.y) * angle);
            }
            else
            {
                sub.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(smoothInput.y) * -angle);
            }
        }
    }

    private void ApplyingMovementToController()
    {
        if (!isSlopeStop)
        {
            CurrentSpeed = isSprinting ? SprintSpeed : Speed;
            currentMaxSpeed = CurrentSpeed;

            moveDirection.Normalize();

            // Calcul de la vitesse de déplacement
            if (!isWallStop && !isSlopeStop)
            {
                speedCorrectionRatio = Mathf.Abs(smoothInput.x) >= Mathf.Abs(smoothInput.y) ? Mathf.Abs(smoothInput.x) : Mathf.Abs(smoothInput.y);
                CurrentSpeed *= speedCorrectionRatio;
                if (isRunningBack)
                {
                    CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0, BackRunSpeed);
                    currentMaxSpeed = BackRunSpeed;
                }
                moveDirection *= CurrentSpeed;
            }

            // Application du mouvement au controleur
            moveDirection.y = verticalSpeed;
            moveDirection += impactVector;
            characterController.Move(moveDirection * Owner.entityDeltaTime);
        }
        else
        {
            moveDirection = new Vector3(0, verticalSpeed, 0);
            moveDirection += impactVector;
            characterController.Move(moveDirection * Owner.entityDeltaTime);
        }
    }

    private void UpdatingAnimationVariables()
    {
        animX = smoothInput.x;
        animY = smoothInput.y;

        // Déplacement Normal
        if (movementType == 0)
        {
            float absInput = Mathf.Abs(animX) + Mathf.Abs(animY);
            absInput = Mathf.Clamp(absInput, 0, 1f);
            animX = 0;
            animY = isRunningBack ? -absInput : absInput;
        }
        // Deplacement libre clavier
        else if (movementType == 1)
        {
            animX = 0;
        }
        // Sprint
        else if (movementType == 2)
        {
            animY = Mathf.Abs(smoothInput.y) + Mathf.Abs(smoothInput.x);
            animY = Mathf.Clamp(animY, 0, 1f);
            animX = 0;
        }

        // Pour éviter des synchronisation de valeurs minimes qui pompent du réseau
        if (Mathf.Abs(animX) < 0.05f)
            animX = 0;
        if (Mathf.Abs(animY) < 0.05f)
            animY = 0;

        // Applying value to animator
        // These values are general for all entities and are synced in the base class
        animatorComponent.SetBool("Moving", IsMoving, false); // géré par onserialize
        animatorComponent.SetFloat("X", animX, 0, false); // géré par onserialize
        animatorComponent.SetFloat("Y", animY, 0, false); // géré par onserialize
        animatorComponent.SetBool("Sprint", isSprinting, false); // géré par onserialize
        animatorComponent.SetBool("Blocking", IsBlocking, false); // géré par onserialize
        animatorComponent.SetBool("IsStrafe", IsStrafing, false); // géré par onserialize

    }

    private void UpdateWallStop()
    {
        int mask = 1 << 11;
        mask = ~mask;
        // On crée un point d'origine devant le personnage dans sa direction de déplacement
        Vector3 slopeCheckerOrigin = transform.position + Vector3.up * 2 + moveDirection.normalized;
        RaycastHit hit2;
        // on vérifie que le joueur ne se situe pas sur un rebord en raycastant sous ses pieds
        Debug.DrawRay(slopeCheckerOrigin, Vector3.down, Color.yellow);
        if (Physics.Raycast(slopeCheckerOrigin, Vector3.down, out hit2, 25, mask))
        {
            if (hit2.collider.gameObject.CompareTag("World"))
            {
                currentSlopeAngle = Vector3.Angle(Vector3.up, hit2.normal);
                isSlopeStop = currentSlopeAngle > slopeLimit;
            }
        }

        // Should the character stop (before a wall or some obstacle)
        isWallStop = Physics.Raycast(transform.position + new Vector3(0, 1, 0), moveDirection.normalized, stopingDistance, stopingMask);
        Debug.DrawRay(transform.position + new Vector3(0, 1, 0), moveDirection.normalized, Color.black);
        if (isWallStop)
        {
            horizontal = 0;
            vertical = 0;
            moveDirection = Vector3.zero;
        }

        if (IsDashing)
            moveDirection = Vector3.zero;
    }

    private void ComputingMovement()
    {
        if (IsRootMotion)
        {
            UpdateCharacterRotation();
        }
        else
        {
            if (isSprinting && smoothInput.magnitude > .01f)
            {
                movementType = 2;

                targetAngle = Mathf.Atan2(smoothInput.x, smoothInput.y) * Mathf.Rad2Deg;
                //float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);            
                moveDirection = Quaternion.Euler(0f, targetAngle + Camera.main.transform.localEulerAngles.y, 0f) * Vector3.forward;
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

                return;
            }

            IsStrafing = Mathf.Abs(smoothInput.x) > .01f;
            if (IsStrafing || Mathf.Abs(smoothInput.y) > .01f)
            {
                if (IsStrafing)
                {
                    movementType = 3;
                    moveDirection = new Vector3(horizontal, 0, vertical);
                    targetAngle = cam.eulerAngles.y;

                    if (vertical < 0)
                        isRunningBack = true;

                    //targetAngle = (180 - Vector3.SignedAngle((Owner.transform.position - MouseManager.Instance.GetAimedPoint(100)), Vector3.forward, Vector3.up)) % 360;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * new Vector3(horizontal, 0, vertical);
                }
                else
                {
                    movementType = 0;
                    moveDirection = new Vector3(horizontal, 0, vertical);

                    if (vertical > 0)
                    {
                        targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                        targetAngle += cam.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                    }
                    else if (vertical < 0)
                    {
                        targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg - 180;
                        targetAngle += cam.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.back;
                        isRunningBack = true;
                    }
                }
            }
            else
            {
                UpdateCharacterRotation();
            }
        }
    }

    private void UpdateCharacterRotation()
    {
        if (ownerCharacter.IsPrevisualizingAbility)
        {
            if (Owner.abilityController.CurrentAimedTarget != Owner.transform.position
                && Owner.abilityController.CurrentAimedTarget != new Vector3(-10000, -10000, -10000))
            {
                Quaternion lookRotation = GetLookRotationTo(Owner.abilityController.CurrentAimedTarget);
                transform.rotation = lookRotation; // Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * playerStaticRotationSmoothness);
            }
        }
        else if (ownerCharacter.IsAiming)
        {
            if (ownerCharacter.aimingSubsystem.CurrentAimedTarget != Owner.transform.position)
            {
                Quaternion lookRotation = GetLookRotationTo(ownerCharacter.aimingSubsystem.CurrentAimedTarget);
                transform.rotation = lookRotation;  //Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * playerStaticRotationSmoothness);
            }
        }
        else if (IsRootMotion || Input.GetKey(KeyCode.Mouse1))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, cam.eulerAngles.y, 0), Time.deltaTime * playerStaticRotationSmoothness);
        }
    }

    public void SetGlobalSpeed(float wantedSpeed)
    {
        float ratio = wantedSpeed / CurrentSpeed;
        AnimationSpeed = ratio;
    }

    #region Saut et Chute Libre
    public void ExecuteJump()
    {
        if (isJumpingToPosition)
            return;

        verticalSpeed = JumpSpeed * WorldManager.Instance.GetTimeFactor(Owner.Team);
    }

    public void StartFall()
    {
        ownerCharacter.characterAnimationSystem.DoStartFall(true);
    }

    public void StartLand()
    {
        ownerCharacter.characterAnimationSystem.DoLand(true);
        IsJumping = false;
    }

    #endregion

    #region Mouvements
    public override void KnockBack(Vector3 knockDirection, float force)
    {
        if (Owner.IsMine)
            base.KnockBack(knockDirection, force);
    }

    public override void DashTo(Vector3 position, float speed)
    {
        StopAllCoroutines();
        StartCoroutine(DashRoutine(position, speed));
    }

    public override bool TeleportTo(Vector3 destination, bool los = true)
    {
        if (Owner.IsLocalCharacter)
        {
            if (los)
            {
                if (RaycastManager.SphereCastHitAllTo(transform.position + Vector3.up, destination, LayerMask.GetMask("World", "Wall"), .25f).Length != 0)
                {
                    Debug.LogError("Can't teleport here.");
                    return false;
                }
                else
                {
                    StartCoroutine(TeleportRoutine(destination));
                    return true;
                }
            }
            else
            {
                StartCoroutine(TeleportRoutine(destination));
                return true;
            }
        }

        return false;

    }

    public void RollTo(Vector3 position, float speed, Action onEndCallback)
    {
        StopAllCoroutines();
        StartCoroutine(RollRoutine(position, speed, onEndCallback));
    }

    protected override IEnumerator JumpRoutine(Vector3[] positions, float speed, float lag, Action endJumpCallback)
    {
        float timer = 0;

        float trajectoryLength = Vector3Utils.GetTrajectoryDistance(positions);
        float totalTime = trajectoryLength / speed;
        totalTime -= lag; // on enlève le temps de latence du temps total pour rattraper le décalage serveur sur toute la durée du saut

        while (true)
        {
            isJumpingToPosition = true;

            timer += Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
            if (timer >= totalTime)
            {
                break;
            }
            transform.position = Vector3Utils.EvaluatePositionLerp(positions, timer / totalTime);

            yield return null;
        }

        isJumpingToPosition = false;

        EndJump(endJumpCallback);
    }

    protected IEnumerator TeleportRoutine(Vector3 destination)
    {
        Debug.Log("Teleported by " + Vector3.Distance(transform.position, destination).ToString() + " meters.");

        HasControl = false;
        Vector3 cameraRelative = Camera.main.transform.position - transform.position;
        characterController.enabled = false;
        Owner.transform.position = destination;
        CameraOperator.Instance.ForcePosition(transform.position + cameraRelative);
        yield return null;
        characterController.enabled = true;
        HasControl = true;
    }

    protected IEnumerator DashRoutine(Vector3 position, float speed)
    {
        float distanceToGo = Vector3.Distance(transform.position, position);
        float currentDistance = 0;
        Vector3 startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        Vector3 dir = position - transform.position;
        Debug.DrawLine(startPosition, position, Color.red, 2f);
        dir.Normalize();
        dir *= speed;

        IsDashing = true;

        // pour l'instant les mouvements sont en autorité client, on verra en prod sous Mirror pour faire ça correctement avec du Server Authority + Client side prediction etc...
        if (Owner.IsLocalCharacter)
        {
            TriggerOnStartDashEntity();

            while (currentDistance < distanceToGo)
            {
                Vector3 increment = dir * Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
                characterController.Move(increment);
                currentDistance += increment.magnitude;
                yield return null;
            }
            TriggerOnEndDashEntity();
        }

        IsDashing = false;
    }

    protected IEnumerator RollRoutine(Vector3 position, float speed, Action onEndCallback)
    {
        float distanceToGo = Vector3.Distance(transform.position, position);
        float currentDistance = 0;
        Vector3 startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        Vector3 dir = position - transform.position;
        Debug.DrawLine(startPosition, position, Color.red, 2f);
        dir.Normalize();
        dir *= speed;

        IsRolling = true;

        // pour l'instant les mouvements sont en autorité client, on verra en prod sous Mirror pour faire ça correctement avec du Server Authority + Client side prediction etc...
        if (Owner.IsLocalCharacter)
        {
            TriggerOnStartMoveEntity(MovementType.Roll);

            while (currentDistance < distanceToGo)
            {
                float tInc = Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
                Vector3 increment = dir * tInc;
                characterController.Move(increment + Vector3.down * WorldManager.Instance.Gravity);
                currentDistance += increment.magnitude;

                yield return null;
            }

            TriggerOnEndMoveEntity(MovementType.Roll);
        }

        IsRolling = false;

        onEndCallback?.Invoke();
    }

    #endregion

    #region Inputs

    public void OnInputDown(InputAction inputAction)
    {
        if (!HasControl || !Owner.IsLocalCharacter)
            return;

        if (inputAction.actionType == ActionType.WeaponSwitch && Owner.CanDoAction)
        {
            // Relax, Current Weapon
            if (!IsJumping && !IsDoingSkill && !ownerCharacter.characterAnimationSystem.IsDoingSheathAction)
            {
                ownerCharacter.characterAnimationSystem.Request_DoSimpleAction(SteamAndMagic.Scripts.Control.SimpleAction.ToggleArmed, new object[] { !IsArmed });
            }
        }
    }

    public void OnInputUp(InputAction inputAction)
    {
        /*if (!HasControl)
            return;*/
    }

    #endregion

    #region Old rpg move
    /*if (RPGCONTROLS)
        {
            if (Mathf.Abs(smoothInput.x) != 0 || Mathf.Abs(smoothInput.y) != 0)
            {
                if (!Owner.IsAiming)
                {
                    if (IsStrafing)
                    {
                        animatorComponent.SetBool("IsStrafe", false, true);
                        IsStrafing = false;
                    }

                    // move with mouse hold
                    if (mouseMoveForward)
                    {
                        movementType = 2;

                        targetAngle = cam.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                    }
                    else
                    {
                        bool mouse1 = false;
                        mouse1 = Input.GetKey(KeyCode.Mouse1);
                        bool mouse0 = false;
                        mouse0 = Input.GetKey(KeyCode.Mouse0);
                        // Player follow cam direction
                        if (mouse1)
                        {
                            movementType = 0;
                            //Debug.LogError(0);
                            moveDirection = new Vector3(horizontal, 0, vertical);
                            // moveDirection.Normalize();
                            if (vertical >= 0)
                            {
                                targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                                targetAngle += cam.eulerAngles.y;
                                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                            }
                            else
                            {
                                targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg - 180;
                                targetAngle += cam.eulerAngles.y;
                                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.back;
                                runningBack = true;
                            }
                        }
                        // Normal move // Free Orbiting cam + normal move
                        else if (!mouse1 && !mouse0 || mouse0)
                        {
                            movementType = 1;

                            if (vertical > 0)
                            {
                                transform.Rotate(0, Input.GetAxisRaw("Horizontal") * playerKeyboardRotationSmoothness * 100f * Time.deltaTime, 0);
                                moveDirection = transform.TransformDirection(Vector3.forward);
                            }
                            else if (vertical < 0)
                            {
                                transform.Rotate(0, -Input.GetAxisRaw("Horizontal") * playerKeyboardRotationSmoothness * 100f * Time.deltaTime, 0);

                                moveDirection = transform.TransformDirection(Vector3.back);
                                runningBack = true;
                            }

                            if (horizontal != 0 && vertical == 0)
                            {
                                // Pour eviter un redemarrage brutal en passant de rotation statique a avancée.
                                smoothInput.x = 0;

                                transform.Rotate(0, Input.GetAxisRaw("Horizontal") * playerKeyboardRotationSmoothness * 100f * Time.deltaTime, 0);
                            }
                        }
                    }
                }
                else
                {
                    movementType = 3;

                    if (!IsStrafing)
                    {
                        animatorComponent.SetBool("IsStrafe", true, true);
                        IsStrafing = true;
                    }

                    // Pendant Ciblage, Strafe mode ?
                    moveDirection = new Vector3(horizontal, 0, vertical);
                    targetAngle = cam.eulerAngles.y;

                    if (vertical < 0)
                        runningBack = true;

                    //targetAngle = (180 - Vector3.SignedAngle((Owner.transform.position - MouseManager.Instance.GetAimedPoint(100)), Vector3.forward, Vector3.up)) % 360;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * new Vector3(horizontal, 0, vertical);
                }

                leanAngleMultiplier = runningBack ? -1 : 1;
            }
            else
            {
                if (OwnerCharacter.IsAiming)
                {
                    float targetAngle = 180 - Vector3.SignedAngle((Owner.transform.position - Owner.abilityController.GetCamAimPoint(Owner.abilityController.currentAbility.CurrentRange)), Vector3.forward, Vector3.up);
                    targetAngle = targetAngle % 360;

                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetAngle, 0), Time.deltaTime * playerStaticRotationSmoothness);
                }
                else if (Input.GetKey(KeyCode.Mouse1))
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, cam.eulerAngles.y, 0), Time.deltaTime * playerStaticRotationSmoothness);
                }

                //TODO static turn anim
            }
        }*/
    #endregion

    private void OnGUI()
    {
        if (isRootMotion)
        {
            GUI.Label(new Rect(10, 10, 100, 20), "ROOT MOTION ON");
        }
    }
}


