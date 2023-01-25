using IncursionDAL;

namespace SteamAndMagic.Interface
{
    public static class EquipmentControllerHandler
    {
        public delegate void AddEquipmentHandler(EquipmentController handler, ItemData itemData);
        public static event AddEquipmentHandler OnAddEquipment;
        public static void AddEquipmentEvent(this EquipmentController handler, ItemData data) => OnAddEquipment?.Invoke(handler, data);

        public delegate void RemoveEquipmentHandler(EquipmentController handler, ItemData itemData);
        public static event RemoveEquipmentHandler OnRemoveEquipment;
        public static void RemoveEquipmentEvent(this EquipmentController handler, ItemData data) => OnRemoveEquipment?.Invoke(handler, data);

        public delegate void SaveEquipmentHandler(EquipmentController handler);
        public static event SaveEquipmentHandler OnSaveEquipment;
        public static void SaveEquipmentRequest(this EquipmentController handler) => OnSaveEquipment?.Invoke(handler);

        public delegate void DisplayEquipmentHandler(EquipmentController handler);
        public static event DisplayEquipmentHandler OnDisplayEquipment;
        public static void DisplayEquipmentRequest(this EquipmentController handler) => OnDisplayEquipment?.Invoke(handler);
    }
}
