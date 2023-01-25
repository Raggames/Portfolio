using Incursion.Backend;
using Sirenix.OdinInspector;
using SteamAndMagic.Systems.Inventory;
using SteamAndMagic.Systems.Items;
using UnityEngine;

public class DebugInventory : MonoBehaviour
{
    public InventorySystem system;

    private void Awake()
    {
        system = FindObjectOfType<InventorySystem>();
    }

    [Button("Add Item")]
    public void AddItemTest(ItemSetting itemSetting, int amount = 1)
    {
        system.AddItemBySetting(itemSetting, amount);
    }

    [Button("Commit")]
    public void Commit()
    {
        BackendManager.Instance.ItemData_Update(null, true);
    }
}

