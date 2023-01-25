using Assets.BattleGame.Scripts.Controllers;
using Assets.BattleGame.Scripts.Managers;
using Assets.SteamAndMagic.Scripts.Managers;
using IncursionDAL;
using Photon.Pun;
using SteamAndMagic.Backend;
using SteamAndMagic.Entities;
using SteamAndMagic.Interface;
using SteamAndMagic.Photon;
using SteamAndMagic.Systems.Abilities;
using SteamAndMagic.Systems.Items;
using SteamAndMagic.Systems.RiftChat;
using SteamAndMagic.Systems.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class AbilityController : MonoBehaviourPun, IInputDownListenner, IInputStayListenner, IInputUpListenner, IPunObservable
{
    [Header("Skilllbook")]
    public GameObject SkillbookParent;

    protected Entity Owner;
    protected Character OwnerCharacter;

    public Ability[] Abilities = new Ability[0]; // Action bar abilities

    public List<IPassiveAbility> Passives = new List<IPassiveAbility>();

    [Space]
    public bool HasControl = false;
    public bool IsPrevisualizing = false;
    public bool OnGCD = false;

    [Space]
    public float GlobalCooldown = 0.5f;
    public float GCD { get; private set; }

    [Space]
    public Ability CurrentAbility = null;

    /// <summary>
    /// Ability start has been requested from the client but the execution is not yet effective.
    /// </summary>
    public bool IsPendingLaunch = false;

    private Vector3 currentAimedTarget;
    public Vector3 CurrentAimedTarget => currentAimedTarget;

    #region Inits

    public virtual void OnEnable()
    {
        InputsManager.AddCallbackTarget(this, this, this);
    }

    public virtual void OnDisable()
    {
        InputsManager.RemoveCallbackTarget(this, this, this);
    }

    public virtual void Init(Entity owner)
    {
        Owner = owner;
        OwnerCharacter = Owner as Character;

        Array.Resize(ref Abilities, CoreManager.Instance.coreSetting.DeckSize);

        if (owner.IsMine)
        {
            // Si IsMine, on a déjà les compétences instantiées dans le DeckManager local, on ne crée pas de doublon
            for (int i = 0; i < DeckManager.Instance.Deck.Length; ++i)
            {
                if (DeckManager.Instance.Deck[i] != null)
                {
                    Abilities[i] = DeckManager.Instance.Deck[i];
                    Abilities[i].gameObject.transform.SetParent(this.SkillbookParent.transform);
                    Abilities[i].transform.localPosition = Vector3.zero;
                    Abilities[i].enabled = true;
                    Abilities[i].gameObject.SetActive(true);
                    //Abilities[i].InitData(playerDeck[i]);
                }
            }
        }
        else
        {
            List<AbilityData> playerDeck = (owner as Character).characterData.abilities.Where(t => t.state >= 0).ToList();
            // Le Deck est mis dans l'ordre par l'Init du DeckManager...
            for (int i = 0; i < playerDeck.Count; ++i)
            {
                Abilities[playerDeck[i].state] = DeckManager.Instance.GenerateAbilityAtCurrentTalent(playerDeck[i]);
                Abilities[playerDeck[i].state].gameObject.transform.SetParent(this.SkillbookParent.transform);
                Abilities[playerDeck[i].state].transform.localPosition = Vector3.zero;
                Abilities[playerDeck[i].state].enabled = true;
                Abilities[playerDeck[i].state].gameObject.SetActive(true);
                //Abilities[playerDeck[i].state].InitData(playerDeck[i]);
            }
        }

        // Le playerDeck est une liste, Abilities un tableau pouvant contenir des case vides
        for (int i = 0; i < Abilities.Length; ++i)
        {
            if (Abilities[i] != null)
            {
                Abilities[i].Init(this, i, owner);
            }
        }

        if (owner.IsMine)
        {
            HUDController.Instance.actionbarPanel.InitAbilityBar(this);
        }
    }

    #endregion

    #region Updates/events
    public void OnInputDown(InputAction actionInputType)
    {
        if (InGameChatController.InChat)
            return;

        if (!HasControl)
            return;

        if (SelectionSubsystem.IsUIMode)
            return;

        if (InterfaceTools.IsPointerOverUIObject())
        {
            Debug.LogError("Pointer over ui");
            //return;
        }

        if (!Owner.IsLocalCharacter)
            return;

        if (IsPrevisualizing
            && CurrentAbility != null
            && (CurrentAbility.controllerIndex != (int)actionInputType.actionType
            || actionInputType.actionType == ActionType.Block
            || actionInputType.actionType == ActionType.Cancel
            || actionInputType.actionType == ActionType.RollsMode
            || actionInputType.actionType == ActionType.Jump
            || actionInputType.actionType == ActionType.WeaponAbility))
        {
            Debug.Log("Canceling aiming");

            StopAiming();
            CurrentAbility = null;
        }

        if (CurrentAbility != null && CurrentAbility.IsCancelable && CurrentAbility.isRunning)
        {
            if (actionInputType.actionType == ActionType.Block
            || actionInputType.actionType == ActionType.Cancel
            || actionInputType.actionType == ActionType.RollsMode
            || actionInputType.actionType == ActionType.Jump
            || actionInputType.actionType == ActionType.WeaponAbility
            || (int)actionInputType.actionType <= 10)
            {
                Debug.Log("Canceling ability");

                Request_EndAbility(CurrentAbility, AbilityEndMode.EndedByPlayer);
            }
        }

        if ((int)actionInputType.actionType <= 10)
        {
            SelectAbility((int)actionInputType.actionType);
        }
    }

    public void OnInputStay(InputAction inputAction)
    {
        if (SelectionSubsystem.IsUIMode)
            return;

        int index = (int)inputAction.actionType;

        if (IsPrevisualizing
           && CurrentAbility != null
            && (int)inputAction.actionType == CurrentAbility.controllerIndex
            && Owner == GameManager.Instance.LocalCharacter)
        {
            bool canBeLaunched = CanLaunchAbility(CurrentAbility);
            HUDController.Instance.actionbarPanel.Display_ClickOnSkill((int)inputAction.actionType, true, canBeLaunched);

            UpdateTargeting(canBeLaunched);
        }
    }

    public Vector3 UpdateTargeting(bool canBeLaunched)
    {
        switch (CurrentAbility.AimingMode)
        {
            case AbilityAimingMode.Target:
                if (Owner.CurrentEntityTarget != null)
                {
                    currentAimedTarget = Owner.CurrentEntityTarget.transform.position;

                    Debug.DrawLine(Owner.chestTransform.position, currentAimedTarget, Color.yellow);
                    CurrentAbility.UpdateTargetingArea(canBeLaunched, currentAimedTarget);
                }
                else
                {
                    currentAimedTarget = new Vector3(-10000, -10000, -10000);
                    CurrentAbility.UpdateTargetingArea(canBeLaunched, Vector3.zero, true);
                }
                break;
            case AbilityAimingMode.AoE:
                currentAimedTarget = TargetingSystem.GetAimingPoint(Owner.transform.position, CurrentAbility.CurrentRange, CurrentAbility.EffectRange);
                Debug.DrawLine(Owner.chestTransform.position, currentAimedTarget, Color.yellow);
                CurrentAbility.UpdateTargetingArea(canBeLaunched, currentAimedTarget);
                break;
            case AbilityAimingMode.Directionnal:
                currentAimedTarget = Owner.transform.position + (new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z) * CurrentAbility.EffectRange);
                Debug.DrawLine(Owner.chestTransform.position, currentAimedTarget, Color.yellow);
                CurrentAbility.UpdateTargetingArea(canBeLaunched, currentAimedTarget);
                break;
            case AbilityAimingMode.FreeAimingPoint:
                currentAimedTarget = CurrentAbility.GetFreeAimingPoint();

                Debug.DrawLine(Owner.chestTransform.position, currentAimedTarget, Color.yellow);
                CurrentAbility.UpdateTargetingArea(canBeLaunched, currentAimedTarget);
                break;
            case AbilityAimingMode.Self:
                currentAimedTarget = Owner.transform.position + Owner.transform.forward;
                CurrentAbility.UpdateTargetingArea(canBeLaunched, Owner.transform.position);
                break;
            case AbilityAimingMode.Cleave:
                currentAimedTarget = new Vector3(Owner.transform.forward.x, 0, Owner.transform.forward.z) * CurrentAbility.EffectRange + Owner.transform.position;
                Debug.DrawLine(Owner.chestTransform.position, currentAimedTarget, Color.yellow);
                CurrentAbility.UpdateTargetingArea(canBeLaunched, currentAimedTarget);
                break;
            case AbilityAimingMode.None:
                break;
        }

        return currentAimedTarget;
    }

    public void OnInputUp(InputAction inputAction)
    {
        if (SelectionSubsystem.IsUIMode)
            return;

        int index = (int)inputAction.actionType;

        if (IsPrevisualizing
            && CurrentAbility != null
            && index <= 10
            && Owner == GameManager.Instance.LocalCharacter
            && CurrentAbility.controllerIndex == (int)inputAction.actionType
            && Owner == GameManager.Instance.LocalCharacter)
        {
            HUDController.Instance.actionbarPanel.Display_ClickOnSkill((int)inputAction.actionType, false, false);
            LaunchAiming();
        }
    }

    private void Update()
    {
        if (OnGCD)
        {
            GCD -= Time.deltaTime * WorldManager.Instance.GetTimeFactor(Owner.Team);
            if (GCD <= 0)
            {
                OnGCD = false;
            }
        }
    }

    #endregion

    #region Aiming

    private void LaunchAiming()
    {
        Debug.Log("Launnch aiming");

        if (currentAimedTarget != new Vector3(-10000, -10000, -10000)
            && CanLaunchAbility(CurrentAbility))
        {
            if (CurrentAbility.CurrentState == AbilityState.Avalaible)
            {
                Debug.Log("Aiming : Starting Cast");

                // Si l'ability est instant, current ability sera null après son lancement, donc stop aiming ne pourra pas cacher le targeting area
                CurrentAbility.HideTargetingArea();

                switch (CurrentAbility.AimingMode)
                {
                    case AbilityAimingMode.Target:
                        ClientCommand_StartCast_OnEntity(CurrentAbility, Owner.CurrentEntityTarget);//Owner.CurrentEntityTarget);
                        break;
                    case AbilityAimingMode.AoE:
                        ClientCommand_StartCast_OnPosition(CurrentAbility, currentAimedTarget);
                        break;
                    case AbilityAimingMode.Directionnal:
                    case AbilityAimingMode.FreeAimingPoint:
                        ClientCommand_StartCast_OnPosition(CurrentAbility, currentAimedTarget);
                        break;
                    case AbilityAimingMode.Self:
                        ClientCommand_StartCast_OnEntity(CurrentAbility, Owner);//Owner.CurrentEntityTarget);
                        break;
                    case AbilityAimingMode.Cleave:
                        ClientCommand_StartCast_OnPosition(CurrentAbility, currentAimedTarget);
                        break;
                    case AbilityAimingMode.None:
                        break;
                }
            }
            else
            {
                Debug.LogError("Couille dans le paté");
            }
        }

        StopAiming();
    }

    public void StopAiming()
    {
        if (CurrentAbility) // Peut déjà être null car EndAbility remet currentAbility à null . 
        {
            CurrentAbility.HideTargetingArea();
            HUDController.Instance.actionbarPanel.Display_ClickOnSkill(CurrentAbility.controllerIndex, false, false);
            //currentAbility = null;
        }

        IsPrevisualizing = false;
        currentAimedTarget = new Vector3(-10000, -10000, -10000);
    }

    #endregion

    #region Add / Remove

    public void Clear()
    {
        Debug.LogError("Ability Controller : Clearing Deck");

        // -1 Cause the last ability is the weapon ability and we don't want to remove it with this function
        for (int i = 0; i < Abilities.Length - 1; ++i)
        {
            if (Abilities[i] != null)
            {
                Abilities[i].OnRemoved();
                Destroy(Abilities[i].gameObject);
                Abilities[i] = null;
            }
        }
    }

    public void AddWeaponFiller(Ability weaponAbility)
    {
        Ability weaponAbilityInstance = Instantiate(weaponAbility, SkillbookParent.transform);
        weaponAbilityInstance.name = weaponAbility.name;

        weaponAbilityInstance.Init(this, CoreManager.Instance.coreSetting.DeckSize - 1, Owner);
        weaponAbilityInstance.abilityData = new AbilityData()
        {
            coreKey = weaponAbility.name,
        };

        Abilities[CoreManager.Instance.coreSetting.DeckSize - 1] = weaponAbilityInstance;
    }

    public void RemoveWeaponFiller()
    {
        if (Abilities[CoreManager.Instance.coreSetting.DeckSize - 1] != null)
        {
            Abilities[CoreManager.Instance.coreSetting.DeckSize - 1].OnRemoved();
            Destroy(Abilities[CoreManager.Instance.coreSetting.DeckSize - 1].gameObject);
            Abilities[CoreManager.Instance.coreSetting.DeckSize - 1] = null;
        }
    }

    public void AddPassive(IPassiveAbility passiveAbility)
    {

    }

    public void RemovePassive(IPassiveAbility passiveAbility)
    {

    }

    public void AddWeaponPassive(IWeaponPassiveAbility weaponPassive, StuffSetting weapon, string weaponDataKey)
    {
        Ability wpPassive = Instantiate(weaponPassive.Ability, SkillbookParent.transform);
        wpPassive.name = weaponPassive.Ability.name;

        IWeaponPassiveAbility iweaponPassive = (wpPassive as IWeaponPassiveAbility);
        iweaponPassive.WeaponSetting = weapon;
        iweaponPassive.WeaponDataKey = weaponDataKey;

        wpPassive.SetAbilityData(new AbilityData() { coreKey = weaponPassive.Ability.name });
        wpPassive.Init(this, CoreManager.Instance.coreSetting.DeckSize - 1, Owner);
        Passives.Add(wpPassive as IPassiveAbility);
    }

    public void RemoveWeaponPassiveByKey(string key)
    {
        for (int i = 0; i < Passives.Count; ++i)
        {
            IWeaponPassiveAbility weaponPassiveAbility = Passives[i] as IWeaponPassiveAbility;

            if (weaponPassiveAbility != null && weaponPassiveAbility.WeaponDataKey == key)
            {
                weaponPassiveAbility.Ability.OnRemoved();
                Destroy(weaponPassiveAbility.Ability.gameObject);
                Passives.RemoveAt(i);
                break;
            }
        }
    }
    #endregion

    #region Global cooldown
    public void StartGCD(Ability skill)
    {
        if (skill.CastTime < GlobalCooldown)
        {
            GCD = GlobalCooldown;
        }
        else
        {
            GCD = skill.CastTime;
        }

        OnGCD = true;
    }

    public bool NotInGlobalCooldown(Ability skill)
    {
        bool state = false;
        if (!OnGCD)
        {
            state = true;
        }
        if (OnGCD && !skill.IsConstrainedByGCD)
        {
            state = true;
        }
        return state;
    }
    #endregion

    #region Ability launching
    public void SelectAbility(int index) // Local 
    {
        SelectAbility(index, Owner.CurrentEntityTarget);
    }

    public virtual void SelectAbility(int index, Entity target)
    {
        if (Abilities.Length > index && Abilities[index] != null && !Abilities[index].IsPassive)
        {
            if (CurrentAbility == null || CurrentAbility.CurrentState == AbilityState.Avalaible || CurrentAbility.CurrentState == AbilityState.Unavalaible)
            {
                CurrentAbility = Abilities[index];

                IsPrevisualizing = true;

                if (CurrentAbility.AutoLaunch)
                {
                    UpdateTargeting(CanLaunchAbility(CurrentAbility));
                    LaunchAiming();
                }
            }
            else
            {
                Debug.Log("Cannot select new skill as current ability is executing.");
            }
        }
    }

    public virtual void Server_SelectAbility(Ability ability, Entity target, Vector3 position) { throw new Exception("not overloaded"); }

    public bool CanLaunchAbility(Ability ability)
    {
        return NotInGlobalCooldown(ability)
                    && ability.IsEquipable()
                    && ability.IsLaunchable();
        //&& Owner.ressourceSystem.CheckRessource(ability.costRessourceValue, ability.costRessourceType);
    }

    public void Request_EndAbility(Ability ability, AbilityEndMode endMode)
    {
        if (NetworkServer.IsOffline)
            RPC_EndSkill(ability.controllerIndex, (int)endMode);
        else
        {
            //TODO SYNCHRO
            photonView.RPC("RPC_EndSkill", RpcTarget.All, ability.controllerIndex, (int)endMode);
            PhotonNetwork.SendAllOutgoingCommands();
        }

    }

    public void AbilityEnded(Ability ended)
    {
        if (ended.HeatPerUse > 0)
        {
            int counter = 0;
            for (int i = 0; i < Abilities.Length; ++i)
            {
                if (Abilities[i] != null && Abilities[i] != ended && Abilities[i].AbilityMode == AbilityMode.Heat)
                {
                    counter++;
                }
            }

            if (counter > 0)
            {
                float heatDiminutionPerAbility = ended.HeatPerUse / counter;
                for (int i = 0; i < Abilities.Length; ++i)
                {
                    if (Abilities[i] != null && Abilities[i] != ended && Abilities[i].AbilityMode == AbilityMode.Heat)
                    {
                        Abilities[i].CurrentHeat -= heatDiminutionPerAbility;
                    }
                }
            }
        }

        if (CurrentAbility == ended)
            CurrentAbility = null;

        if (ended.AutoLaunch)
        {
            if (Owner.IsMine && InputsManager.Instance.IsKeyStaying((ActionType)ended.controllerIndex))
            {
                SelectAbility(ended.controllerIndex);
            }
            else
            {
                 ended.OnAutoLaunchEnded();
            }
        }
    }

    #endregion

    #region AnimationAction
    /*
        /// <summary>
        /// Déclenché par un évenement d'animation, la skillloop est appelée sur les clients par le serveur
        /// afin de garder une synchronisation..
        /// </summary>
        public virtual void ExecuteSkillLoop()
        {
            if (GameServer.IsMaster && CurrentAbility != null)
            {
                if (CurrentAbility.CurrentState == AbilityState.Executing
                    && CurrentAbility.ExecutionMode == AbilityLaunchMode.DrivenByAnimation)
                {
                    switch (CurrentAbility.AimingMode)
                    {
                        case AbilityAimingMode.Target:
                            if (Owner.CurrentEntityTarget != null)
                            {
                                Request_ExecuteSkillLoop_OnEntity(CurrentAbility, Owner.CurrentEntityTarget);
                            }
                            else
                            {
                                Debug.LogError("No target !");
                            }
                            break;
                        case AbilityAimingMode.AoE:
                            Request_ExecuteSkillLoop_OnPosition(CurrentAbility, Owner.CurrentPositionTarget);
                            break;
                        case AbilityAimingMode.Directionnal:
                            Request_ExecuteSkillLoop_OnPosition(CurrentAbility, (Owner.CurrentPositionTarget - Camera.main.transform.position) + Owner.transform.position);
                            break;
                    }
                }
            }
            else Debug.LogError("Cant execute skill loop");
        }*/

    #endregion

    #region Network
    public void Request_ResynchronizeDeck()
    {
        photonView.RPC("RPC_ResynchronizeDeck", RpcTarget.Others, (Owner as Character).characterData);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    [PunRPC]
    private void RPC_ResynchronizeDeck(CharacterData characterData)
    {
        Debug.Log("Resynchronizing deck");

        (Owner as Character).characterData = characterData;
        Clear();
        Init(Owner);
    }

    protected void ClientCommand_StartCast_OnEntity(Ability skill, Entity entityTarget)
    {
        IsPendingLaunch = true;
        // On envoie la requête client
        GameServer.networkServer.ClientCommand_AbilityController_StartCastOnEntity(Owner, skill, entityTarget);
        Debug.Log("Start Cast : " + skill.name + " on " + entityTarget);
    }

    public void ServerRequest_StartCast_OnEntity(Ability skill, Entity entityTarget, double startTime, bool check = true)
    {
        Owner.CurrentEntityTarget = entityTarget;
        // La requête client est examinée sur le serveur, si l'ability est lançable, le serveur déclencher une RPC sur tous les clients
        if (check && CanLaunchAbility(skill) || !check)
        {
            float hasteRatio = Owner.HasteRatio;
            float cost = skill.ComputeCost();

            GameServer.networkServer.ServerRPC_AbilityController_StartCastOnEntity(Owner, skill, entityTarget, startTime, hasteRatio, cost);
        }
        else
        {
            Debug.Log("Server : cannot launch " + skill);
            photonView.RPC("RPC_AbortLaunchAbility", RpcTarget.All);
            PhotonNetwork.SendAllOutgoingCommands();
        }
    }

    public void RPC_StartCastOnEntity(Ability ability, Entity target, float hasteRatio, float lag, float cost)
    {
        IsPendingLaunch = false;

        CurrentAbility = ability;
        //StartGCD(CurrentAbility);

        CurrentAbility.StartAbility(target, Vector3.negativeInfinity, hasteRatio, lag, cost);
    }

    protected void ClientCommand_StartCast_OnPosition(Ability skill, Vector3 aimedPos)
    {
        IsPendingLaunch = true;

        Debug.Log("Start Cast : " + skill.name);
        GameServer.networkServer.ClientCommand_AbilityController_StartCastOnPosition(Owner, skill, aimedPos);
    }

    public void ServerRequest_StartCast_OnPosition(Ability skill, Vector3 positionTarget, double startTime, bool check = true)
    {
        // La requête client est examinée sur le serveur, si l'ability est lançable, le serveur déclencher une RPC sur tous les clients
        if (check && CanLaunchAbility(skill) || !check)
        {
            float hasteRatio = Owner.HasteRatio;
            float cost = skill.ComputeCost();

            GameServer.networkServer.ServerRPC_AbilityController_StartCastOnPosition(Owner, skill, positionTarget, startTime, hasteRatio, cost);
        }
        else
        {
            Debug.Log("Server : cannot launch " + skill);
            photonView.RPC("RPC_AbortLaunchAbility", RpcTarget.All);
            PhotonNetwork.SendAllOutgoingCommands();
        }
    }

    public void RPC_StartCastOnPosition(Ability ability, Vector3 target, float hasteRatio, float lag, float cost)
    {
        IsPendingLaunch = false;

        CurrentAbility = ability;
        //StartGCD(CurrentAbility);
        CurrentAbility.StartAbility(null, target, hasteRatio, lag, cost);
    }

    public void Request_StartHitLoopEntity(Ability skill, Entity target = null)
    {
        GameServer.networkServer.ServerRequest_AbilityController_StartHitLoopEntity(Owner, skill, target);
    }

    public void Request_StartHitLoopPosition(Ability skill, Vector3 target)
    {
        GameServer.networkServer.ServerRequest_AbilityController_StartHitLoopPosition(Owner, skill, target);
    }

    // A voir plus tard, pour l'instant les executes skillloop concernent le pipeline de lancement via event d'animation qui a été mis en suspend pour des raisons de bugs
    protected void Request_ExecuteSkillLoop_OnEntity(Ability skill, Entity target, bool doAnim = true)
    {
        if (NetworkServer.IsOffline)
        {
            RPC_ExecuteSkillOnEntity(skill.controllerIndex, target.NetworkID, doAnim);
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"{Owner} execute {skill} on {target}");

                photonView.RPC("RPC_ExecuteSkillOnEntity", RpcTarget.All, skill.controllerIndex, target.NetworkID, doAnim);
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }

    protected void Request_ExecuteSkillLoop_OnPosition(Ability skill, Vector3 target, bool doAnim = true)
    {
        if (NetworkServer.IsOffline)
        {
            RPC_ExecuteSkill(skill.controllerIndex, target, doAnim);
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"{Owner} execute {skill} on {target}");

                photonView.RPC("RPC_ExecuteSkill", RpcTarget.All, skill.controllerIndex, target, doAnim);
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }

    /// <summary>
    /// Positive values are bonues, negative values are maluses
    /// </summary>
    /// <param name="ability"></param>
    /// <param name="newValue"></param>
    public void Request_AddCooldownBonusMalus(Ability ability, float newValue)
    {
        if (NetworkServer.IsOffline)
        {
            RPC_AddCooldownBonusMalus(ability.controllerIndex, newValue);
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_AddCooldownBonusMalus", RpcTarget.All, ability.controllerIndex, newValue);
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }
    #endregion

    #region Utils

    public Ability GetAbilityByKey(string abilityKey)
    {
        for (int i = 0; i < Abilities.Length; ++i)
        {
            if (Abilities[i] != null && Abilities[i].corekey == abilityKey)
            {
                return Abilities[i];
            }
        }
        return null;
    }

    public Ability GetAbilityByIndex(int index)
    {
        if (index >= 0 && index < Abilities.Length)
        {
            return Abilities[index];
        }
        Debug.LogError("No ability at index " + index + " on entity : " + Owner);
        return null;
    }

    public List<Ability> GetSkillsWithFlag(AbilityFlags flag)
    {
        List<Ability> flaggedSkill = new List<Ability>();

        for (int i = 0; i < Abilities.Length; ++i)
        {
            if (Abilities[i].AbilityFlags == flag)
            {
                flaggedSkill.Add(Abilities[i]);
            }
        }
        return flaggedSkill;
    }

    public int GetIndex(Ability skill)
    {
        for (int i = 0; i < Abilities.Length; ++i)
        {
            if (Abilities[i] == skill)
            {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region RPC
    [PunRPC]
    private void RPC_AddCooldownBonusMalus(int skillbookIndex, float newValue)
    {
        if (Abilities.Length > skillbookIndex && Abilities[skillbookIndex] != null)
        {
            Abilities[skillbookIndex].AddCooldownBonusMalus(newValue);
        }
    }
    [PunRPC]
    private void RPC_AbortLaunchAbility()
    {
        Debug.Log("Server => Abort launch ability");
        IsPendingLaunch = false;
    }

    [PunRPC]
    public void RPC_ExecuteSkill(int skillbookIndex, Vector3 target, bool init = true)
    {
        if (Abilities.Length > skillbookIndex && Abilities[skillbookIndex] != null)
        {
            Abilities[skillbookIndex].ExecuteSkill(target);
        }
    }

    [PunRPC]
    public void RPC_ExecuteSkillOnEntity(int skillbookIndex, short targetID, bool init = true)
    {
        if (Abilities.Length > skillbookIndex && Abilities[skillbookIndex] != null)
        {
            Abilities[skillbookIndex].ExecuteSkill(GameServer.Instance.GetEntityFromID(targetID));
        }
    }

    [PunRPC]
    public void RPC_AutoCancelSkill(int abilityIndex)
    {
        if (Abilities.Length > abilityIndex && Abilities[abilityIndex] != null)
        {
            Abilities[abilityIndex].AutoCancel();
        }
    }

    [PunRPC]
    public void RPC_EndSkill(int abilityIndex, int endCode)
    {
        if (Abilities.Length > abilityIndex && Abilities[abilityIndex] != null)
        {
            Abilities[abilityIndex].EndAbility((AbilityEndMode)endCode);
        }
    }

    public void RPC_StartHitLoopEntity(int abilityIndex, Entity target)
    {
        if (Abilities.Length > abilityIndex && Abilities[abilityIndex] != null)
        {
            Abilities[abilityIndex].StartHitLoop(target);
        }
    }

    public void RPC_StartHitLoopPosition(int abilityIndex, Vector3 position)
    {
        if (Abilities.Length > abilityIndex && Abilities[abilityIndex] != null)
        {
            Abilities[abilityIndex].StartHitLoop(position);
        }
    }

    #endregion

    #region Stream

    public bool IsExecutingStreamAbility = false;
    public float LastSentTime = 0;
    public float DeltaTime = 0;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!IsExecutingStreamAbility || CurrentAbility == null)
            return;

        if (stream.IsWriting)
        {
            DeltaTime = (float)PhotonNetwork.Time - LastSentTime;
            LastSentTime = (float)PhotonNetwork.Time;
            stream.SendNext((float)DeltaTime);
            CurrentAbility.OnStreamWrite(stream, info, DeltaTime);
        }
        else
        {
            DeltaTime = (float)stream.ReceiveNext();
            CurrentAbility.OnStreamRead(stream, info, DeltaTime);
        }
    }

    #endregion
}
