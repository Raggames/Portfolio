using SteamAndMagic.Entities;
using SteamAndMagic.Interface;
using SteamAndMagic.Systems.Inventory;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.Economics
{
    public class LootbagPanel : MonoBehaviour
    {
        public UIInventoryItem InventoryItem_Prefab;
        public GameObject LootbackPanel;
        public Transform LootbackPanelContent;

        public List<UIInventoryItem> CurrentItems = new List<UIInventoryItem>();

        private void Awake()
        {
            LootbackPanel.SetActive(false);
        }

        private void OnEnable()
        {
            LootBagEventHandler.OnLootbagInteractabilityChange += LootBagEventHandler_OnLootbagInteractabilityChange;
        }

        private void OnDisable()
        {
            LootBagEventHandler.OnLootbagInteractabilityChange -= LootBagEventHandler_OnLootbagInteractabilityChange;
        }

        private void LootBagEventHandler_OnLootbagInteractabilityChange(LootBag lootBag, bool state)
        {
            LootbackPanel.SetActive(state);

            if (!state)
            {
                ItemDetailDynamicPanelEventHandler.DisplayDetailPanelRequest(null, false);
            }

            if (state)
            {
                for (int i = 0; i < CurrentItems.Count; ++i)
                {
                    PoolManager.Instance.DespawnGo(CurrentItems[i].gameObject);
                }

                CurrentItems.Clear();
                
                for (int i = 0; i < lootBag.LootsInBag.Count; ++i)
                {
                    CurrentItems.Add(CreateUIInventoryItemFromLoot(lootBag.LootsInBag[i]));
                }
            }            
        }

        public UIInventoryItem CreateUIInventoryItemFromLoot(Loot loot)
        {
            UIInventoryItem item =
               PoolManager.Instance.SpawnGo(InventoryItem_Prefab.gameObject, Vector3.zero, LootbackPanelContent)
               .GetComponent<UIInventoryItem>();

            if (loot.ItemData != null)
            {
                item.Initialize(loot.ItemData);
            }
            else if (loot.CoinData != null)
            {
                item.Initialize(loot.CoinData);
            }

            item.CurrentInteractability = UIItemInteractionMode.None;
            return item;
        }

        public void Close()
        {
            LootbackPanel.SetActive(false);

            ItemDetailDynamicPanelEventHandler.DisplayDetailPanelRequest(null, false);
        }
    }
}
