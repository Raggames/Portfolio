using SteamAndMagic.Interface.DragNDrop;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public abstract class UIItemContainer : MonoBehaviour
    {
        public DragAndDropGrid ContainerGrid;
        public UIInventoryItem inventoryItem_Prefab;

        public virtual Dictionary<string, UIInventoryItem> UIInventory
        {
            get
            {
                Dictionary<string, UIInventoryItem> result = new Dictionary<string, UIInventoryItem>();

                for (int i = 0; i < ContainerGrid.Grid.Count; ++i)
                {
                    var pair = ContainerGrid.Grid.ElementAt(i);
                    if (pair.Value != null)
                    {
                        var item = pair.Value.gameObject.GetComponent<UIInventoryItem>();
                        if (item != null)
                        {
                            result.Add(item.itemData.uniqueKey, item);
                        }
                    }
                }

                return result;
            }
        }

        protected virtual void OnEnable()
        {
            ContainerGrid.Init();
            ContainerGrid.OnAdded += DragAndDropLayout_OnObjectAdded;
            ContainerGrid.OnRemoved += DragAndDropLayout_OnObjectRemoved;
            ContainerGrid.OnDropped += DragAndDropLayout_OnEndDrop;
            ContainerGrid.OnOverlap += DragAndDropLayout_OnOverlap;
            ContainerGrid.OnDropConflict += DragAndDropLayout_OnDropConflict;
            ContainerGrid.OnStartedDrag += DragAndDropLayout_OnStartedDrag;
            ContainerGrid.OnEndedDrag += DragAndDropLayout_OnEndedDrag;

            UpdateUIItemsAmount();
        }

        protected virtual void OnDisable()
        {
            ContainerGrid.OnAdded -= DragAndDropLayout_OnObjectAdded;
            ContainerGrid.OnRemoved -= DragAndDropLayout_OnObjectRemoved;
            ContainerGrid.OnDropped -= DragAndDropLayout_OnEndDrop;
            ContainerGrid.OnOverlap -= DragAndDropLayout_OnOverlap;
            ContainerGrid.OnDropConflict -= DragAndDropLayout_OnDropConflict;
            ContainerGrid.OnStartedDrag -= DragAndDropLayout_OnStartedDrag;
        }

        public virtual void UpdateUIItemsAmount()
        {
            var dict = UIInventory;
            for (int i = 0; i < dict.Count; ++i)
            {
                var pair = dict.ElementAt(i);

                if (pair.Value != null)
                {
                    pair.Value.UpdateAmountText(pair.Value.itemData.value);
                }
            }
        }

        protected virtual void DragAndDropLayout_OnStartedDrag(DraggableComponent obj)
        {
        }

        protected virtual void DragAndDropLayout_OnEndedDrag(DraggableComponent obj)
        {

        }

        protected virtual void DragAndDropLayout_OnEndDrop(DraggableComponent obj, int slotPosition)
        {
        }

        protected virtual void DragAndDropLayout_OnObjectAdded(DraggableComponent added, int slotPosition)
        {
        }

        protected virtual void DragAndDropLayout_OnObjectRemoved(DraggableComponent removed, int slotPosition)
        {
        }

        protected virtual void DragAndDropLayout_OnOverlap(DraggableComponent overlaping, DraggableComponent overlaped, int slotPosition)
        {
        }

        protected virtual void DragAndDropLayout_OnDropConflict(DraggableComponent dropping, DraggableComponent conflicted, int slotPosition)
        {
        }
    }
}
