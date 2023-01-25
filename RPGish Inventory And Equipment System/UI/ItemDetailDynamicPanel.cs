using SteamAndMagic.Interface;
using SteamAndMagic.Systems.Items;
using SteamAndMagic.Systems.LocalizationManagement;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SteamAndMagic.Systems.Inventory
{
    public class ItemDetailDynamicPanel : MonoBehaviour
    {
        [Header("UI Inventory Item")]
        public TextMeshProUGUI ItemName_Text;
        public TextMeshProUGUI ItemDescription_Text;
        public TextMeshProUGUI ItemPower_Text;
        public float offset;
        public Image QualityBackground_Image;
        public Color CommonColor;
        public Color SuperiorColor;
        public Color RareColor;
        public Color ArtefactColor;

        public DynamicTextRow DynamicRow_Prefab;

        public List<DynamicTextRow> Rows = new List<DynamicTextRow>();

        public Transform StatsContent;

        public Transform AbilityDetailContent_Filler;
        public Image AbilityFillerIcon_image;
        public TextMeshProUGUI AbilityFillerTitle_text;
        public TextMeshProUGUI AbilityFillerDescription_text;
        public Transform AbilityDetailContent_Passive;
        public Image AbilityPassiveIcon_image;
        public TextMeshProUGUI AbilityPassiveName_text;
        public TextMeshProUGUI AbilityPassiveDescription_text;

        private RectTransform rect;
        private RectTransform anchorRect;
        private Camera cam;
        private Vector3 min, max;

        private void Start()
        {
            cam = Camera.main;
            rect = GetComponent<RectTransform>();
            min = new Vector3(0, 0, 0);
            max = new Vector3(cam.pixelWidth, cam.pixelHeight, 0);
        }

        public int SetPivot()
        {
            Vector3 targetPosition = Input.mousePosition;

            // Détermine le coin le plus proche
            Vector2 corner = new Vector2(
                ((targetPosition.x > (Screen.width / 2f)) ? 1f : 0f),
                ((targetPosition.y > (Screen.height / 2f)) ? 1f : 0f)
            );

            rect.pivot = corner;

            if (corner.x == 0f && corner.y == 0f)
            {
                //0
                return 2;
            }
            else if (corner.x == 0f && corner.y == 1f)
            {
                //1
                return 3;
            }

            else if (corner.x == 1f && corner.y == 0f)
            {
                //3
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public void Display(UIInventoryItem uiItem, bool arg2)
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            anchorRect = uiItem.rect;

            int corner = SetPivot();
            Vector3[] targetWorldCorners = new Vector3[4];
            anchorRect.GetWorldCorners(targetWorldCorners);

            AbilityDetailContent_Filler.gameObject.SetActive(false);
            AbilityDetailContent_Passive.gameObject.SetActive(false);

            rect.position = targetWorldCorners[corner];
            if (arg2)
            {
                if (uiItem.itemSetting != null)
                {
                    ItemName_Text.text = LocalizationManager.GetLocalizedValue(uiItem.itemSetting.Name, LocalizationFamily.Resources);
                    ItemDescription_Text.text = LocalizationManager.GetLocalizedValue(uiItem.itemSetting.Description, LocalizationFamily.Resources);
                    ItemPower_Text.text = uiItem.itemSetting.Power.ToString();

                    switch (uiItem.itemSetting.quality)
                    {
                        case Items.ItemQuality.Common:
                            ItemName_Text.color = CommonColor;
                            ItemPower_Text.color = CommonColor;
                            break;
                        case Items.ItemQuality.Superior:
                            ItemName_Text.color = SuperiorColor;
                            ItemPower_Text.color = SuperiorColor;
                            break;
                        case Items.ItemQuality.Rare:
                            ItemName_Text.color = RareColor;
                            ItemPower_Text.color = RareColor;
                            break;
                        case Items.ItemQuality.Artefact:
                            ItemName_Text.color = ArtefactColor;
                            ItemPower_Text.color = ArtefactColor;
                            break;
                    }

                    if (uiItem.itemSetting.IsEquipment)
                    {
                        StuffSetting stuffSetting = uiItem.itemSetting as StuffSetting;

                        if(uiItem.CurrentInteractability == UIItemInteractionMode.CharacterWindow || uiItem.CurrentInteractability == UIItemInteractionMode.ShopSell || uiItem.CurrentInteractability == UIItemInteractionMode.Craft)
                        {
                            ShowItemDataStats(uiItem, stuffSetting);
                        }
                        else if(uiItem.CurrentInteractability == UIItemInteractionMode.ShopBuy)
                        {
                            if (uiItem.shopItemData.sold_by_player == 0)
                            {
                                ShowNotGeneratedStats(stuffSetting);
                            }
                            else
                            {
                                ShowItemDataStats(uiItem, stuffSetting);
                            }
                        }

                        if (uiItem.itemSetting.IsWeapon)
                        {
                            WeaponSetting weaponSetting = uiItem.itemSetting as WeaponSetting;

                            if(weaponSetting.weaponAbility != null)
                            {
                                // Filler ********
                                AbilityDetailContent_Filler.gameObject.SetActive(true);
                                AbilityFillerIcon_image.sprite = weaponSetting.weaponAbility.Icon;
                                AbilityFillerTitle_text.text = LocalizationManager.GetLocalizedValue(weaponSetting.weaponAbility.Name, LocalizationFamily.Gameplay);
                                AbilityFillerDescription_text.text = LocalizationManager.GetLocalizedAbilityDescription(weaponSetting.weaponAbility);
                            }

                            // Passif *******
                            if (weaponSetting.weaponPassiveAbility != null)
                            {
                                AbilityDetailContent_Passive.gameObject.SetActive(true);
                                AbilityPassiveIcon_image.sprite = weaponSetting.weaponPassiveAbility.Icon;
                                AbilityPassiveName_text.text = LocalizationManager.GetLocalizedValue(weaponSetting.weaponPassiveAbility.Name, LocalizationFamily.Gameplay);
                                AbilityPassiveDescription_text.text = LocalizationManager.GetLocalizedAbilityDescription(weaponSetting.weaponPassiveAbility);
                            }
                        }
                    }
                }
                else if (uiItem.coinSetting != null)
                {
                    ItemName_Text.text = LocalizationManager.GetLocalizedValue(uiItem.coinSetting.coinType.ToString(), LocalizationFamily.Resources);
                    ItemDescription_Text.text = uiItem.coinData.Amount.ToString();
                }
            }
            else
            {
                for (int i = 0; i < Rows.Count; ++i)
                {
                    Rows[i].Clear();
                    PoolManager.Instance.DespawnGo(Rows[i].gameObject);
                }

                Rows.Clear();
            }
        }

        private void ShowNotGeneratedStats(StuffSetting stuffSetting)
        {
            for (int i = 0; i < stuffSetting.Stats.Length; ++i)
            {
                DynamicTextRow row = PoolManager.Instance.SpawnGo(DynamicRow_Prefab.gameObject, Vector3.zero, StatsContent).GetComponent<DynamicTextRow>();
                Rows.Add(row);
                row.InitRow(LocalizationManager.GetLocalizedValue(stuffSetting.Stats[i].stat.ToString(), LocalizationFamily.Resources), stuffSetting.Stats[i].minValue + " - " + stuffSetting.Stats[i].maxValue);
            }
        }

        private void ShowItemDataStats(UIInventoryItem uiItem, StuffSetting stuffSetting)
        {
            for (int i = 0; i < stuffSetting.Stats.Length; ++i)
            {
                DynamicTextRow row = PoolManager.Instance.SpawnGo(DynamicRow_Prefab.gameObject, Vector3.zero, StatsContent).GetComponent<DynamicTextRow>();
                Rows.Add(row);
                row.InitRow(LocalizationManager.GetLocalizedValue(stuffSetting.Stats[i].stat.ToString(), LocalizationFamily.Resources), uiItem.itemData.stats[i].value.ToString());
            }
        }
    }

    public static class ItemDetailDynamicPanelEventHandler
    {
        public static event Action<UIInventoryItem, bool> OnDisplayDetailPanel;
        public static void DisplayDetailPanelRequest(this UIInventoryItem uIInventoryItem, bool state) => OnDisplayDetailPanel?.Invoke(uIInventoryItem, state);
    }
}
