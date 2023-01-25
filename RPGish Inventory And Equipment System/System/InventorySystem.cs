using Incursion.Backend;
using IncursionDAL;
using Sirenix.OdinInspector;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Interface;
using SteamAndMagic.Interface.DragNDrop;
using SteamAndMagic.Systems.Economics;
using SteamAndMagic.Systems.Items;
using SteamAndMagic.Systems.LocalizationManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SteamAndMagic.Systems.Inventory
{
    public enum ItemContainerType
    {
        Equipment = 0,
        InventoryMain = 1,
        Shop = 2,
        ShopInventory = 3,
        Bag3,
        Bag4,
        Wallet = 6,
        CraftInput = 7,
        CraftOutput = 8,
        Lootbag = 9,
        Mailbox = 10,    
    }

    [Serializable]
    public class InventoryContainer
    {
        public bool IsActive = false;
        public ItemContainerType ContainerType;
        public int Size;

        // Collection for spatial/data relation
        [ShowInInspector] public Dictionary<int, ItemData> Slot_Data_Items = new Dictionary<int, ItemData>();

        public void InitializeContainer()
        {
            Slot_Data_Items.Clear();
            for (int i = 0; i < Size; ++i)
            {
                Slot_Data_Items.Add(i, null);
            }
        }

        public void PopulateContainer(List<ItemData> itemDatas)
        {
            for (int i = 0; i < itemDatas.Count; ++i)
            {
                if (itemDatas[i].state == -1000)
                    continue;

                if (Slot_Data_Items[itemDatas[i].state] != null && Slot_Data_Items[itemDatas[i].state] != itemDatas[i])
                {
                    Debug.LogError($"Trying to add {itemDatas[i].coreKey}_{itemDatas[i].uniqueKey} on a slot occupied by {Slot_Data_Items[itemDatas[i].state].coreKey}_{Slot_Data_Items[itemDatas[i].state].uniqueKey}.");
                    continue;
                }

                Slot_Data_Items[itemDatas[i].state] = itemDatas[i];
            }
        }

        public void AddItem(ItemData data)
        {
            if (Slot_Data_Items[data.state] != null)
            {
                Debug.LogError($"Trying to add {data.coreKey} on a slot occupied by {Slot_Data_Items[data.state].coreKey}.");
                return;
            }

            if (Slot_Data_Items.Count > data.state)
                Slot_Data_Items[data.state] = data;
            else Debug.LogError("Container doesn't contain a slot for the index " + data.state);
        }

        public void RemoveItem(ItemData data)
        {
            if (Slot_Data_Items.Count > data.state)
                Slot_Data_Items[data.state] = null;
            else Debug.LogError("Container doesn't contain a slot for the index " + data.state);
        }

        public ItemData GetItemDataByUniqueID(string uniqueID)
        {
            for (int i = 0; i < Slot_Data_Items.Count; ++i)
            {
                var pair = Slot_Data_Items.ElementAt(i);
                if (pair.Value != null && pair.Value.uniqueKey == uniqueID)
                    return pair.Value;
            }

            Debug.LogError("Data not found in container for " + uniqueID);
            return null;
        }

        public ItemData GetItemDataByCoreKey(string coreKey)
        {
            for (int i = 0; i < Slot_Data_Items.Count; ++i)
            {
                var pair = Slot_Data_Items.ElementAt(i);
                if (pair.Value != null && pair.Value.coreKey == coreKey)
                    return pair.Value;
            }

            Debug.Log("Data not found in container for " + coreKey);
            return null;
        }

        public bool IsItemDataInContainer(ItemData data)
        {
            for (int i = 0; i < Slot_Data_Items.Count; ++i)
            {
                var pair = Slot_Data_Items.ElementAt(i);
                if (pair.Value != null && pair.Value == data)
                    return true;
            }
            return false;
        }
    }

    public class InventorySystem : MonoBehaviour
    {
        [HideInInspector]
        public List<ItemData> itemsData => BackendManager.Instance.Character.inventory;
        public List<InventoryContainer> InventoryContainers = new List<InventoryContainer>();

        public bool AutoSave { get; set; }

        private IInventorySystemOwner owner;

        public delegate void AddNewItemHandler(IInventorySystemOwner owner, ItemData data);
        public static event AddNewItemHandler OnAddNewItem;

        public delegate void UpdateItemHandler(IInventorySystemOwner owner, ItemData data);
        public static event UpdateItemHandler OnUpdateItem;

        public void OnEnable()
        {
            InventoryControllerEventHandler.OnAddItem += InventoryControllerHandler_OnAddItem;
            InventoryControllerEventHandler.OnRemoveItem += InventoryControllerHandler_OnRemoveItem;
            InventoryControllerEventHandler.OnSaveInventory += CommitInventory;
            InventoryControllerEventHandler.OnDisplayInventory += InventoryControllerHandler_OnDisplayInventory;
            DraggableTrash.OnDraggableComponentTrashed += Trash_OnDraggableComponentTrashed;
        }

        public void OnDisable()
        {
            InventoryControllerEventHandler.OnAddItem -= InventoryControllerHandler_OnAddItem;
            InventoryControllerEventHandler.OnRemoveItem -= InventoryControllerHandler_OnRemoveItem;
            InventoryControllerEventHandler.OnSaveInventory -= CommitInventory;
            InventoryControllerEventHandler.OnDisplayInventory -= InventoryControllerHandler_OnDisplayInventory;
            DraggableTrash.OnDraggableComponentTrashed -= Trash_OnDraggableComponentTrashed;
        }

        public void Init(IInventorySystemOwner owner)
        {
            this.owner = owner;

            // verification qu'il n'y a eu aucune duplication d'objets
            for (int i = 0; i < itemsData.Count; ++i)
            {
                ItemData toCheck = itemsData[i];

                for (int j = 0; j < itemsData.Count; ++j)
                {
                    if (itemsData[j] != toCheck && itemsData[j].uniqueKey == toCheck.uniqueKey)
                    {
                        Debug.LogError($"Duplicate d'objet dans l'inventaire pour l'objet {toCheck.coreKey}{toCheck.uniqueKey}. Cela devrait être impossible.");
                    }
                }
            }

            // Remplir les containers
            for (int i = 0; i < InventoryContainers.Count; ++i)
            {
                if (!InventoryContainers[i].IsActive) // Not all containers are avalaible to the player when he starts the game. Some needs to be unlocked.
                    continue;

                InventoryContainers[i].InitializeContainer();

                List<ItemData> itemsForContainer = itemsData.Where(t => t.container == (int)InventoryContainers[i].ContainerType).ToList();
                InventoryContainers[i].PopulateContainer(itemsForContainer);
            }

            // Check for forgotten items in craft
            for (int i = 0; i < itemsData.Count; ++i)
            {
                if (itemsData[i].container == (int)ItemContainerType.CraftInput)
                {
                    Debug.LogError(itemsData[i] + " was left in craft input, go to mailbox");

                    MailBoxManagerEventHandler.CreateItemDataMailRequest(itemsData[i]);
                }
                else if (itemsData[i].container == (int)ItemContainerType.CraftOutput)
                {
                    Debug.LogError(itemsData[i] + " was left in craft output, go to mailbox");

                    MailBoxManagerEventHandler.CreateItemDataMailRequest(itemsData[i]);
                }
            }
        }

        #region Inventory and ItemData Operations

        public bool AddItemByData(ItemData itemData)
        {
            ItemSetting setting = CoreManager.Instance.GetItemByKey(itemData.coreKey);
            if (setting.stackable)
            {
                ItemData stackableData = FindItemDataForSetting(setting);
                if (stackableData != null)
                {
                    // Merging datas
                    stackableData.value += itemData.value;

                    if (itemsData.Contains(itemData))
                    {
                        DeleteItemData(itemData);
                    }

                    OnUpdateItem?.Invoke(owner, stackableData);
                    BackendManager.Instance.ItemData_Update(stackableData, AutoSave);

                    return true;
                }
            }

            // Stackable or not, if no existing data, we try to find a new position to place our item
            int freeIndex = 0;
            int containerIndex = 0;
            if (FindFreeInventoryPosition(out freeIndex, out containerIndex))
            {
                itemData.container = containerIndex;
                itemData.state = freeIndex;

                // Adding the reference of data and setting into the Items 
                InventoryContainers[InventoryContainers.FindIndex(t => (int)t.ContainerType == containerIndex)].AddItem(itemData);
                OnAddNewItem?.Invoke(owner, itemData);
                BackendManager.Instance.ItemData_Create(itemData, AutoSave);

                return true;
            }
            else
            {
                Debug.LogError("No slot avalaible in inventory.");
                AudioManager.Instance.Play_UINotAllowed();

                return false;
            }
        }

        /// <summary>
        /// Update inventory with a setting (create or update the related ItemData)
        /// </summary>
        /// <param name="itemSetting"></param>
        /// <param name="amount"></param>
        public bool AddItemBySetting(ItemSetting itemSetting, int amount = 1)
        {
            if (itemSetting.stackable)
            {
                ItemData itemData = FindItemDataForSetting(itemSetting);
                if (itemData != null)
                {
                    // Incrementing the value
                    itemData.value += amount;

                    OnUpdateItem?.Invoke(owner, itemData);
                    BackendManager.Instance.ItemData_Update(itemData, AutoSave);

                    return true;
                }
            }

            int freeIndex = 0;
            int containerIndex = 0;
            if (FindFreeInventoryPosition(out freeIndex, out containerIndex))
            {
                ItemData itemData = GenerateItemData(itemSetting, freeIndex, containerIndex, amount);

                // Adding the reference of data and setting into the Items 
                InventoryContainers[InventoryContainers.FindIndex(t => (int)t.ContainerType == containerIndex)].AddItem(itemData);
                OnAddNewItem?.Invoke(owner, itemData);
                BackendManager.Instance.ItemData_Create(itemData, AutoSave);
            }
            else
            {
                Debug.LogError("No slot avalaible in inventory.");
                AudioManager.Instance.Play_UINotAllowed();

                return false;
            }

            return true;
        }

        public void ModifyItemDataQuantity(ItemData toModify, int newValue)
        {
            toModify.value = newValue;

            if (toModify.value <= 0)
            {
                Debug.LogError("Deleting " + toModify.coreKey + " cause has no amount left");
                DeleteItemData(toModify);
            }
            else
            {
                Debug.Log("Modify " + toModify.coreKey + " amount");
                CreateOrUpdateItemData(toModify);
            }
        }

        public void DeleteItemData(ItemData item)
        {
            int index = InventoryContainers.FindIndex(t => (int)t.ContainerType == item.container);
            if (index != -1)
                InventoryContainers[index].RemoveItem(item);

            item.state = -1000;
            OnUpdateItem?.Invoke(owner, item);

            BackendManager.Instance.ItemData_Update(item, true);
            Debug.Log("Deleted " + item.coreKey + " amount");
        }

        /// <summary>
        /// Create or Update inventory with an existing ItemData, not handling of positioning etc.. 
        /// </summary>
        /// <param name="itemData"></param>
        private void CreateOrUpdateItemData(ItemData itemData)
        {
            if (!IsItemDataInInventory(itemData)) // if not found, we create
            {
                Debug.LogError("CreateOrUpdateItemData => Not Found in inventory. Adding it");
                AddItemByData(itemData);
            }
            else
            {
                UpdateItemData(itemData);
            }
        }

        public void UpdateItemData(ItemData itemData)
        {
            // Updated the value or the state
            OnUpdateItem?.Invoke(owner, itemData);
            BackendManager.Instance.ItemData_Update(itemData, AutoSave);
        }

        /// <summary>
        /// Generates an item with its setting. 
        /// Calls ItemSetting.OnCreated function (randing stats for Stuff, etc...)
        /// </summary>
        /// <param name="blueprint"></param>
        /// <returns></returns>
        public static ItemData GenerateItemData(ItemSetting blueprint, int state = -1000, int container = 1, int value = 0)
        {
            ItemData itemData = new ItemData();
            itemData.characterKey = BackendManager.Instance.Character.coreKey;
            itemData.coreKey = blueprint.name;
            itemData.uniqueKey = UtilityHelper.GenerateGuidToString();
            itemData.state = state;
            itemData.container = container;
            itemData.value = value;
            itemData.stats = new List<Attributes.ItemStat>();
            blueprint.OnCreated(itemData);
            Debug.Log("Generated item : " + itemData.coreKey + "  " + itemData.uniqueKey);
            return itemData;
        }

        public void CommitInventory(InventoryController context)
        {
            if (context.owner == owner)
                BackendManager.Instance.ItemData_Update(null, true);
        }

        #endregion

        #region UI Control Events

        private void InventoryControllerHandler_OnAddItem(InventoryController handler, ItemData itemData)
        {
            var container = InventoryContainers.Find(t => (int)t.ContainerType == itemData.container);
            if (container != null)
            {
                container.AddItem(itemData);
            }
            else
            {
                Debug.LogError($"InventoryControllerHandler_OnAddItem : Container of index {itemData.container} not found in the inventory.");
            }

            if (handler.owner == owner)
                BackendManager.Instance.ItemData_Update(itemData, AutoSave);
        }

        /// <summary>
        /// When player moves items in inventory interface, these callbacks update the datas in backend
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="itemData"></param>
        private void InventoryControllerHandler_OnRemoveItem(InventoryController handler, ItemData itemData)
        {
            var container = InventoryContainers.Find(t => (int)t.ContainerType == itemData.container);
            if (container != null)
            {
                container.RemoveItem(itemData);
            }
            else
            {
                Debug.LogError($"InventoryControllerHandler_OnRemoveItem : Container of index {itemData.container} not found in the inventory.");
            }
            // Remove Item is when the player drag an item on the grid, so we just do nothing because the drop will trigger the Add
        }

        private void InventoryControllerHandler_OnDisplayInventory(InventoryController handler)
        {
            if (handler.owner == owner)
            {
                InventoryContainer inventorySystemContainer = InventoryContainers.FindItem(t => t.ContainerType == handler.ItemContainerType);
                if (inventorySystemContainer != null)
                {
                    // Vidange complète de la grille de draggable component 
                    for (int i = 0; i < handler.ContainerGrid.Grid.Count; ++i)
                    {
                        if (handler.ContainerGrid.Grid[i] != null)
                        {
                            PoolManager.Instance.DespawnGo(handler.ContainerGrid.Grid[i].gameObject);
                            handler.ContainerGrid.Grid[i] = null;
                        }
                    }

                    // Regenération des UIInventoryItem
                    for (int i = 0; i < inventorySystemContainer.Slot_Data_Items.Count; ++i)
                    {
                        var pair = inventorySystemContainer.Slot_Data_Items.ElementAt(i);

                        if (pair.Value != null)
                        {
                            UIInventoryItem item = handler.CreateUIItemFromData(pair.Value);
                            handler.ContainerGrid.PopulateObjectOnSlot(i, item.GetComponent<DraggableComponent>());
                        }
                    }
                }
                else
                {
                    Debug.LogError($"The container {handler.ItemContainerType.ToString()} is not active.");
                }
            }
        }

        private void Trash_OnDraggableComponentTrashed(DraggableComponent obj)
        {
            UIInventoryItem item = obj.gameObject.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                DeleteItemData(item.itemData);
            }
        }

        #endregion

        #region Tools

        public ItemData FindItemDataForSetting(ItemSetting itemSetting)
        {
            if (itemSetting == null)
            {
                Debug.LogError("Item setting null, couldn't search in inventory !");
                return null;
            }

            for (int i = 0; i < InventoryContainers.Count; ++i)
            {
                ItemData itemData = InventoryContainers[i].GetItemDataByCoreKey(itemSetting.coreKey);
                if (itemData != null)
                    return itemData;
            }

            return null;
        }

        public bool IsItemDataInInventory(ItemData itemData)
        {
            for (int i = 0; i < InventoryContainers.Count; ++i)
            {
                if (InventoryContainers[i].IsItemDataInContainer(itemData))
                    return true;
            }

            return false;
        }

        public bool FindFreeContainerPosition(ItemContainerType containerType, out int result) // 1 is the main inventory, 
        {
            InventoryContainer container = InventoryContainers.FindItem(t => t.ContainerType == containerType);
            if (container != null)
            {
                for (int i = 0; i < container.Slot_Data_Items.Count; ++i)
                {
                    if (container.Slot_Data_Items[i] == null)
                    {
                        result = i;
                        return true;
                    }
                }
            }
            else
            {
                Debug.LogError("No container existing of type " + containerType.ToString());
            }

            result = 0;
            return false;
        }

        public bool FindFreeInventoryPosition(out int position, out int container)
        {
            for (int i = 0; i < InventoryContainers.Count; ++i)
            {
                if (InventoryContainers[i].IsActive
                    && !InventoryContainers[i].ContainerType.IsAny(ItemContainerType.Equipment))
                {
                    if (FindFreeContainerPosition(InventoryContainers[i].ContainerType, out position))
                    {
                        container = (int)InventoryContainers[i].ContainerType;
                        return true;
                    }
                }
            }

            position = -1;
            container = -1;
            return false;
        }

        public ItemData GetItemDataByUniqueKey(string uniqueKey)
        {
            for(int i = 0; i < itemsData.Count; ++i)
            {
                if(itemsData[i].uniqueKey == uniqueKey)
                {
                    return itemsData[i];
                }
            }

            return null;
        }
        #endregion
    }
}
