using SteamAndMagic.Interface;
using UnityEngine;

namespace SteamAndMagic.Systems.Inventory
{
    public class ItemDetailHandler : MonoBehaviour
    {
        public ItemDetailDynamicPanel ItemDetailDynamicPanel;

        private void Awake()
        {
            ItemDetailDynamicPanel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ItemDetailDynamicPanelEventHandler.OnDisplayDetailPanel += ItemDetailDynamicPanelEventHandler_OnDisplayDetailPanel;
            GameMenuControllerEventHandler.OnGameMenuToggled += GameMenuControllerEventHandler_OnGameMenuToggled;
        }

        private void OnDisable()
        {
            ItemDetailDynamicPanelEventHandler.OnDisplayDetailPanel -= ItemDetailDynamicPanelEventHandler_OnDisplayDetailPanel;
            GameMenuControllerEventHandler.OnGameMenuToggled -= GameMenuControllerEventHandler_OnGameMenuToggled;
        }

        public void ItemDetailDynamicPanelEventHandler_OnDisplayDetailPanel(UIInventoryItem uiItem, bool arg2)
        {
            if (uiItem != null)
                ItemDetailDynamicPanel.Display(uiItem, arg2);

            ItemDetailDynamicPanel.gameObject.SetActive(arg2);
        }


        private void GameMenuControllerEventHandler_OnGameMenuToggled(bool obj)
        {
            if (!obj)
            {
                ItemDetailDynamicPanel.gameObject.SetActive(false);
            }
        }

    }
}
