using Assets.BattleGame.Scripts.Managers;
using Core;
using SteamAndMagic.Entities;
using SteamAndMagic.Systems.Inventory;
using SteamAndMagic.Systems.Wallet;
using TMPro;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class CharacterWindow : UIBlock
    {
        public TextMeshProUGUI CharacterName_text;
        public TextMeshProUGUI CharacterPower_text;

        public TextMeshProUGUI CaldianCrown_text;
        public TextMeshProUGUI Gold_text;

        public AbilitiesController abilitiesController;
        public EquipmentController equipmentController;
        public InventoryController inventoryController;
        public StatsBlock statsWindow;

        public CanvasGroup RightPanel;
        public CanvasGroup MiddlePanel;
        public CanvasGroup LeftPanel;

        public GameObject MiddlePanel_AbilityCollection;
        public GameObject MiddlePanel_CharacterOtherDetails;

        private Character owner;

        //public CharacterDetailsPanel characterDetailsPanel;
        //public WalletPanel walletPanel;

        private void OnEnable()
        {
            WalletSystemEventHandler.OnUpdatedWalletData += WalletSystemEventHandler_OnUpdatedWalletData;
            UIAbilityEventHandler.OnUIAbilitySelected += UIAbilityEventHandler_OnUIAbilitySelected;
        }

        private void OnDisable()
        {
            WalletSystemEventHandler.OnUpdatedWalletData -= WalletSystemEventHandler_OnUpdatedWalletData;
            UIAbilityEventHandler.OnUIAbilitySelected -= UIAbilityEventHandler_OnUIAbilitySelected;
        }

        private void UIAbilityEventHandler_OnUIAbilitySelected(UIAbility uIAbility, bool state)
        {
            if (state)
            {
                LeftPanel.alpha = 1;
            }
            else
            {
                LeftPanel.alpha = 0;
            }
        }

        public void Init(Character owner)
        {
            this.owner = owner;

            CharacterName_text.text = owner.characterData.name;

            GameMenuControllerEventHandler.GetGameMenuControllerRequest((gameMenuController) =>
            {
                inventoryController = gameMenuController.inventoryController;
            });

            equipmentController.Init(owner);
            statsWindow.Init(owner);
        }

        protected override void OnShow()
        {
            base.OnShow();

            GameMenuControllerEventHandler.GetGameMenuControllerRequest((gameMenuController) =>
            {
                gameMenuController.inventoryController.gameObject.SetActive(true);
            });

            UpdateWalletTexts();
            abilitiesController.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
            ItemDetailDynamicPanelEventHandler.DisplayDetailPanelRequest(null, false);

            GameMenuControllerEventHandler.GetGameMenuControllerRequest((gameMenuController) =>
            {
                gameMenuController.inventoryController.gameObject.SetActive(false);
            });
            abilitiesController.OnHide();
        }

        private void UpdateWalletTexts()
        {
            if (GameManager.Instance.LocalCharacter == null)
                return;

            CaldianCrown_text.text = GameManager.Instance.LocalCharacter.walletSystem.GetAmountForCoin(CoinType.CaldianCrows).ToString();
            Gold_text.text = GameManager.Instance.LocalCharacter.walletSystem.GetAmountForCoin(CoinType.Gold).ToString();
        }

        private void WalletSystemEventHandler_OnUpdatedWalletData(IWalletSystemOwner owner, Backend.WalletData updated)
        {
            if(owner == this.owner as IWalletSystemOwner)
            {
                CaldianCrown_text.text = GameManager.Instance.LocalCharacter.walletSystem.GetAmountForCoin(CoinType.CaldianCrows).ToString();
                Gold_text.text = GameManager.Instance.LocalCharacter.walletSystem.GetAmountForCoin(CoinType.Gold).ToString();
            }           
        }

        // UI Method
        public void ToggleEquipmentContent()
        {
            equipmentController.gameObject.SetActive(!equipmentController.gameObject.activeSelf);
        }

        public void ShowCharacterOtherDetails(bool state)
        {
            MiddlePanel_CharacterOtherDetails.SetActive(state);
            MiddlePanel_AbilityCollection.SetActive(!state);
        }

        private void Update()
        {
            if(owner != null)
            {
                CharacterPower_text.text = owner.characterData.power.ToString();
            }
        }
    }
}