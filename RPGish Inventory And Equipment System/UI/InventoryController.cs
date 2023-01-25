using IncursionDAL;
using SteamAndMagic.Audio;
using SteamAndMagic.Interface.DragNDrop;
using SteamAndMagic.Systems.Inventory;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class InventoryController : UIItemContainer
    {
        [Header("Sounds")]
        public AudioClipInfo DropClip;
        public AudioClipInfo DragClip;

        public UIItemInteractionMode ItemsInteractabilityMode;

        public ItemContainerType ItemContainerType;
        public IInventorySystemOwner owner { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            InventorySystem.OnAddNewItem += InventorySystem_OnAddedNewItem;
            InventorySystem.OnUpdateItem += InventorySystem_OnUpdatedItem;

            this.DisplayedInventoryRequest();            
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            InventorySystem.OnAddNewItem -= InventorySystem_OnAddedNewItem;
            InventorySystem.OnUpdateItem -= InventorySystem_OnUpdatedItem;
        }

        // Initialized By Inventory System When Game Launches
        public void Init(IInventorySystemOwner owner)
        {
            this.owner = owner;
        }

        public UIInventoryItem CreateUIItemFromData(ItemData data)
        {
            UIInventoryItem item =
                PoolManager.Instance.SpawnGo(inventoryItem_Prefab.gameObject, Vector3.zero, ContainerGrid.Grid_Transform)
                .GetComponent<UIInventoryItem>();

            item.Initialize(data);
            item.CurrentInteractability = ItemsInteractabilityMode;
            return item;
        }

        public void SortInventory()
        {
            // layout Populate() with a dictionnary order
        }

        public void RestackAllStackableItems()
        {

        }

        /*/// <summary>
        /// Méthode spéciale liée à l'inventaire qui contient plusieurs grid. On ne peux pas effectuer un 
        /// </summary>
        /// <param name="draggableComponent"></param>
        public bool HandleDropAuto(DraggableComponent draggableComponent)
        {
            UIInventoryItem item = draggableComponent.gameObject.GetComponent<UIInventoryItem>();
            if (item == null)
                return false;

            int container = -1;
            int slot = -1;
            if(owner.inventorySystem.FindFreeInventoryPosition(out slot, out container))
            {
                // Do drop on container.


                return true;
            }

            return false;
        }
*/
        private void InventorySystem_OnAddedNewItem(IInventorySystemOwner owner, ItemData data)
        {
            if (this.owner != owner
                || data.container != (int)ItemContainerType)
                return;

            UIInventoryItem item = CreateUIItemFromData(data);

            // Inventory System looked for a free index and set it on data.state so if it's not free there, something wrong happened. UI Item is occupying a slot whereas the System don't have a reference of it on the slot.
            if (ContainerGrid.Grid[data.state] != null)
            {
                Debug.LogError($"Grid is not free at {data.state}. Occupied by {ContainerGrid.Grid[data.state].GetComponent<UIInventoryItem>().itemData.coreKey}." +
                    $" An error probably occured in Inventory System because the position was found as free in the datas. " +
                    $" Now finding another free index as fail safe..");

                data.state = ContainerGrid.GetFreeSlotIndex();
            }

            if (data.state == -1)
            {
                Debug.LogError(data.coreKey + " position is -1, meaning that no free position was found.. Do a redeem window like mailbox to handle this case.");
            }
            else
            {
                ContainerGrid.PopulateObjectOnSlot(data.state, item.GetComponent<DraggableComponent>());
            }
        }
        
        private void InventorySystem_OnUpdatedItem(IInventorySystemOwner owner, ItemData data)
        {
            if (this.owner != owner)
                return;

            UIInventoryItem item = null;
            if (UIInventory.TryGetValue(data.uniqueKey, out item))
            {
                if (item.itemData.value <= 0 || item.itemData.state == -1000)
                {
                    Debug.LogError($"Item {data.coreKey} removed from Inventory.");
                    PoolManager.Instance.DespawnGo(item.gameObject);
                }
                else
                {
                    Debug.Log($"Item {data.coreKey} updated by Inventory.");
                    item.UpdateAmountText(item.itemData.value);
                }
            }
        }

        protected override void DragAndDropLayout_OnObjectAdded(DraggableComponent added, int slotPosition)
        {
            UIInventoryItem item = added.gameObject.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                Debug.LogError("Inventory Add " + item.itemData.coreKey);

                item.itemData.state = added.PositionInlayout;
                item.itemData.container = (int)this.ItemContainerType;
                //inventorySystem.UpdateInventory(item.itemData, false);

                InventoryControllerEventHandler.AddedItem(this, item.itemData);

                //Auto commit is on
                //InventoryControllerHandler.SaveInventory(this);
            }
        }

        protected override void DragAndDropLayout_OnObjectRemoved(DraggableComponent removed, int slotPosition)
        {
            UIInventoryItem item = removed.gameObject.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                Debug.LogError("Inventory Remove " + item.itemData.coreKey);
                //inventorySystem.UpdateInventory(item.itemData, false);
                InventoryControllerEventHandler.RemovedItem(this, item.itemData);
            }
        }

        protected override void DragAndDropLayout_OnStartedDrag(DraggableComponent obj)
        {
            if (obj != null)
                AudioManager.Instance.PlayOneShot(DragClip.audioClip, DragClip.clipVolume);
        }

        protected override void DragAndDropLayout_OnEndDrop(DraggableComponent obj, int slotPosition)
        {
            //inventorySystem.CommitInventory();
            AudioManager.Instance.PlayOneShot(DropClip.audioClip, DropClip.clipVolume);
        }

        protected override void DragAndDropLayout_OnOverlap(DraggableComponent overlaping, DraggableComponent overlaped, int slotPosition)
        {
            // Swapping the overlaped to overlaping previous position wich is represented by HoveringPosition
            // inventoryGrid.AddObjectToSlot(overlaping.HoveredPosition, overlaped);
        }

        protected override void DragAndDropLayout_OnDropConflict(DraggableComponent dropping, DraggableComponent conflicted, int slotPosition)
        {
            if (ContainerGrid.Grid.ContainsKey(dropping.HoveredPosition))
            {
                // On replace l'objet en conflit sur la case previous 
                if (ContainerGrid.Grid[dropping.HoveredPosition] == null
                   || ContainerGrid.Grid[dropping.HoveredPosition] == dropping)
                {
                    ContainerGrid.AddObjectToSlot(dropping.HoveredPosition, conflicted);
                }
                else if (ContainerGrid.Grid[dropping.InitialPosition] == null
                   || ContainerGrid.Grid[dropping.InitialPosition] == dropping)
                {
                    ContainerGrid.AddObjectToSlot(dropping.InitialPosition, conflicted);
                }
                else
                {
                    int freeSlot = ContainerGrid.ClosestFreeSlotIndex(dropping.transform.position);
                    if (ContainerGrid.Grid.ContainsKey(freeSlot))
                    {
                        ContainerGrid.AddObjectToSlot(freeSlot, conflicted);
                    }
                    else
                    {
                        Debug.LogError("Error with drop conflict : No free slot avalaible to swap conflicted element.");
                    }
                }
            }
        }

    }
}
