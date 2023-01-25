using Assets.SteamAndMagic.Scripts.Managers;
using IncursionDAL;
using SteamAndMagic.Audio;
using SteamAndMagic.Entities;
using SteamAndMagic.Interface.DragNDrop;
using SteamAndMagic.Systems.Inventory;
using SteamAndMagic.Systems.Items;
using SteamAndMagic.Systems.LocalizationManagement;
using TMPro;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class EquipmentController : UIItemContainer
    {
        public DragAndDropGrid inventoryGrid;

        [Header("Sounds")]
        public AudioClipInfo DropClip;
        public AudioClipInfo DragClip;
        public AudioClipInfo NotAllowedClip;

        [Header("Info Texts")]
        public TextMeshProUGUI Armor_Heavy_Weights_text;
        public TextMeshProUGUI Armor_Utilitary_Weights_text;
        public TextMeshProUGUI Armor_Light_Weights_text;

        public IEquipmentSystemOwner owner { get; private set; }

        public void Init(IEquipmentSystemOwner owner)
        {
            this.owner = owner;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            DraggableComponent.OnDraggableStartDrag += DraggableComponent_OnDraggableStartDrag;
            DraggableComponent.OnDraggableEndDrag += DraggableComponent_OnDraggableEndDrag;

            this.DisplayEquipmentRequest();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DraggableComponent.OnDraggableStartDrag -= DraggableComponent_OnDraggableStartDrag;
            DraggableComponent.OnDraggableEndDrag -= DraggableComponent_OnDraggableEndDrag;
        }

        protected override void DragAndDropLayout_OnObjectAdded(DraggableComponent added, int slotPosition)
        {
            UIInventoryItem item = added.gameObject.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                UIEquipmentSlot uIEquipmentSlot = ContainerGrid.Slots[slotPosition].GetComponent<UIEquipmentSlot>();

                if (item.itemSetting.IsEquipment)
                {
                    if (uIEquipmentSlot != null)
                    {
                        StuffSetting stuffSetting = item.itemSetting as StuffSetting;
                        if (uIEquipmentSlot.itemSlot == stuffSetting.StuffPartType)
                        {
                            bool canAddItem = true;

                            // Si l'objet qu'on dépose est une arme, il est possible qu'on doive enlever l'autre main
                            WeaponSetting weaponSetting = stuffSetting as WeaponSetting;
                            if (weaponSetting != null)
                            {
                                // AJOUT d'ARME A DEUX MAINS
                                if (weaponSetting.Is2Handed)
                                {
                                    /// ON DOIT ENLEVER LES OFF HANDS 
                                    if (weaponSetting.IsRightHand && ContainerGrid.Grid[(int)StuffPartType.LeftWeapon] != null)
                                    {
                                        Debug.LogError("Remove left weapon before adding right weapon");

                                        int freeSlotIndex = inventoryGrid.GetFreeSlotIndex();
                                        if (freeSlotIndex == -1)
                                        {
                                            canAddItem = false;
                                        }
                                        else
                                        {
                                            DraggableComponent toRemove = ContainerGrid.Grid[(int)StuffPartType.LeftWeapon];
                                            ContainerGrid.RemoveObjectFromSlot(toRemove);
                                            inventoryGrid.HandleDrop(toRemove, freeSlotIndex);
                                        }
                                    }
                                    else if (weaponSetting.IsLeftHand && ContainerGrid.Grid[(int)StuffPartType.RightWeapon] != null)
                                    {
                                        Debug.LogError("Remove right weapon before adding left weapon");

                                        int freeSlotIndex = inventoryGrid.GetFreeSlotIndex();
                                        if (freeSlotIndex == -1)
                                        {
                                            canAddItem = false;
                                        }
                                        else
                                        {
                                            DraggableComponent toRemove = ContainerGrid.Grid[(int)StuffPartType.RightWeapon];
                                            ContainerGrid.RemoveObjectFromSlot(toRemove);
                                            inventoryGrid.HandleDrop(toRemove, freeSlotIndex);
                                        }
                                    }
                                }
                                // AJOUT d'ARME A UNE MAIN
                                else
                                {
                                    Character character = owner as Character;
                                    if (weaponSetting.IsLeftHand && character.equipmentSystem.RightWeapon != null && character.equipmentSystem.RightWeapon.Is2Handed)
                                    {
                                        Debug.LogError("Remove right 2Handed weapon before adding left weapon");

                                        int freeSlotIndex = inventoryGrid.GetFreeSlotIndex();
                                        if (freeSlotIndex == -1)
                                        {
                                            canAddItem = false;
                                        }
                                        else
                                        {
                                            DraggableComponent toRemove = ContainerGrid.Grid[(int)StuffPartType.RightWeapon];
                                            ContainerGrid.RemoveObjectFromSlot(toRemove);
                                            inventoryGrid.HandleDrop(toRemove, freeSlotIndex);
                                        }
                                    }
                                    else if (weaponSetting.IsRightHand && character.equipmentSystem.LeftWeapon != null && character.equipmentSystem.LeftWeapon.Is2Handed)
                                    {
                                        Debug.LogError("Remove left 2Handed weapon before adding right weapon");

                                        int freeSlotIndex = inventoryGrid.GetFreeSlotIndex();
                                        if (freeSlotIndex == -1)
                                        {
                                            canAddItem = false;
                                        }
                                        else
                                        {
                                            DraggableComponent toRemove = ContainerGrid.Grid[(int)StuffPartType.LeftWeapon];
                                            ContainerGrid.RemoveObjectFromSlot(toRemove);
                                            inventoryGrid.HandleDrop(toRemove, freeSlotIndex);
                                        }
                                    }
                                }
                            }

                            if (canAddItem)
                            {
                                Debug.LogError("Equipment Add " + item.itemData.coreKey);

                                item.itemData.state = (int)uIEquipmentSlot.itemSlot;
                                item.itemData.container = (int)ItemContainerType.Equipment;

                                EquipmentControllerHandler.AddEquipmentEvent(this, item.itemData);
                                //equipmentSystem.UpdateEquipment(item.itemData, false);
                                return;
                            }
                        }
                        else
                            Debug.LogError(item + " doesn't fit the slot of type " + uIEquipmentSlot.itemSlot);
                    }
                    else Debug.LogError("UIEquipmentSlot component is missing.");
                }

                Debug.LogError($"Couldn't drop {item.itemData.coreKey} here, return to inventory");

                ContainerGrid.RemoveObjectFromSlot(added);
                inventoryGrid.HandleDrop(added, inventoryGrid.GetFreeSlotIndex());

                AudioManager.Instance.PlayOneShot(NotAllowedClip.audioClip, NotAllowedClip.clipVolume);

                //Auto commit is on
                //EquipmentControllerHandler.SaveEquipmentRequest(this);
            }
            else
            {
                Debug.Log("Dropped object null.");
            }
        }

        protected override void DragAndDropLayout_OnObjectRemoved(DraggableComponent removed, int slotPosition)
        {
            // En théorie le remove d'un objet dans l'equipment sera forcement repercuté par un ajout dans l'inventaire
            // On garde la double securité pour l'instant

            UIInventoryItem item = removed.gameObject.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                Debug.LogError("Equipment Remove " + item.itemData.coreKey);

                //item.itemData.state = removed.PositionInlayout; // placé en -1000         
                //equipmentSystem.UpdateEquipment(item.itemData, false);
                EquipmentControllerHandler.RemoveEquipmentEvent(this, item.itemData);
            }
        }

        protected override void DragAndDropLayout_OnEndDrop(DraggableComponent obj, int slotPosition)
        {
            AudioManager.Instance.PlayOneShot(DropClip.audioClip, DropClip.clipVolume);
        }

        protected override void DragAndDropLayout_OnOverlap(DraggableComponent overlaping, DraggableComponent overlaped, int slotPosition)
        {
            // Swapping the overlaped to overlaping previous position wich is represented by HoveringPosition
            //inventoryGrid.AddObjectToSlot(overlaping.HoveredPosition, overlaped);
        }

        protected override void DragAndDropLayout_OnDropConflict(DraggableComponent dropping, DraggableComponent conflicted, int slotPosition)
        {
            int freeSlot = inventoryGrid.GetFreeSlotIndex();
            if (freeSlot == -1)
            {
                Debug.LogError("Inventory full, cannot move conflicted element to inventory.");
                return;
            }

            ContainerGrid.RemoveObjectFromSlot(conflicted);
            inventoryGrid.AddObjectToSlot(freeSlot, conflicted);
        }

        private void DraggableComponent_OnDraggableEndDrag(DraggableComponent inDrag)
        {
            UIInventoryItem uiItem = inDrag.GetComponent<UIInventoryItem>();
            if (uiItem == null)
                return;

            if (uiItem.itemSetting.IsEquipment)
            {
                // Feedback dans l'equipment grid
            }
        }

        private void DraggableComponent_OnDraggableStartDrag(DraggableComponent inDrag)
        {
            UIInventoryItem uiItem = inDrag.GetComponent<UIInventoryItem>();
            if (uiItem == null)
                return;

            AudioManager.Instance.PlayOneShot(DragClip.audioClip, DragClip.clipVolume);

            if (uiItem.itemSetting.IsEquipment)
            {
                /* Debug.LogError("Start drag and remove => " + uiItem.itemData.coreKey);
                 this.RemoveEquipmentEvent(uiItem.itemData);*/
            }
        }

        public UIInventoryItem CreateUIItemFromData(ItemData data)
        {
            UIInventoryItem item =
                PoolManager.Instance.SpawnGo(inventoryItem_Prefab.gameObject, Vector3.zero, inventoryGrid.Grid_Transform)
                .GetComponent<UIInventoryItem>();

            item.Initialize(data);
            item.CurrentInteractability = UIItemInteractionMode.CharacterWindow;
            return item;
        }

        private void Update()
        {
            if(owner != null && owner.equipmentSystem.CurrentEquipmentTypeWeights.Count > 0)
            {
                // A faire mieux 
                // Afficher que les bonus de set qu'on a
                Armor_Heavy_Weights_text.text = LocalizationManager.GetLocalizedValue("AbilityEquipmentType_" + AbilityEquipmentType.Protection.ToString(), LocalizationFamily.Gameplay) + " : " + DeckManager.Instance.GetAbilityEquipementSetWeight(AbilityEquipmentType.Protection) + "/" + owner.equipmentSystem.CurrentEquipmentTypeWeights[AbilityEquipmentType.Protection];
                Armor_Utilitary_Weights_text.text = LocalizationManager.GetLocalizedValue("AbilityEquipmentType_" + AbilityEquipmentType.Engineering.ToString(), LocalizationFamily.Gameplay) + " : " + DeckManager.Instance.GetAbilityEquipementSetWeight(AbilityEquipmentType.Engineering) + "/" + owner.equipmentSystem.CurrentEquipmentTypeWeights[AbilityEquipmentType.Engineering];
                Armor_Light_Weights_text.text = LocalizationManager.GetLocalizedValue("AbilityEquipmentType_" + AbilityEquipmentType.Inquisitor.ToString(), LocalizationFamily.Gameplay) + " : " + DeckManager.Instance.GetAbilityEquipementSetWeight(AbilityEquipmentType.Inquisitor) + "/" + owner.equipmentSystem.CurrentEquipmentTypeWeights[AbilityEquipmentType.Inquisitor];
            }
        }
    }
}
