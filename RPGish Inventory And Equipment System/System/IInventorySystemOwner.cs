namespace SteamAndMagic.Systems.Inventory
{
    public interface IInventorySystemOwner
    {
        public abstract InventorySystem inventorySystem { get; }
    }
}
