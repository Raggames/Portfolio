using IncursionDAL;
using SteamAndMagic.Interface;

namespace SteamAndMagic.Systems.Inventory
{
    public static class InventoryControllerEventHandler
    {
        public delegate void AddItemHandler(InventoryController handler, ItemData itemData);
        public static event AddItemHandler OnAddItem;
        public static void AddedItem(this InventoryController handler, ItemData data) => OnAddItem?.Invoke(handler, data);

        public delegate void RemoveItemHandler(InventoryController handler, ItemData itemData);
        public static event RemoveItemHandler OnRemoveItem;
        public static void RemovedItem(this InventoryController handler, ItemData data) => OnRemoveItem?.Invoke(handler, data);

        public delegate void SaveInventoryHandler(InventoryController handler);
        public static event SaveInventoryHandler OnSaveInventory;
        public static void SaveInventory(this InventoryController handler) => OnSaveInventory?.Invoke(handler);

        public delegate void DisplayInventoryHandler(InventoryController handler);
        public static event DisplayInventoryHandler OnDisplayInventory;
        public static void DisplayedInventoryRequest(this InventoryController handler) => OnDisplayInventory?.Invoke(handler);
    }
}
