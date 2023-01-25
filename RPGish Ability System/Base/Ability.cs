using Assets.BattleGame.Scripts.Controllers;
using Assets.SteamAndMagic.Scripts.Managers;
using Incursion.Backend;
using IncursionDAL;
using Photon.Pun;
using RPGCharacterAnims;
using Sirenix.OdinInspector;
using SteamAndMagic.Audio;
using SteamAndMagic.Backend;
using SteamAndMagic.Entities;
using SteamAndMagic.GameplayFX;
using SteamAndMagic.Interface;
using SteamAndMagic.Systems.Abilities;
using SteamAndMagic.Systems.Attributes;
using SteamAndMagic.Systems.Balancing;
using SteamAndMagic.Systems.DamagePhysics;
using SteamAndMagic.Systems.LocalizationManagement;
using SteamAndMagic.Systems.Ressource;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Systems.DamageEffect;
using Systems.DamagesAndEffects;
using UnityEngine;

// [UIAbilityParameter] permet d'afficher automatiquement en UI des fields dans les ability
public class Ability : MonoBehaviour //, IDamager
{
    /*public IDamagerType DamagerType { get => IDamagerType.Ability; }
    public object[] IdentificationData { get => identificationData; }
    private object[] identificationData;*/

    #region Events
    public delegate void OnAbilityStartHandler(Entity launcher, Ability ability);
    public delegate void OnAbilityEndHandler(Entity launcher, Ability ability);
    public delegate void OnAbilityCancelHandler(Entity launcher, Ability ability);
    public delegate void OnAbilityInterruptHandler(Entity launcher, Ability ability);
    public delegate void OnAbilityStateChangeHandler(Entity launcher, Ability ability);
    public delegate void OnAbilityBeforeApplyDamageAndEffectsToHandler(Entity launcher, Entity target, Ability ability, List<Damage> damages, List<Effect> effects);

    public static event OnAbilityStartHandler OnAbilityStart;
    public static event OnAbilityEndHandler OnAbilityEnd;
    public static event OnAbilityCancelHandler OnAbilityCancel;
    public static event OnAbilityInterruptHandler OnAbilityInterrupt;
    public static event OnAbilityStateChangeHandler OnAbilityStateChange;
    /// <summary>
    /// SERVER ONLY / Called before network applying damages and effects
    /// </summary>
    public static event OnAbilityBeforeApplyDamageAndEffectsToHandler OnAbilityBeforeApplyDamageAndEffects;

    protected void OnAbilityStartRequest()
    {
        OnAbilityStart?.Invoke(owner, this);
    }

    public void OnAbilityEndRequest()
    {
        OnAbilityEnd?.Invoke(owner, this);
    }

    public void OnAbilityCancelRequest()
    {
        OnAbilityCancel?.Invoke(owner, this);
    }

    public void OnAbilityInterruptRequest()
    {
        OnAbilityInterrupt?.Invoke(owner, this);
    }

    #endregion

    #region Fields

    [Header("--------- DEPENDANCES ---------")]
    public Entity owner;
    public Character ownerCharacter;
    public AbilityData abilityData; // Référence à la sauvegarde de la carte possédée par le joueur (niveau, XP, etc)
    protected AbilityController abilityController;
    public AbilityController AbilityController => abilityController;

    [Header("--------- CORE ---------")]
    [FoldoutGroup("--------- CORE ---------")] public string Name;

    [Space]
    [FoldoutGroup("--------- CORE ---------")] [Title("Description", bold: false)] [HideLabel] [MultiLineProperty(10)] public string Description; // Descriptif de l'histoire de la carte.
    [Space]

    [FoldoutGroup("--------- CORE ---------")] public Sprite Icon;
    [FoldoutGroup("--------- CORE ---------")] public float Power = 1f; // puissance de l'ability, pour calcul de la puissance du perso
    [FoldoutGroup("--------- CORE ---------")] public float perUseXP = 50f;    // Experience rapportée par chaque utilisation de la carte
    [FoldoutGroup("--------- CORE ---------")] public bool IsCollectionAbility = true; // Does the ability appear in the codex (false for the weapon abilities for example)

    public string corekey { get { return this.name; } }

    [Header("--------- ARBRE DE TALENTS ---------")]

    [FoldoutGroup("--------- TALENTS ---------")] [ListDrawerSettings(CustomAddFunction = "OnAddTalentBranch")] public List<TalentTreeBranch> talentTreeBranches = new List<TalentTreeBranch>();
    public int Level { get { return abilityData != null ? abilityData.totalTalentPoints : 0; } }

    [FoldoutGroup("--------- TALENTS ---------")] public List<Talent> SelectedTalents = new List<Talent>();

    [FoldoutGroup("--------- TALENTS ---------")] public Rune Rune;

    [Header("--------- GESTION RESSOURCES ---------")]
    [FoldoutGroup("--------- RESSOURCE ---------")] public RessourceType costRessourceType;
    [FoldoutGroup("--------- RESSOURCE ---------")] public int costRessourceValue;
    [FoldoutGroup("--------- RESSOURCE ---------")] public int ComboGain = 1;

    /* public RessourceType generateRessourceType;
     [TalentParameter] public int generateRessourceValue;*/

    [Header("--------- PARAMETRAGES DE BASE ---------")]

    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool LaunchableOnSelf = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool LaunchableOnAlly = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool LaunchableOnEnemy = true;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool LaunchableWhileMoving = false;

    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool CanDamageSelf = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool CanDamageAlly = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool CanDamageEnemy = true;

    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool UpdateAimingWhileCasting = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool ShowChanneling = false; // TO DO
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsConstrainedByGCD = true; // le skill est-il soumis au GlobalCooldown Time
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsCancelable = false; // le skill peut-il etre annulé pendant l'execution de sa loop ?
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsPassive = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsBlocable = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsRollingDodgeable = true;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool IsRangeDynamic = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool AutoLaunch = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool UseAutoVoice = true;

    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool ApplyDamageOncePerTarget = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool AllowAutoRotationTowardTarget = true;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public bool UseAbilityDamageNature = false;
    [FoldoutGroup("--------- PARAMETRAGES DE BASE ---------")] public int AutoVoiceChancesPurcent = 30;
    [Space]

    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public RoleType roleType;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityMode AbilityMode;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityLaunchMode ExecutionMode;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityAimingMode AimingMode;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityFlags AbilityFlags;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityCategory AbilityCategory;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public DamageComputationMode WeaponDamageMode;
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public AbilityEquipmentType RequiredEquipmentType;
    /// <summary>
    /// Le nombre d'équipement du type requis pour équiper cette compétence
    /// </summary>
    [FoldoutGroup("--------- PARAMETRAGES ENUMS ---------")] public int RequiredEquipmentTypePoints = -1;
    [HideInInspector] public HashSet<Weapon> AvalaibleWeapon;

    [Space]

    /// <summary>
    /// Liste des paramétrages par arme pour les compétences
    /// </summary>
    [FoldoutGroup("--------- PARAMETRAGE ARMES ---------")] public List<AbilityWeaponParameter> abilityWeaponParameters = new List<AbilityWeaponParameter>();
    private Dictionary<Weapon, AbilityWeaponParameter> abilityWeaponParameter_cache = new Dictionary<Weapon, AbilityWeaponParameter>();

    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] [ShowIf("AbilityMode", AbilityMode.Cooldown)] public float CooldownTime;
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] [ShowIf("AbilityMode", AbilityMode.Cooldown)] public int Charges = 0;

    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] public float CastTime; // Temps d'incantation avant la skill loop
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] public float ChannelTime = 0; // Temps pendant lequel la skill loop est active dans le cas du skill canalisé par exemple. 
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] public float EndTime = 0.3f;
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] public float Range;
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] public float EffectRange;

    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] [ShowIf("AbilityMode", AbilityMode.Heat)] public int MaxHeat = 100;
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] [ShowIf("AbilityMode", AbilityMode.Heat)] public int HeatPerUse = 30;
    [FoldoutGroup("--------- LANCEMENT ET RANGE ---------")] [ShowIf("AbilityMode", AbilityMode.Heat)] public int HeatDissipation = 5;

    [Header("--------- CIBLAGE ---------")]

    [FoldoutGroup("--------- CIBLAGE ---------")] public TargetingArea TargetingArea_prefab;
    [FoldoutGroup("--------- CIBLAGE ---------")] [ReadOnly] public TargetingArea TargetingArea;

    [Header("--------- DOMMAGES ET EFFETS ---------")]

    [FoldoutGroup("--------- DOMMAGES ET EFFETS ---------")] public List<Damage> Damages = new List<Damage>();
    [FoldoutGroup("--------- DOMMAGES ET EFFETS ---------")] public List<Effect> Effects = new List<Effect>();

    [Header("--------- FEEDBACKS ---------")]

    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public CamShakePreset camShakePreset;
    /// <summary>
    /// The sound when you use the ability
    /// </summary>
    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public AudioClipInfo ExecutionSound;

    /// <summary>
    /// The sound when you casting the ability
    /// </summary>
    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public AudioClipInfo CastSound;
    /// <summary>
    /// Potential hit sounds
    /// </summary>
    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public AudioClipInfo[] HitSounds;
    /// <summary>
    /// Other SFXs that could be needed
    /// </summary>
    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public AudioClipInfo[] SFXs;
    /// <summary>
    /// The list of visual effects
    /// </summary>
    [FoldoutGroup("--------- BASE FEEDBACKS ---------")] public VFX[] VFXs;

    [Header("--------- RUNTIME VARIABLES ---------")]

    [ReadOnly] public int CurrentProcs = 0;
    [ReadOnly] public float CurrentRange;
    [ReadOnly] public float CurrentLag = 0;

    [ShowIf("AbilityMode", AbilityMode.Cooldown)] [ReadOnly] public float CurrentCooldown;
    [ShowIf("AbilityMode", AbilityMode.Cooldown)] [ReadOnly] public int CurrentCharges = 0;
    [ShowIf("AbilityMode", AbilityMode.Cooldown)] [ReadOnly] public float CooldownBonusMalus = 0;

    [ShowIf("AbilityMode", AbilityMode.Heat)] [ReadOnly] public float currentHeat = 0;
    [ShowIf("AbilityMode", AbilityMode.Heat)] [ReadOnly] public float currentCost = 0;
    [ShowIf("AbilityMode", AbilityMode.Heat)] [ReadOnly] public float currentHeatDissipation = 0;

    public float CurrentHeat
    {
        get
        {
            return currentHeat;
        }
        set
        {
            currentHeat = value;
            if (currentHeat < 0)
            {
                currentHeat = 0;
            }
        }
    }

    public float CurrentCooldownTime { get { return CooldownTime * hasteMultiplier - CooldownBonusMalus; } }
    public float CurrentCastTime { get { return CastTime * hasteMultiplier; } }
    public float CurrentChannelTime { get { return ChannelTime * hasteMultiplier; } }
    protected float hasteMultiplier { get; set; }
    public bool isRunning { get { return CurrentState == AbilityState.Executing || CurrentState == AbilityState.InCast; } }

    public bool HasHit
    {
        get
        {
            return hitted_entities_temp.Count > 0;
        }
    }
    private AbilityState _currentState;
    [PropertyOrder(-2)]
    [ShowInInspector]
    public AbilityState CurrentState
    {
        get
        {
            return _currentState;
        }
        set
        {
            _currentState = value;
            OnAbilityStateChange?.Invoke(owner, this);
        }
    }

    private int _controllerIndex = -1;
    public int controllerIndex { get { return _controllerIndex; } set { _controllerIndex = value; } }

    protected WaitForSeconds waitForEndTime;
    protected Coroutine executionCoroutine = null;
    protected Coroutine updateTargetingRoutine = null;
    protected bool isChargeCooling = false;
    protected Transform rightHand;
    protected Transform leftHand;
    protected List<Entity> hitted_entities_temp = new List<Entity>();

    #endregion

    #region Initialisation et Editor

    private void Awake()
    {
        abilityWeaponParameter_cache.Clear();
        for (int i = 0; i < abilityWeaponParameters.Count; ++i)
        {
            abilityWeaponParameter_cache.Add(abilityWeaponParameters[i].weapon, abilityWeaponParameters[i]);
        }

        AvalaibleWeapon = abilityWeaponParameter_cache.Keys.ToHashSet();
        waitForEndTime = new WaitForSeconds(EndTime);
    }

    public virtual void OnEnable()
    {
        EntityControl.OnStartMoveEntity += OnEntityStartMove;
        GameServer.OnAfterAttack += GameServer_OnAfterAttack;

        if (IsRangeDynamic && owner != null)
        {
            owner.GetAttribute(Stat.Range).AddCurrentListenner(OnCurrentRangeBonusUpdate);
        }
    }

    public virtual void OnDisable()
    {
        EntityControl.OnStartMoveEntity -= OnEntityStartMove;
        GameServer.OnAfterAttack -= GameServer_OnAfterAttack;

        if (IsRangeDynamic && owner != null)
        {
            owner.GetAttribute(Stat.Range).RemoveCurrentListenner(OnCurrentRangeBonusUpdate);
        }

        for (int b = 0; b < talentTreeBranches.Count; ++b)
        {
            for (int l = 0; l < talentTreeBranches[b].leaves.Count; ++l)
            {

                if (talentTreeBranches[b].leaves[l].gameObject.activeSelf)
                {
                    talentTreeBranches[b].leaves[l].OnRemoved();
                }
            }
        }

        if (Rune != null)
        {
            Rune.OnRemoved();
        }
    }

    public void SetAbilityData(AbilityData abilityData)
    {
        this.abilityData = abilityData;

        if (abilityData != null)
            this.controllerIndex = abilityData.state;
        //InitializeTalentTree(abilityData);
    }

    public void InitializeTalentTree(AbilityData data)
    {
        SetAbilityData(data);

        if (talentTreeBranches != null && data != null)
        {
            // iterating branches
            for (int b = 0; b < talentTreeBranches.Count; ++b)
            {
                for (int l = 0; l < talentTreeBranches[b].leaves.Count; ++l)
                {
                    Talent talentInstance = Instantiate(talentTreeBranches[b].leaves[l], this.transform);

                    talentInstance.Init(this, abilityData, l, b);
                    // We instantiate only the selected talent that have to be active
                    if (talentInstance.selected)
                    {
                        SelectedTalents.Add(talentInstance);
                        talentInstance.OnAdded(this);
                    }
                    else
                    {
                        talentInstance.gameObject.SetActive(false);
                    }

                    // Replacing the reference by the instance
                    talentTreeBranches[b].leaves[l] = talentInstance;
                }
            }
        }
    }

    public void AddNewRune(Rune rune)
    {
        if (abilityData == null)
        {
            Debug.LogError("Ability data is null");
            return;
        }

        abilityData.runeKey = rune.name;
        BackendManager.Instance.AbilitiesRepository_Update(abilityData, true);

        InitializeRune(rune);
    }

    public void InitializeRune(Rune model = null)
    {
        if (abilityData == null)
            return;

        // No rune applied on this ability
        if (string.IsNullOrEmpty(abilityData.runeKey))
            return;

        Rune rune = model;
        if (rune == null)
        {
            rune = CoreManager.Instance.GetRuneByKey(abilityData.runeKey);
        }

        if (rune == null)
            return;

        if (Rune != null)
        {
            Debug.LogError("Overriding existing rune. Delete old");
            DeleteRune();
        }

        Rune = Instantiate(rune, this.transform);
        Rune.selected = true;
        Rune.unlocked = true;
        Rune.Init(this, abilityData, -1, -1);
    }

    public void DeleteRune()
    {
        Rune.OnRemoved();
        DestroyImmediate(Rune);

        Rune = null;

        abilityData.runeKey = "";
        BackendManager.Instance.AbilitiesRepository_Update(abilityData, true);
    }

    public void SelectTalent(Talent talent)
    {
        SelectedTalents.Add(talent);
        talent.selected = true;
        talent.OnAdded(this);
        talent.gameObject.SetActive(true);
    }

    public void UnselectTalent(Talent talent)
    {
        int index = SelectedTalents.FindIndex(t => t == talent);
        if (index != -1)
        {
            SelectedTalents.RemoveAt(index);
        }
        talent.OnRemoved();
        talent.selected = false;
        talent.gameObject.SetActive(false);
    }

    public void UnselectAllTalents()
    {
        for (int i = 0; i < SelectedTalents.Count; ++i)
        {
            UnselectTalent(SelectedTalents[i]);
            i--;
        }
    }

    public Talent GetTalent(string talentID)
    {
        if (talentTreeBranches != null)
        {
            // iterating branches
            for (int b = 0; b < talentTreeBranches.Count; ++b)
            {
                for (int l = 0; l < talentTreeBranches[b].leaves.Count; ++l)
                {
                    if (talentTreeBranches[b].leaves[l].CheckID(talentID))
                        return talentTreeBranches[b].leaves[l];
                }
            }
        }
        return null;
    }

    public List<Talent> GetBranch(int branchIndex)
    {
        if (branchIndex < talentTreeBranches.Count)
            return talentTreeBranches[branchIndex].leaves;

        return null;
    }

    public void OnAddTalentBranch()
    {
        this.talentTreeBranches.Add(new TalentTreeBranch());
    }

    [Button("Compute Power")]
    public void ComputePower()
    {
        Power = BalancingSetting.ComputeAbilityPower(this);
    }

    public virtual void Init(AbilityController skillbookComp, int skIndex, Entity owner)
    {
        Debug.Log("Initialize Skill : " + this + " of : " + owner);

        this.owner = owner;

        ComputePower();

        if (TargetingArea_prefab != null)
        {
            TargetingArea = Instantiate(TargetingArea_prefab);
            TargetingArea.gameObject.SetActive(false);
            TargetingArea.Owner = owner.gameObject.transform;
        }

        ownerCharacter = owner as Character;
        abilityController = skillbookComp;

        CurrentRange = Range;
        CurrentCharges = Charges;

        if (IsRangeDynamic)
        {
            this.owner.GetAttribute(Stat.Range).AddCurrentListenner(this.OnCurrentRangeBonusUpdate);
            OnCurrentRangeBonusUpdate(this.owner.GetAttribute(Stat.Range).Current);
        }

        CurrentState = AbilityState.Avalaible;
        controllerIndex = skIndex;
        rightHand = owner.rigthHand;
        leftHand = owner.leftHand;

        for (int i = 0; i < SelectedTalents.Count; ++i)
        {
            SelectedTalents[i].OnRootInitialized();
        }

        if (Rune != null)
        {
            Rune.OnRootInitialized();
        }

        owner.GetAttribute(Stat.HeatDissipation).AddCurrentListenner(UpdateCurrentHeatDissipation);
        UpdateCurrentHeatDissipation(owner.GetAttribute(Stat.HeatDissipation).Current);
    }
    #endregion

    #region Execution

    public void OnCurrentRangeBonusUpdate(float value)
    {
        CurrentRange = Range + Range * value / 100f;
    }

    public virtual Vector3 GetFreeAimingPoint()
    {
        return ownerCharacter.targetingSystem.GetFreeAimingPoint(Camera.main.transform.position, Camera.main.transform.forward, CurrentRange + CameraOperator.Instance.CamToPlayerDistance, false);
    }

    public virtual void StartAbility(Entity entityTarget, Vector3 positionTarget, float hastRatio, float lag, float cost) // A MODIFIER PIPELINE SKILL
    {
        CurrentLag = lag;

        OnAbilityStartRequest();

        this.hasteMultiplier = hastRatio;

        if (entityTarget != null)
        {
            owner.CurrentEntityTarget = entityTarget;
        }

        if (positionTarget != Vector3.negativeInfinity)
        {
            if (AimingMode == AbilityAimingMode.Directionnal || AimingMode == AbilityAimingMode.FreeAimingPoint || AimingMode == AbilityAimingMode.AoE)
            {
                owner.CurrentPositionTarget = positionTarget; // Position générée par le targeting system get aiming point     
            }
        }

        if (entityTarget == null && positionTarget == Vector3.negativeInfinity)
        {
            Debug.LogError("No target set !");
        }

        //IsInCooldown = true;
        Debug.Log("Prepare Skill");
        if (owner.IsLocalCharacter)
        {
            if (AllowAutoRotationTowardTarget && !ownerCharacter.playerControl.IsMoving)
            {
                if (entityTarget != null)
                {
                    ownerCharacter.playerControl.LookTo(entityTarget.transform.position);
                }
                else if (positionTarget != Vector3.negativeInfinity && positionTarget != owner.transform.position)
                {
                    ownerCharacter.playerControl.LookTo(positionTarget);
                }
            }

            if (UpdateAimingWhileCasting)
            {
                updateTargetingRoutine = StartCoroutine(UpdateTargetingArea());
            }
        }

        currentCost = cost;

        if (cost > 0)
        {
            owner.ressourceSystem.ReservateRessource(currentCost, costRessourceType);
        }

        CurrentState = AbilityState.Executing;

        hitted_entities_temp.Clear();
        Execute(lag);
    }

    public virtual void Execute(float lag)
    {
        //this.SetAbilityVFXContextRequest();

        switch (ExecutionMode)
        {
            case AbilityLaunchMode.Instant:
                // On lance l'animation + la skillloop
                StartAnimationAction();
                ExecuteLocal();
                break;
            case AbilityLaunchMode.DrivenByAnimation:
                // On lance l'animation et on attend la callBack de AnimationEvent
                StartAnimationAction();
                break;
            case AbilityLaunchMode.DrivenByCast:
                // On lance l'animation et la routine de cast, qui lancera la skilloop au terme du temps de cast
                StartAnimationAction();
                StartCoroutine(CastRoutine(lag));
                break;
        }
    }

    protected void ExecuteLocal()
    {
        if (UpdateAimingWhileCasting && updateTargetingRoutine != null)
        {
            StopCoroutine(updateTargetingRoutine);
        }

        CurrentState = AbilityState.Executing;

        switch (AimingMode)
        {
            case AbilityAimingMode.Target:
                if (owner.CurrentEntityTarget != null)
                {
                    ExecuteSkill(owner.CurrentEntityTarget);
                }
                else
                {
                    Debug.LogError("No target !");
                    AutoCancel();
                }
                break;
            case AbilityAimingMode.AoE:
            case AbilityAimingMode.Directionnal:
            case AbilityAimingMode.FreeAimingPoint:
                ExecuteSkill(owner.CurrentPositionTarget);
                break;
            case AbilityAimingMode.Self:
                ExecuteSkill(owner);
                break;
            case AbilityAimingMode.Cleave:
                ExecuteSkill(owner.transform.position);
                break;
            case AbilityAimingMode.None:
                ExecuteSkill(null);
                break;
        }
    }

    /// <summary>
    /// Executé avant le lancement de la skillLoop ou d'un cast.
    /// </summary>
    public virtual void StartAnimationAction()
    {

    }

    public virtual void EndAnimationAction()
    {

    }

    /// <summary>
    /// Lancement de l'execution du skill.
    /// </summary>
    /// <param name="target"></param>
    public void ExecuteSkill(Vector3 target)
    {
        if (ExecutionSound != null)
            owner.PlaySoundRequest(ExecutionSound);

        //CurrentState = AbilityState.Executing;

        executionCoroutine = StartCoroutine(SkillLoop(target));
        StartCoroutine(VFXLoop());
        /*        ExecutionCoroutine = this.StartThrowingCoroutine(SkillLoop(target), (exception) => SkillLoopExceptionHandler(exception));
                this.StartThrowingCoroutine(VFXLoop(), (exception) => { });
        */
        if (ShowChanneling)
        {
            StartCoroutine(ChannelingRoutine());
        }
    }

    public void ExecuteSkill(Entity target)
    {
        //CurrentState = AbilityState.Executing;
        if (ExecutionSound != null)
            owner.PlaySoundRequest(ExecutionSound);

        executionCoroutine = StartCoroutine(SkillLoop(target));
        StartCoroutine(VFXLoop());

        if (ShowChanneling)
        {
            StartCoroutine(ChannelingRoutine());
        }
    }

    protected virtual void SkillLoopExceptionHandler(Exception e)
    {
        if (GameServer.IsMaster)
        {
            abilityController.Request_EndAbility(this, AbilityEndMode.Interrupted);
        }
        else
        {
            Debug.LogError("Exception happen on client side.");
            abilityController.Request_EndAbility(this, AbilityEndMode.Interrupted);
        }
    }

    protected virtual IEnumerator SkillLoop(Vector3 target)
    {
        // Execution
        EndAbility();
        yield return null;
    }

    protected virtual IEnumerator SkillLoop(Entity target)
    {
        yield return null;
    }

    protected virtual IEnumerator VFXLoop()
    {
        yield return null;
    }

    public virtual void OnStreamWrite(PhotonStream stream, PhotonMessageInfo info, float deltaTime) { }

    public virtual void OnStreamRead(PhotonStream stream, PhotonMessageInfo info, float deltaTime) { }

    public void StartHitLoop(Entity target)
    {
        StartCoroutine(HitLoop(target));
    }

    public void StartHitLoop(Vector3 position)
    {
        StartCoroutine(HitLoop(position));
    }

    protected virtual IEnumerator HitLoop(Entity target)
    {
        // Cette routine peut être utilisée dans le cas d'un projectile pour executer du code après le hit (calculé sur le serveur). 
        // Le code executé devra être de l'ordre du visuel ou de la gestion d'un délai entre déclenchement d'un FX et affichage de dégats par exemple).
        ApplyDamageAndEffectsTo(target, Damages, Effects);

        yield return null;
    }

    protected virtual IEnumerator HitLoop(Vector3 target)
    {
        yield return null;
    }

    protected void Request_EndAbility()
    {
        if (GameServer.IsMaster)
        {
            owner.abilityController.Request_EndAbility(this, AbilityEndMode.Classic);
        }
        else
        {
            Debug.LogError("Cant end ability from client.");
        }
    }

    public virtual void EndAbility(AbilityEndMode abilityEndMode = AbilityEndMode.Classic, bool generateRessource = true)
    {
        Debug.Log("End ability");
        EndAnimationAction();

        StopAllCoroutines();

        if (CurrentState == AbilityState.InCast)
        {
            InterruptCast();

            if (currentCost > 0)
            {
                owner.ressourceSystem.CancelReservateRessource(currentCost, costRessourceType);
            }

            CurrentState = AbilityState.Avalaible;
        }
        else
        {
            if (currentCost > 0)
            {
                owner.ressourceSystem.UseReservatedRessource(costRessourceType, currentCost);
            }

            if (AbilityMode == AbilityMode.Heat)
            {
                CurrentHeat += HeatPerUse;

                if (CurrentHeat > MaxHeat)
                {
                    CurrentState = AbilityState.Unavalaible;
                }
                else
                {
                    CurrentState = AbilityState.Avalaible;
                }
            }
            else
            {
                if (Charges > 0)
                    CurrentCharges--;

                if (CurrentCooldownTime > 0 && CurrentProcs == 0 && Charges == 0)
                    StartCoroutine(CooldownRoutine());
                else
                    CurrentState = AbilityState.Avalaible;
            }
        }

        owner.abilityController.AbilityEnded(this);

        if (owner.IsLocalCharacter)
        {
            HideTargetingArea();

            HUDController.Instance.actionbarPanel.UpdateFillAmount(controllerIndex, 0);
            HUDController.Instance.actionbarPanel.ToggleCastBar(false);
        }

        switch (abilityEndMode)
        {
            case AbilityEndMode.Classic:
                OnAbilityEndRequest();
                break;
            case AbilityEndMode.Interrupted:
                OnInterrupt();
                OnAbilityInterruptRequest();
                break;
            case AbilityEndMode.EndedByPlayer:
                OnEndedByPlayer();
                OnAbilityEndRequest();
                break;

            default:
                Debug.LogError("not handled");
                break;
        }

        hitted_entities_temp.Clear();
    }

    private void InterruptCast()
    {
        if (CastSound.audioClip != null)
        {
            ownerCharacter.ToggleCastLoopSound(CastSound, false);
        }

        if (owner.IsLocalCharacter)
        {
            HUDController.Instance.actionbarPanel.ToggleCastBar(false);
        }
        else
        {
            owner.lifeBar.UpdateCastInfo(0, 1);
            owner.lifeBar.ShowCastInfo(false);
        }
    }

    /// <summary>
    /// Annulation automatique ne consommant pas de ressource et ne générant pas de cooldown
    /// </summary>
    public virtual void AutoCancel()
    {
        StopAllCoroutines();

        EndAnimationAction();

        if (costRessourceValue > 0)
        {
            owner.ressourceSystem.CancelReservateRessource(costRessourceValue, costRessourceType);
        }

        CurrentState = AbilityState.Avalaible;
        hitted_entities_temp.Clear();
        OnAbilityCancelRequest();
    }

    protected virtual void GameServer_OnAfterAttack(Entity launcher, Entity target, Ability ability, Damage[] damages, Effect[] effects)
    {
        if (launcher == owner && this == ability && this.CurrentState == AbilityState.Executing && hitted_entities_temp.Contains(target) && HitSounds.Length > 0)
        {
            owner.PlaySoundRequest(HitSounds[Randomizer.Rand(0, HitSounds.Length)]);
        }
    }

    public virtual void OnAutoLaunchEnded()
    {

    }
    #endregion

    #region Checkers
    // A surcharger à l'occasion pour les skills de NPC
    public virtual bool IsEquipable()
    {
        if (RequiredEquipmentType == AbilityEquipmentType.None)
        {
            return true;
        }
        else if (RequiredEquipmentType == AbilityEquipmentType.Hammer
            || RequiredEquipmentType == AbilityEquipmentType.Rifle
            || RequiredEquipmentType == AbilityEquipmentType.Staff)
        {
            if (owner.IsUnarmed)
            {
                if (AvalaibleWeapon.Contains(Weapon.Fists))
                {
                    return true;
                }
            }
            else
            {
                if (AvalaibleWeapon.Contains(ownerCharacter.characterAnimationSystem.RightWeapon)
                    || AvalaibleWeapon.Contains(ownerCharacter.characterAnimationSystem.LeftWeapon)
                    || !ownerCharacter.characterAnimationSystem.HasWeaponOut && AvalaibleWeapon.Contains(Weapon.Fists))
                {
                    return true;
                }
            }
        }
        else
        {
            if (ownerCharacter.equipmentSystem.CurrentEquipmentTypeWeights[RequiredEquipmentType] >= RequiredEquipmentTypePoints)
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool IsLaunchable()
    {
        if (!IsLaunchableExternal())
            return false;

        return IsLaunchableInternal();
    }

    public virtual bool IsLaunchableExternal()
    {
        if (owner.IsRolling || owner.IsBlocking)
            return false;

        if (!owner.CanDoAction)
            return false;

        return true;
    }

    public virtual bool IsLaunchableInternal()
    {
        if (!LaunchableWhileMoving)
        {
            if (owner.entityControl.IsJumping || owner.entityControl.IsFalling || owner.IsMoving)
                return false;
        }

        if (CurrentState != AbilityState.Avalaible)
            return false;

        if (Charges > 0 && CurrentCharges <= 0)
            return false;

        /*if (!IsEquipable()) // géré au niveau du ability controller
            return false;*/

        if (AimingMode == AbilityAimingMode.Target)
        {
            if (owner.CurrentEntityTarget == null)
                return false;

            if (owner.CurrentEntityTarget == owner && !LaunchableOnSelf)
                return false;

            if (owner.CurrentEntityTarget.Team == owner.Team && !LaunchableOnAlly)
                return false;

            if (owner.CurrentEntityTarget.Team != owner.Team && !LaunchableOnEnemy)
                return false;

            if (Vector3.Distance(owner.CurrentEntityTarget.transform.position, owner.transform.position) > CurrentRange)
                return false;
        }

        if (AbilityMode == AbilityMode.Heat)
        {
            float cost = ComputeCost();
            if (cost > 0
                && !owner.ressourceSystem.CheckRessource(cost, costRessourceType))
                return false;
        }
        else
        {
            if (costRessourceValue > 0
            && !owner.ressourceSystem.CheckRessource(costRessourceValue, costRessourceType))
                return false;
        }

        return true;
    }

    public bool IsPositionInRange(Vector3 checkPosition, float range)
    {
        return Vector3.Distance(owner.transform.position, checkPosition) <= range;
    }

    public virtual bool IsFocusInRange(float range)
    {
        return owner.CurrentEntityTarget != null && Vector3.Distance(owner.CurrentEntityTarget.transform.position, owner.transform.position) <= range;
    }

    #endregion

    #region Physics

    public virtual void OnDetect(PhysicsObject pObj, Collider other)
    {
        if (other.CompareTag("Character") || other.CompareTag("CollidableEntity"))
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity != null)
            {
                if (IsRollingDodgeable && entity.rollingSubsystem != null && entity.rollingSubsystem.IsRolling)
                {
                    Debug.LogError(entity + " dodged ability physic object !");
                }
                else
                {
                    OnDetectEntity(pObj, entity);
                }
            }
        }
        else if (other.CompareTag("World"))
        {
            OnDetectWorld(pObj);
        }
    }

    protected virtual void OnDetectEntity(PhysicsObject pObj, Entity entity)
    {

    }

    protected virtual void OnDetectWorld(PhysicsObject pObj)
    {

    }

    public static List<Entity> GetOverlapEntitiesFrom(Vector3 center, float radius, bool losCheck, int excludedTeam = -1)
    {
        LayerMask mask = LayerMask.GetMask("Entities");
        Collider[] colliders = Physics.OverlapSphere(center, radius, mask);

        int los_mask = LayerMask.GetMask("Walls", "Ground");

        List<Entity> results = new List<Entity>();
        Entity entity = null;

        for (int i = 0; i < colliders.Length; ++i)
        {
            if (colliders[i].IsAnyDamageableEntity())
            {
                colliders[i].TryGetEntityFromCollider(out entity);

                if (entity == null)
                {
                    continue;
                }

                if (excludedTeam != -1 && entity.Team == excludedTeam)
                    continue;

                if (losCheck && Physics.Linecast(center, entity.chestTransform.position, los_mask))
                {
                    continue;
                }

                results.Add(entity);
            }
        }

        return results;
    }

    public static List<Entity> GetOverlapConeEntitiesFrom(Vector3 center, Vector3 forward, float radius, float angle, bool losCheck, int excludedTeam = -1)
    {
        List<Entity> allEntities = GetOverlapEntitiesFrom(center, radius, losCheck, excludedTeam);
        List<Entity> result = new List<Entity>();

        if (angle < 360)
        {
            float dot = Mathf.Cos(angle / 2f * Mathf.Deg2Rad);

            for (int i = 0; i < allEntities.Count; ++i)
            {
                float crtDot = Vector3.Dot(forward, (allEntities[i].transform.position - center).normalized);
                if (crtDot >= dot) // on enleve tout ce qui n'est pas dans le cone avec l'angle voulu
                {
                    result.Add(allEntities[i]);
                }
            }
        }
        else
        {
            result = allEntities;
        }

        return result;
    }

    #endregion

    #region Damages And Effects

    public virtual void ApplyDamageAndEffectsTo(Entity toDamage, List<Damage> damages, List<Effect> effects)
    {
        if (ApplyDamageOncePerTarget && hitted_entities_temp.Contains(toDamage))
            return;

        hitted_entities_temp.Add(toDamage);

        if (!GameServer.IsMaster)
            return;

        if (toDamage == owner)
        {
            if (!CanDamageSelf)
                return;
        }
        else
        {
            if (toDamage.Team == owner.Team)
            {
                if(!CanDamageAlly)
                    return;
            }
            else
            { 
                if (!CanDamageEnemy)
                {
                    return;
                }
            }               
        }


        if (damages != null && effects != null)
        {
            // On crée une nouvelle liste car l'event before apply damage pourra apporter des modifications aux listes, et on ne veut pas les répercuter dans l'instance du skill
            List<Damage> instanceDamList = new List<Damage>();
            if (damages != null)
                damages.For(t => instanceDamList.Add(new Damage(t)));

            List<Effect> instanceEffList = new List<Effect>();
            if (effects != null)
                effects.For(t => instanceEffList.Add(t));

            OnAbilityBeforeApplyDamageAndEffects?.Invoke(owner, toDamage, this, instanceDamList, instanceEffList);

            GameServer.Instance.Server_ExecuteAttack(owner, toDamage, this, instanceDamList, EffectSystem.CreateDatas(instanceEffList));
        }
        else if (damages != null && effects == null)
        {
            // On crée une nouvelle liste car l'event before apply damage pourra apporter des modifications aux listes, et on ne veut pas les répercuter dans l'instance du skill
            List<Damage> instanceDamList = new List<Damage>();
            if (damages != null)
                damages.For(t => instanceDamList.Add(new Damage(t)));

            OnAbilityBeforeApplyDamageAndEffects?.Invoke(owner, toDamage, this, instanceDamList, null);

            GameServer.Instance.Server_ExecuteAttack(owner, toDamage, this, instanceDamList, null);
        }
        else if (damages == null && effects != null)
        {
            List<Effect> instanceEffList = new List<Effect>();
            if (effects != null)
                effects.For(t => instanceEffList.Add(t));

            OnAbilityBeforeApplyDamageAndEffects?.Invoke(owner, toDamage, this, null, instanceEffList);

            GameServer.Instance.Server_ApplyEffect(owner, owner, this, EffectSystem.CreateDatas(instanceEffList));
        }
    }

    public List<long> ApplyEffectsTo(Entity target, List<Effect> effects)
    {
        List<long> effects_ids = new List<long>();
        EffectData[] datas = EffectSystem.CreateDatas(effects);

        // Dans ce cas particulier on veux récupérer les IDs générés pour les retourner  
        for (int i = 0; i < effects.Count; ++i)
        {
            effects_ids.Add(datas[i].uniqueID);
        }

        GameServer.Instance.Server_ApplyEffect(owner, target, this, datas);
        return effects_ids;
    }

    public void RemoveEffectOf(Entity target, List<long> ids)
    {
        for (int i = 0; i < ids.Count; ++i)
        {
            Effect current = target.effectSystem.GetEffectByUniqueID(ids[i]);
            if (current != null)
                target.effectSystem.Request_Remove(current);
            else Debug.LogError("Can't find effect of id " + ids[i] + " on " + target);
        }
    }

    public virtual Vector3 GetHitFeedbackIkPosition(Entity launcher, Entity target)
    {
        return (target.transform.position - launcher.transform.position).normalized;
    }

    #endregion

    #region Casting and Cooldown Management

    private void Update()
    {
        UpdateCharges(Time.deltaTime);
        UpdateHeat();
    }

    protected void UpdateCharges(float time)
    {
        if (Charges == 0 || owner == null)
            return;

        if (CurrentCharges < Charges)
        {
            bool canDisplay = CurrentState != AbilityState.Executing && CurrentState != AbilityState.InCast;

            if (!isChargeCooling)
            {
                CurrentCooldown = 0;
                isChargeCooling = true;
            }

            if (isChargeCooling)
            {
                CurrentCooldown += time * WorldManager.Instance.GetTimeFactor(owner.Team);

                if (canDisplay && owner.IsLocalCharacter)
                    HUDController.Instance.actionbarPanel.UpdateFillAmountCharge(controllerIndex, CurrentCooldown / CurrentCooldownTime);

                if (CurrentCooldown > CurrentCooldownTime)
                {
                    CurrentCharges++;
                    isChargeCooling = false;

                    if (canDisplay && owner.IsLocalCharacter)
                        HUDController.Instance.actionbarPanel.UpdateFillAmountCharge(controllerIndex, 0);
                }
            }
        }
    }

    protected void UpdateHeat()
    {
        if (AbilityMode == AbilityMode.Heat && !isRunning && CurrentHeat > 0)
        {
            CurrentHeat -= owner.entityDeltaTime * currentHeatDissipation;

            if (CurrentHeat > MaxHeat)
            {
                CurrentState = AbilityState.Unavalaible;
            }
            else
            {
                CurrentState = AbilityState.Avalaible;
            }
        }
    }

    private void UpdateCurrentHeatDissipation(float value)
    {
        currentHeatDissipation = (1 + value / 100f) * HeatDissipation;
    }

    [Button("Test cooldown malus")]
    public void AddBonusMalusTest(float bonusMalus)
    {
        CooldownBonusMalus += bonusMalus;
    }

    protected virtual IEnumerator UpdateTargetingArea()
    {
        while (true)
        {
            abilityController.UpdateTargeting(true);
            yield return null;
        }
    }

    protected virtual IEnumerator CooldownRoutine()
    {
        // Le cooldown est calculé à l'envers pour supporter des temps de recharge dynamqie
        // Si le temps de recharge de base est de 3secondes mais qu'un bonus de hâte vient le faire passer à 2 seconde pendant la routine, 
        // si current cooldown > CurrentCooldownTime, on sortira automatique du CD 

        CurrentCooldown = 0; // CurrentCooldownTime;

        while (CurrentCooldown < CurrentCooldownTime)
        {
            yield return null;

            CurrentCooldown += Time.deltaTime * WorldManager.Instance.GetTimeFactor(owner.Team);

            // Affichage du fillamount uniquement pour le joueur local..
            if (CurrentProcs > 0)
            {
                CurrentState = AbilityState.Avalaible;

                if (owner.IsLocalCharacter)
                    HUDController.Instance.actionbarPanel.UpdateFillAmount(controllerIndex, 0);
            }
            else
            {
                if (CurrentState != AbilityState.Unavalaible)
                    CurrentState = AbilityState.Unavalaible;

                // On inverse l'affichage 
                if (owner.IsLocalCharacter)
                {
                    if (CurrentCooldownTime > 0)
                        HUDController.Instance.actionbarPanel.UpdateFillAmount(controllerIndex, (CurrentCooldownTime - CurrentCooldown) / CurrentCooldownTime);
                }
            }

        }

        CooldownBonusMalus = 0;

        if (owner.IsLocalCharacter)
            HUDController.Instance.actionbarPanel.UpdateFillAmount(controllerIndex, 0);

        CurrentState = AbilityState.Avalaible;
    }

    protected virtual IEnumerator CastRoutine(float lag = 0)
    {
        float currentCastTime = lag;
        CurrentState = AbilityState.InCast;

        if (CastSound.audioClip != null)
        {
            ownerCharacter.ToggleCastLoopSound(CastSound, true);
        }

        if (owner.IsLocalCharacter)
        {
            HUDController.Instance.actionbarPanel.ToggleCastBar(true);

            while (currentCastTime < CurrentCastTime)
            {
                yield return null;
                currentCastTime += Time.deltaTime * WorldManager.Instance.GetTimeFactor(owner.Team);

                HUDController.Instance.actionbarPanel.UpdateFillAmountCasting(controllerIndex, currentCastTime / CurrentCastTime);
                HUDController.Instance.actionbarPanel.UpdateCastBar(currentCastTime, CurrentCastTime);
            }

            HUDController.Instance.actionbarPanel.UpdateFillAmountCasting(controllerIndex, 0);
            HUDController.Instance.actionbarPanel.ToggleCastBar(false);

        }
        else
        {
            owner.lifeBar.ShowCastInfo(true, LocalizationManager.GetLocalizedValue(this.Name, LocalizationFamily.Gameplay));

            while (currentCastTime < CurrentCastTime)
            {
                yield return null;
                currentCastTime += Time.deltaTime * WorldManager.Instance.GetTimeFactor(owner.Team);

                // En réseau, montrer la barre du cast en cours sous la barre de vie
                owner.lifeBar.UpdateCastInfo(currentCastTime, CurrentCastTime);
            }

            owner.lifeBar.UpdateCastInfo(0, 1);
            owner.lifeBar.ShowCastInfo(false);
        }

        if (CastSound.audioClip != null)
        {
            ownerCharacter.ToggleCastLoopSound(CastSound, false);
        }

        ExecuteLocal();
    }

    protected virtual IEnumerator ChannelingRoutine()
    {
        // ****************************************
        float currentChannelTime = 0; //CurrentChannelTime;

        if (owner.IsLocalCharacter)
        {
            HUDController.Instance.actionbarPanel.ToggleCastBar(true);

            while (currentChannelTime < CurrentChannelTime)
            {
                yield return null;
                currentChannelTime += Time.deltaTime * WorldManager.Instance.GetTimeFactor(owner.Team);

                HUDController.Instance.actionbarPanel.UpdateFillAmountChanneling(controllerIndex, (CurrentChannelTime - currentChannelTime) / CurrentChannelTime);
                HUDController.Instance.actionbarPanel.UpdateChannelBar((CurrentChannelTime - currentChannelTime), CurrentChannelTime);
            }

            HUDController.Instance.actionbarPanel.UpdateFillAmount(controllerIndex, 0);
            HUDController.Instance.actionbarPanel.ToggleCastBar(false);
        }
        else
        {
            owner.lifeBar.ShowCastInfo(true, LocalizationManager.GetLocalizedValue(this.Name, LocalizationFamily.Gameplay));

            while (currentChannelTime < CurrentChannelTime)
            {
                yield return null;
                currentChannelTime += Time.deltaTime * WorldManager.Instance.GetTimeFactor(owner.Team);

                // En réseau, montrer la barre du cast en cours sous la barre de vie
                owner.lifeBar.UpdateCastInfo(CurrentChannelTime - currentChannelTime, CurrentChannelTime);

            }

            owner.lifeBar.UpdateCastInfo(0, 1);
            owner.lifeBar.ShowCastInfo(false);
        }
    }

    public void AddCooldownBonusMalus(float newvalue)
    {
        CooldownBonusMalus += newvalue;
    }
    #endregion

    #region Utils

    public virtual float ComputeHasteMultiplier(Entity launcher)
    {
        return 1 - launcher.GetAttribute(Stat.Haste).Current / 100f;
    }

    public float ComputeCost()
    {
        if (AbilityMode == AbilityMode.Heat)
        {
            return costRessourceValue * (1f + (CurrentHeat / 50f));
        }

        return costRessourceValue;
    }

    public FieldInfo[] GetUiAbilityParameters()
    {
        Type abilityType = this.GetType();
        var talentFields = abilityType.GetFields();
        var fields = new List<FieldInfo>();
        for (int i = 0; i < talentFields.Length; ++i)
        {
            var attribute = (UIAbilityParameterAttribute[])talentFields[i].GetCustomAttributes(typeof(UIAbilityParameterAttribute), true);
            if (attribute != null && attribute.Length > 0)
            {
                fields.Add(talentFields[i]);
            }
        }
        return fields.ToArray();
    }

    public float GetXPRatio()
    {

        return GetCurrentXP() / GetLevelXP();
    }

    public float GetLevelXP()
    {
        int points = Mathf.Clamp(abilityData.totalTalentPoints, 0, CoreManager.Settings.abilitiesSettings.MaxAbilityLevel - 1);
        return (float)CoreManager.Settings.abilitiesSettings.abilityExperiencePallier[points];
    }

    public float GetCurrentXP()
    {
        return abilityData != null ? (float)abilityData.xp : 0f;
    }

    public AbilityWeaponParameter GetWeaponParameter(Weapon weapon)
    {
        return abilityWeaponParameter_cache[weapon];
    }

    public float GetDynamicYieldTime(int yieldIndex)
    {
        return GetWeaponParameter(ownerCharacter.characterAnimationSystem.CurrentMainWeapon).yieldTimes[yieldIndex] * hasteMultiplier;
    }
    #endregion

    #region Aiming

    public virtual void ShowTargetingArea(Vector3 tarOrDir)
    {
        if (TargetingArea != null)
        {
            TargetingArea.gameObject.SetActive(true);

            if (tarOrDir != Vector3.negativeInfinity)
                TargetingArea.transform.position = tarOrDir;
            else
                TargetingArea.transform.position = tarOrDir;

        }
    }

    public virtual void UpdateTargetingArea(bool canBeLaunched, Vector3 tarOrDir, bool noTargetAvalaible = false)
    {
        if (TargetingArea != null)
        {
            if (tarOrDir == new Vector3(-10000, -10000, -10000))
            {
                TargetingArea.gameObject.SetActive(false);
            }
            else
            {
                switch (AimingMode)
                {
                    case AbilityAimingMode.Target:
                        if (!noTargetAvalaible)
                        {
                            TargetingArea.transform.position = tarOrDir;
                            TargetingArea.DisplayCircleArea(EffectRange == 0 ? 1 : EffectRange);
                        }
                        else
                        {
                            TargetingArea.transform.position = owner.transform.position;
                            TargetingArea.DisplayCircleArea(CurrentRange == 0 ? 1 : CurrentRange);
                        }
                        break;
                    case AbilityAimingMode.AoE:
                        TargetingArea.transform.position = tarOrDir;
                        TargetingArea.DisplayCircleArea(EffectRange == 0 ? 1 : EffectRange);
                        break;

                    case AbilityAimingMode.Directionnal:
                    case AbilityAimingMode.FreeAimingPoint:
                    case AbilityAimingMode.Self:
                    case AbilityAimingMode.Cleave:
                    case AbilityAimingMode.None:
                        TargetingArea.transform.position = owner.transform.position;
                        TargetingArea.DisplayDirectionnalArea(EffectRange == 0 ? 1 : EffectRange, (tarOrDir - owner.transform.position).normalized);
                        break;
                }

                TargetingArea.gameObject.SetActive(true);
            }
        }
    }

    public virtual void HideTargetingArea()
    {
        if (TargetingArea != null)
        {
            TargetingArea.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Callbacks

    protected virtual void OnEntityStartMove(Entity entity, MovementType type)
    {
        if (entity == owner
            && isRunning // cast or execute
            && !LaunchableWhileMoving)
        {
            Debug.Log("Interrupting " + this + " as it cant be executed while moving");
            abilityController.Request_EndAbility(this, AbilityEndMode.Interrupted);
        }
    }

    public virtual void OnInterrupt()
    {

    }

    public virtual void OnEndedByPlayer()
    {

    }

    public virtual void OnRemoved()
    {

    }

    #endregion

    #region Network 

    /// <summary>
    /// Attention, peut etre lancé depuis un client
    /// </summary>
    /// <param name="datas"></param>
    public void Client_BroadcastNetworkMessage(object[] datas)
    {
        GameServer.networkServer.ClientCommand_Ability_BroadcastNetworkMessage(owner, this, datas);
    }

    public virtual void RPC_BroadcastedMessage(object[] data)
    {

    }

    #endregion
}

#region Enums

/// <summary>
/// Etat de disponibilité actuel du skill. |
/// Avalaible : Disponible |
/// InCast : En train de s'éxecuter |
/// InCooldown : En cours de recharge
/// </summary>
public enum AbilityState
{
    Avalaible,
    Unavalaible,
    InCast,
    Executing // InChanneling
}

/// <summary>
/// Mode de lancement 
/// </summary>
public enum AbilityAimingMode
{
    Target, // Tab or select entity in world
    AoE, // Mouse in world
    Directionnal,
    Self,
    Cleave,
    FreeAimingPoint,
    None,
}

public enum AbilityCategory
{
    AttackMelee = 0,
    AttackDistance = 1,
    Defense = 2,
    Utilitary = 3,
    Move = 4,
}

public enum AbilityEquipmentType
{
    None = 0,

    Hammer = 1, // Si weapon, voir spécifiquement les armes valables via un enum Weapon
    Rifle = 2,
    Staff = 4,

    Inquisitor = 64,
    Engineering = 128,
    Protection = 256,

    LightMovement = 512,
    MediumMovement = 1024,
    HeavyMovement = 2048,
}

/// <summary>
/// Instant : pas de temps de cast, gestion 100% skillloop
/// AnimationEvent : executé par event d'animation
/// Cast : lancement à la fin du temps de cast
/// </summary>
public enum AbilityLaunchMode
{
    Instant,
    DrivenByAnimation,
    DrivenByCast,
}

public enum AbilityMode
{
    Heat = 0,
    Cooldown = 1,
}

/// <summary>
/// Utilisé pour flagger l'usage des skills pour les IA
/// </summary>
public enum AbilityFlags
{
    None,
    SelfHeal,
    AllyHeal,
    OffCooldown,
    DefCooldown,
    AoE,
    SimpleTarget,
    Cleave,
    MultipleTargets,
    Util,
    Projectile
}

/// <summary>
/// Tells the server how to compute the ability damages, taking both weapon, only left or only right.
/// </summary>
public enum DamageComputationMode
{
    None,
    BothWeaponDamages,
    RigthWeaponDamages,
    LeftWeaponDamages,
}

public enum AbilityEndMode
{
    Classic, // EndSkill normal déclenché par le code de la skillloop
    Interrupted,
    EndedByPlayer
}

#endregion
