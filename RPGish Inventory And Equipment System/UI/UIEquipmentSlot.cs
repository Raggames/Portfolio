using SteamAndMagic.Systems.Items;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class UIEquipmentSlot : MonoBehaviour
    {
        public StuffPartType itemSlot;

        public GameObject LockedImage;
        // Left weapon slot is locked when right weapon slot is fulfilled with 2 HAND weapon
        public bool Locked
        {
            set
            {
                LockedImage.SetActive(value);
            }
        }

        public void HighLight(bool state)
        {

        }

        public void DownLight(bool state)
        {

        }

        private void OnEnable()
        {
            EquipmentControllerHandler.OnAddEquipment += EquipmentControllerHandler_OnAddEquipment;
            EquipmentControllerHandler.OnRemoveEquipment += EquipmentControllerHandler_OnRemoveEquipment;
        }

        private void OnDisable()
        {
            EquipmentControllerHandler.OnAddEquipment -= EquipmentControllerHandler_OnAddEquipment;
            EquipmentControllerHandler.OnRemoveEquipment -= EquipmentControllerHandler_OnRemoveEquipment;
        }

        private void EquipmentControllerHandler_OnRemoveEquipment(EquipmentController handler, IncursionDAL.ItemData itemData)
        {

        }

        private void EquipmentControllerHandler_OnAddEquipment(EquipmentController handler, IncursionDAL.ItemData itemData)
        {
        }

    }
}
