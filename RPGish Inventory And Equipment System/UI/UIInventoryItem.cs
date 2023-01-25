using Coffee.UIEffects;
using Core;
using Incursion.Backend;
using IncursionDAL;
using SteamAndMagic.Audio;
using SteamAndMagic.Backend;
using SteamAndMagic.Interface.DragNDrop;
using SteamAndMagic.Systems.Economics;
using SteamAndMagic.Systems.Inventory;
using SteamAndMagic.Systems.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SteamAndMagic.Interface
{
    public enum UIItemInteractionMode
    {
        None,
        CharacterWindow,
        Craft,
        ShopSell,
        ShopBuy
    }

    public enum UIInventoryItemContext
    {
        Inventory,
        Equipment,
        Craft,
        Lootbag
    }

    public class UIInventoryItemEventHandler
    {
        public delegate void UIInventoryItemSelectedHandler(UIInventoryItem uIInventoryItem, bool state);
        public static event UIInventoryItemSelectedHandler OnUiInventoryItemSelected;
        public static void UIInventoryItemSelected(UIInventoryItem uIInventoryItem, bool state) => OnUiInventoryItemSelected?.Invoke(uIInventoryItem, state);

        public delegate void UIInventoryItemClickedHandler(UIInventoryItem uIInventoryItem);
        public static event UIInventoryItemClickedHandler OnUiInventoryItemClicked;
        public static void UIInventoryItemClicked(UIInventoryItem uIInventoryItem) => OnUiInventoryItemClicked?.Invoke(uIInventoryItem);
    }

    public class UIInventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDropHandler
    {
        [Header(" ---- MEMBERS ---- ")]

        // Members
        public Image image;
        public TextMeshProUGUI Amount_Text;
        public GameObject SelectedImage;
        public UIEffect UIEffect;

        public GameObject ShopModeContent;
        public TextMeshProUGUI SellPrice;
        public Color basePriceTextColor;

        [Header("---- RUNTIME ----")]

        public DraggableComponent draggable;
        public RectTransform rect;
        public ItemContainerType container
        {
            get
            {
                if (shopItemData != null && shopItemData.item_data != null)
                {
                    return ItemContainerType.Shop;
                }
                else if (itemData != null && itemData.coreKey != string.Empty) 
                {
                    return (ItemContainerType)itemData.container;
                }
                else if(coinData != null && coinData.Amount > 0)
                {
                    return ItemContainerType.Wallet;
                }                 

                Debug.LogError("No container type initialized ?");
                return ItemContainerType.InventoryMain;
            }
        } 
        
        private UIItemInteractionMode currentInteractability;
        public UIItemInteractionMode CurrentInteractability
        {
            get
            {
                return currentInteractability;
            }
            set
            {
                UIEffect.enabled = false;

                currentInteractability = value;

                switch (currentInteractability)
                {
                    case UIItemInteractionMode.None:
                        draggable.IsInteractable = false;

                        break;
                    case UIItemInteractionMode.CharacterWindow:
                        draggable.IsInteractable = true;

                        break;
                    case UIItemInteractionMode.Craft:
                        if (itemSetting.IsResource)
                        {
                            draggable.IsInteractable = true;
                        }
                        else
                        {
                            draggable.IsInteractable = false;
                            UIEffect.enabled = true;
                        }
                        break;
                    case UIItemInteractionMode.ShopSell:
                        draggable.IsInteractable = false;
                        this.ShopModeContent.SetActive(true);
                        this.SellPrice.text = itemSetting.sellValue.ToString();

                        break;
                }
            }
        }

        private bool isSelected = false;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                SelectedImage.SetActive(value);

                UIInventoryItemEventHandler.UIInventoryItemSelected(this, isSelected);
            }
        }

        [Header(" ---- DATAS ---- ")]

        public ItemData itemData;
        public ItemSetting itemSetting;
        [Space]
        public CoinData coinData;
        public CoinSetting coinSetting;
        [Space]
        public ShopItemData shopItemData;
        public ShopSettingItem shopSettingItem;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            basePriceTextColor = SellPrice.color;
        }

        void OnEnable()
        {
            DraggableTrash.OnDraggableComponentTrashed += DraggableTrash_OnDraggableComponentTrashed;
            UIInventoryItemEventHandler.OnUiInventoryItemSelected += UIInventoryItemEventHandler_OnUiInventoryItemSelected;
        }

        void OnDisable()
        {
            DraggableTrash.OnDraggableComponentTrashed -= DraggableTrash_OnDraggableComponentTrashed;
            UIInventoryItemEventHandler.OnUiInventoryItemSelected -= UIInventoryItemEventHandler_OnUiInventoryItemSelected;
        }

        public void Initialize(ItemData itemData)
        {
            this.itemData = itemData;
            this.itemSetting = CoreManager.Instance.GetItemByKey(itemData.coreKey);
            this.image.sprite = itemSetting.icon;
            //this.draggable = GetComponent<DraggableComponent>();
            this.ShopModeContent.SetActive(false);

            this.name = "UIItem_" + itemData.coreKey + "_" + itemData.uniqueKey;

            UpdateAmountText(itemData.value);
        }

        public void Initialize(CoinData coinData)
        {
            this.coinData = coinData;
            this.coinSetting = CoreManager.Instance.GetCoinByType((CoinType)this.coinData.CoinType);
            this.image.sprite = coinSetting.icon;
            //this.draggable = GetComponent<DraggableComponent>();

            this.name = "UICoin_" + coinSetting.name;
            this.ShopModeContent.SetActive(false);

            UpdateAmountText(this.coinData.Amount);
        }

        public void Initialize(ShopItemData shopItemData)
        {
            this.shopItemData = shopItemData;
            this.itemData = shopItemData.item_data;

            if (shopItemData.sold_by_player == 0)
            {
                this.shopSettingItem = CoreManager.Instance.GetShopSettingByUniqueID(shopItemData.shop_setting_unique_id).GetShopItemByItemSettingCorekey(shopItemData.item_data.coreKey);
                this.itemSetting = shopSettingItem.ItemSetting;
                this.SellPrice.text = shopSettingItem.Cost.ToString();
            }
            else
            {
                this.itemSetting = CoreManager.Instance.GetItemByKey(itemData.coreKey);
                this.SellPrice.text = itemSetting.sellValue.ToString();
            }

            this.image.sprite = itemSetting.icon;
            //this.draggable = GetComponent<DraggableComponent>();

            this.name = "UIItem_" + shopItemData.item_data.coreKey;
            this.ShopModeContent.SetActive(true);

            UpdateAmountText(shopItemData.item_data.value);
        }

        public void UpdateAmountText(int amount)
        {
            this.Amount_Text.text = amount.ToString();
            if (amount > 1)
            {
                Amount_Text.enabled = true;
            }
            else Amount_Text.enabled = false;
        }

        public void CraftOutputToInventory(GameMenuController gameMenuController)
        {
            int freeSlotIndex = gameMenuController.characterWindow.inventoryController.ContainerGrid.GetFreeSlotIndex();
            if (freeSlotIndex == -1)
            {
                AudioManager.Instance.Play_UINotAllowed();
            }
            else
            {
                gameMenuController.craftingWindow.craftingController.ClearOutput();
                //gameMenuController.characterWindow.equipmentController.ContainerGrid.RemoveObjectFromSlot(draggable);
                gameMenuController.inventoryController.ContainerGrid.HandleDrop(this.GetComponent<DraggableComponent>(), freeSlotIndex);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Display details
            ItemDetailDynamicPanelEventHandler.DisplayDetailPanelRequest(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // End display details
            ItemDetailDynamicPanelEventHandler.DisplayDetailPanelRequest(this, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (CurrentInteractability)
            {
                case UIItemInteractionMode.None: return;

                case UIItemInteractionMode.CharacterWindow:
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        if (this.itemSetting.stackable)
                        {
                            Debug.LogError("Start split");

                            SplitStack();
                        }
                        else
                        {
                            if (itemSetting.IsEquipment)
                            {
                                if (container == ItemContainerType.InventoryMain)
                                {
                                    // go equipement
                                    StuffSetting stuff = (StuffSetting)itemSetting;

                                    GameMenuController.Instance.inventoryController.ContainerGrid.RemoveObjectFromSlot(draggable);

                                    bool dropped = GameMenuController.Instance.characterWindow.equipmentController.ContainerGrid.HandleDrop(draggable, (int)stuff.StuffPartType);
                                    if (!dropped)
                                    {
                                        Debug.LogError("Couldn't auto drop to equipment, return to inventory previous position.");
                                        AudioManager.Instance.Play_UINotAllowed();
                                        GameMenuController.Instance.inventoryController.ContainerGrid.AddObjectToSlot(draggable.InitialPosition, draggable);
                                    }
                                }
                                else if (container == ItemContainerType.Equipment)
                                {
                                    // go inventory
                                    int freeSlotIndex = GameMenuController.Instance.characterWindow.inventoryController.ContainerGrid.GetFreeSlotIndex();
                                    if (freeSlotIndex == -1)
                                    {
                                        AudioManager.Instance.Play_UINotAllowed();
                                    }
                                    else
                                    {
                                        GameMenuController.Instance.characterWindow.equipmentController.ContainerGrid.RemoveObjectFromSlot(draggable);
                                        bool dropped = GameMenuController.Instance.inventoryController.ContainerGrid.HandleDrop(this.GetComponent<DraggableComponent>(), freeSlotIndex);
                                        if (!dropped)
                                        {
                                            Debug.LogError("Problem with auto drop to inventory. Position should be free but drop couldn't be done.");
                                        }
                                    }
                                }
                            }
                            else if (itemSetting.IsRune)
                            {
                                // Open the rune application panel on left click
                                RuneApplicationControllerEventHandler.ClickedRuneSettingRequest(this.itemData, this.itemSetting as RuneSetting);
                            }

                        }
                    }
                    else if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        if (this.itemSetting is StuffSetting)
                        {
                            //Debug.LogError("Selection/Deselection de l'objet");
                            IsSelected = !IsSelected;
                        }
                    }
                    break;
                case UIItemInteractionMode.Craft:
                    if (container == ItemContainerType.CraftOutput)
                    {
                        CraftOutputToInventory(GameMenuController.Instance);
                    }
                    break;
                case UIItemInteractionMode.ShopSell:
                case UIItemInteractionMode.ShopBuy:
                    UIInventoryItemEventHandler.UIInventoryItemClicked(this);
                    break;
            }
        }

        public void OnDoubleClick()
        {
            GameMenuController gameMenuController = null;
            GameMenuControllerEventHandler.GetGameMenuControllerRequest((gmController) => gameMenuController = gmController);

            if (gameMenuController == null)
            {
                Debug.LogError("No game menu controller avalaible");
                return;
            }

            switch (CurrentInteractability)
            {
                case UIItemInteractionMode.None: return;

                case UIItemInteractionMode.CharacterWindow:

                    
                    break;
                case UIItemInteractionMode.Craft:
                   
                    break;
                case UIItemInteractionMode.ShopSell:
                    break;
            }
        }

        public void SplitStack()
        {

        }

        public void Stack(UIInventoryItem toStackWith)
        {
            PoolManager.Instance.DespawnGo(toStackWith.gameObject);
            this.itemData.value += toStackWith.itemData.value;
            UpdateAmountText(this.itemData.value);
            BackendManager.Instance.ItemData_Delete(toStackWith.itemData);
            BackendManager.Instance.ItemData_Update(this.itemData);
        }

        // Si on drop un item sur un autre, le raycast ne passe plus au travers pour toucher la grille, l'item présent notifie donc à sa grille de faire quelque chose..
        public void OnDrop(PointerEventData eventData)
        {
            UIInventoryItem item = eventData.pointerDrag.GetComponent<UIInventoryItem>();
            if (item != null)
            {
                if (item.itemSetting != null && item.itemSetting.stackable && item.itemSetting == this.itemSetting)
                {
                    Stack(item);
                    return;
                }
            }

            DraggableComponent dropDragComponent = eventData.pointerDrag.GetComponent<DraggableComponent>();
            if (dropDragComponent == null)
                return;

            if (draggable.Context != null)
            {
                draggable.Context.OnDraggableDrop(dropDragComponent);
            }
        }

        private void DraggableTrash_OnDraggableComponentTrashed(DraggableComponent obj)
        {
            if (obj == this.draggable)
            {
                PoolManager.Instance.DespawnGo(this.gameObject);
            }
        }

        private void UIInventoryItemEventHandler_OnUiInventoryItemSelected(UIInventoryItem uIInventoryItem, bool state)
        {
            // Selection unique des items, on deselectionne donc tous les autres en selectionnant un nouvel item
            if (state && isSelected && uIInventoryItem != this)
            {
                isSelected = false;
                SelectedImage.SetActive(false);
            }
        }

    }
}
