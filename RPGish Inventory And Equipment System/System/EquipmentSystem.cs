using Incursion.Backend;
using IncursionDAL;
using Photon.Pun;
using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using SteamAndMagic.Interface;
using SteamAndMagic.Interface.DragNDrop;
using SteamAndMagic.Photon;
using SteamAndMagic.Systems.Abilities;
using SteamAndMagic.Systems.Attributes;
using SteamAndMagic.Systems.Economics;
using SteamAndMagic.Systems.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.Inventory
{
    public class EquipmentSystem : MonoBehaviourPun
    {
        public Character owner;

        [ShowInInspector, ReadOnly] public List<ItemData> equipment = new List<ItemData>();
        [ShowInInspector, ReadOnly] public Dictionary<StuffPartType, StuffSetting> equipmentSettings = new Dictionary<StuffPartType, StuffSetting>();

        /// <summary>
        /// The stats from equipment that are currently applied on the Character's attribute
        /// </summary>
        [ShowInInspector, ReadOnly] private Dictionary<AbilityEquipmentType, int> currentEquipmentTypeWeights = new Dictionary<AbilityEquipmentType, int>();
        [ShowInInspector, ReadOnly] private Dictionary<ItemData, List<AttributeModifier>> _equipments_attributeModifiers = new Dictionary<ItemData, List<AttributeModifier>>();

        #region Accessors
        public WeaponSetting RightWeapon
        {
            get
            {
                StuffSetting rigthWeaponSetting = null;
                if (equipmentSettings.TryGetValue(StuffPartType.RightWeapon, out rigthWeaponSetting))
                {
                    return rigthWeaponSetting as WeaponSetting;
                }
                return null;
            }
        }

        public WeaponSetting LeftWeapon
        {
            get
            {
                StuffSetting leftweaponSetting = null;
                if (equipmentSettings.TryGetValue(StuffPartType.LeftWeapon, out leftweaponSetting))
                {
                    return leftweaponSetting as WeaponSetting;
                }
                return null;
            }
        }

        public Dictionary<AbilityEquipmentType, int> CurrentEquipmentTypeWeights
        {
            get
            {
                return currentEquipmentTypeWeights;
            }
        }
        #endregion


        public void OnEnable()
        {
            EquipmentControllerHandler.OnAddEquipment += EquipmentControllerHandler_OnAddEquipment;
            EquipmentControllerHandler.OnRemoveEquipment += EquipmentControllerHandler_OnRemoveEquipment;
            EquipmentControllerHandler.OnSaveEquipment += CommitEquipment;
            EquipmentControllerHandler.OnDisplayEquipment += EquipmentControllerHandler_OnDisplayEquipment;
        }

        public void OnDisable()
        {
            EquipmentControllerHandler.OnAddEquipment -= EquipmentControllerHandler_OnAddEquipment;
            EquipmentControllerHandler.OnRemoveEquipment -= EquipmentControllerHandler_OnRemoveEquipment;
            EquipmentControllerHandler.OnSaveEquipment -= CommitEquipment;
            EquipmentControllerHandler.OnDisplayEquipment -= EquipmentControllerHandler_OnDisplayEquipment;
        }

        public void Init(Character owner)
        {
            this.owner = owner;

            LoadEquipment();

            this.EquipmentSystemInitialized();
        }

        public void LoadEquipment()
        {
            equipment.Clear();
            equipmentSettings.Clear();

            if (owner.IsMine)
            {
                HUDController.Instance.actionbarPanel.DesactivateRessourceBar();
            }

            for (int i = 0; i < owner.characterData.inventory.Count; ++i)
            {
                if (owner.characterData.inventory[i].container != (int)ItemContainerType.Equipment)
                    continue;

                if (owner.characterData.inventory[i].state < 0)
                    continue;

                if (equipment.Exist(t => t.state == owner.characterData.inventory[i].state))
                {
                    Debug.LogError("Cannot equip two piece on the same SLOT at state " + owner.characterData.inventory[i].state + equipment.Find(t => t.state == owner.characterData.inventory[i].state).coreKey);
                    //TODO placement de l'objet dans l'inventaire à un slot libre
                    // TODO comprendre pourquoi deux equipements sont au meme endroit
                    if (!owner.inventorySystem.AddItemByData(owner.characterData.inventory[i]))
                    {
                        MailBoxManagerEventHandler.CreateItemDataMailRequest(owner.characterData.inventory[i]);
                    }

                    continue;
                }

                AddEquipment(owner.characterData.inventory[i], CoreManager.Instance.GetItemByKey(owner.characterData.inventory[i].coreKey) as StuffSetting);
            }
        }
               
        /// <summary>
        /// Local callback of inventory controller UI
        /// </summary>
        /// <param name="context"></param>
        /// <param name="itemData"></param>
        private void EquipmentControllerHandler_OnAddEquipment(EquipmentController context, ItemData itemData)
        {
            if (context.owner == owner as IEquipmentSystemOwner)
            {
                if (NetworkServer.IsOffline)
                {
                    RPC_AddEquipment(itemData);
                }
                else
                {
                    photonView.RPC("RPC_AddEquipment", RpcTarget.All, itemData);
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }

        public void AddEquipment(ItemData itemData, ItemSetting itemSetting = null)
        {
            if (itemSetting == null)
                itemSetting = CoreManager.Instance.GetItemByKey(itemData.coreKey);

            if (itemSetting == null)
                return;

            StuffSetting stuffSetting = itemSetting as StuffSetting;
            // Backend is updated locally
            if (owner.IsMine)
                BackendManager.Instance.ItemData_Update(itemData, true);

            equipment.Add(itemData);

            if (equipmentSettings.ContainsKey(stuffSetting.StuffPartType))
            {
                equipmentSettings[stuffSetting.StuffPartType] = stuffSetting;
            }
            else
            {
                equipmentSettings.Add(stuffSetting.StuffPartType, stuffSetting);
            }

            WeaponSetting weaponSetting = itemSetting as WeaponSetting;

            if (weaponSetting != null)
            {
                if (weaponSetting.side == RPGCharacterAnims.Side.Right)
                {
                    if (owner.IsMine)
                    {
                        HUDController.Instance.actionbarPanel.ToggleRessourceBar(weaponSetting);
                    }

                    owner.abilityController.AddWeaponFiller(weaponSetting.weaponAbility);
                }

                if (weaponSetting.weaponPassiveAbility != null)
                {
                    owner.abilityController.AddWeaponPassive(weaponSetting.weaponPassiveAbility as IWeaponPassiveAbility, weaponSetting, itemData.uniqueKey);
                }

                owner.CanBlock = weaponSetting.CanBlockWithWeapon;
            }

            // event listenned by character animation system for weapon animation handling
            this.OnEquipRequest(itemSetting);

            InitializeEquipmentTypeWeights();

            if (GameServer.IsMaster)
            {
                _equipments_attributeModifiers.Add(itemData, new List<AttributeModifier>());

                for (int j = 0; j < itemData.stats.Count; ++j)
                {
                    AttributeModifier equipmentModifier = new AttributeModifier();
                    equipmentModifier.stat = itemData.stats[j].stat;
                    equipmentModifier.value = itemData.stats[j].value;
                    equipmentModifier.target = TargetedStat.Base;
                    equipmentModifier.mode = ModifierMode.Raw;
                    equipmentModifier.isEquipmentModifier = true;

                    _equipments_attributeModifiers[itemData].Add(owner.AddAttributeModifier(equipmentModifier));
                }
            }
        }

        [PunRPC]
        private void RPC_AddEquipment(ItemData itemData)
        {
            AddEquipment(itemData);
        }

        /// <summary>
        /// Local callback of inventory controller UI
        /// </summary>
        /// <param name="context"></param>
        /// <param name="itemData"></param>
        private void EquipmentControllerHandler_OnRemoveEquipment(EquipmentController context, ItemData itemData)
        {
            if (context.owner == owner as IEquipmentSystemOwner)
            {
                /* if (owner.IsLocalCharacter)
                     // Backend is updated locally
                     BackendManager.Instance.InventoryRepository_Update(itemData, false);*/

                if (NetworkServer.IsOffline)
                {
                    RPC_RemoveEquipment(itemData);
                }
                else
                {
                    photonView.RPC("RPC_RemoveEquipment", RpcTarget.All, itemData);
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }

        public void RemoveEquipment(ItemData itemData, ItemSetting itemSetting = null)
        {
            if (itemSetting == null)
                itemSetting = CoreManager.Instance.GetItemByKey(itemData.coreKey);

            StuffSetting stuffSetting = itemSetting as StuffSetting;

            if (!equipment.Contains(itemData))
            {
                Debug.LogError("Trying to remove an item that is not currently equiped.");
                return;
            }

            equipment.Remove(itemData);

            if (equipmentSettings.ContainsKey(stuffSetting.StuffPartType))
            {
                equipmentSettings.Remove(stuffSetting.StuffPartType);
            }
            else
            {
                Debug.LogError("Setting doesn't exist for this stuff part type in equipment.");
            }

            WeaponSetting weaponSetting = stuffSetting as WeaponSetting;

            if (weaponSetting != null)
            {
                if (weaponSetting.side == RPGCharacterAnims.Side.Right)
                {
                    if (owner.IsMine)
                    {
                        HUDController.Instance.actionbarPanel.DesactivateRessourceBar();
                    }

                    owner.abilityController.RemoveWeaponFiller();
                }

                if (weaponSetting.weaponPassiveAbility != null)
                {
                    owner.abilityController.RemoveWeaponPassiveByKey(itemData.uniqueKey);
                }

                owner.CanBlock = false;
            }

            this.UnequipRequest(itemSetting);

            InitializeEquipmentTypeWeights();

            if (GameServer.IsMaster)
            {
                if (_equipments_attributeModifiers.ContainsKey(itemData))
                {
                    foreach(var modifier in _equipments_attributeModifiers[itemData])
                    {
                        owner.RemoveAttributeModifier(modifier);
                    }
                }

                _equipments_attributeModifiers.Remove(itemData);
            }
        }

        [PunRPC]
        private void RPC_RemoveEquipment(ItemData itemData)
        {
            RemoveEquipment(itemData);
        }

        private void EquipmentControllerHandler_OnDisplayEquipment(EquipmentController handler)
        {
            if ((handler.owner as Character) == owner)
            {
                for (int i = 0; i < equipment.Count; ++i)
                {
                    if (equipment[i].state < 0)
                        continue;

                    if (!handler.UIInventory.ContainsKey(equipment[i].uniqueKey))
                    {
                        // Adding UI Inventory
                        Debug.Log("Equipment create " + equipment[i].coreKey);
                        UIInventoryItem item = handler.CreateUIItemFromData(equipment[i]);

                        // we are using Mathf.Abs here cause the state are saved below 0 for the equipment
                        handler.ContainerGrid.PopulateObjectOnSlot(equipment[i].state, item.GetComponent<DraggableComponent>());
                    }
                }
            }
        }

        public void CommitEquipment(EquipmentController context)
        {
            if (context.owner == owner as IEquipmentSystemOwner)
                BackendManager.Instance.ItemData_Update(null, true);
        }

       /* public void SwapWeapon(WeaponSetting oldWeapon, WeaponSetting newWeapon)
        {
            owner.characterAnimationSystem.Request_SwitchWeapons(oldWeapon, newWeapon);
        }*/

        #region Gestion Equipement et Sets

        private void InitializeEquipmentTypeWeights()
        {
            CurrentEquipmentTypeWeights.Clear();

            foreach (int i in Enum.GetValues(typeof(AbilityEquipmentType)))
            {
                CurrentEquipmentTypeWeights.Add((AbilityEquipmentType)i, GetWeightForEquipmentType((AbilityEquipmentType)i));
            }
        }

        private int GetWeightForEquipmentType(AbilityEquipmentType abilityEquipmentType)
        {
            int value = 0;
            foreach (var stuff in equipmentSettings)
            {
                if (stuff.Value.AbilityEquipmentType == abilityEquipmentType)
                {
                    value += stuff.Value.AbilityEquipmentWeight;
                }
            }

            return value;
        }
        #endregion
    }

    public static class EquipmentSystemEventHandler
    {
        public delegate void OnEquipHandler(EquipmentSystem context, ItemSetting setting);
        public static event OnEquipHandler OnEquip;
        public static void OnEquipRequest(this EquipmentSystem context, ItemSetting setting) => OnEquip?.Invoke(context, setting);

        public delegate void OnUnequipHandler(EquipmentSystem owner, ItemSetting setting);
        public static event OnUnequipHandler OnUnequip;
        public static void UnequipRequest(this EquipmentSystem context, ItemSetting setting) => OnUnequip?.Invoke(context, setting);

        public delegate void EquipmentSystemInitializedHandler(EquipmentSystem context);
        public static event EquipmentSystemInitializedHandler OnEquipmentSystemInitialized;
        public static void EquipmentSystemInitialized(this EquipmentSystem equipmentSystem) => OnEquipmentSystemInitialized?.Invoke(equipmentSystem);
    }
}
