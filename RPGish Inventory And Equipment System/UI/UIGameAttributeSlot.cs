using SteamAndMagic.Systems.Attributes;
using SteamAndMagic.Systems.LocalizationManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SteamAndMagic.Interface
{
    public class UIGameAttributeSlot : MonoBehaviour
    {
        public GameAttribute GameAttribute;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Current;
        public TextMeshProUGUI Base;

        public void Initialize(GameAttribute attribute)
        {
            GameAttribute = attribute;
            Name.text = LocalizationManager.GetLocalizedValue(attribute.stat.ToString(), LocalizationFamily.Gameplay);
            attribute.AddCurrentListenner(UpdateCurrentValue);
            attribute.AddBaseListenner(UpdateBaseValue);

            UpdateCurrentValue(attribute.Current);
            UpdateBaseValue(attribute.Base);
        }

        private void UpdateCurrentValue(float value)
        {
            switch (GameAttribute.AttributeType)
            {
                case AttributeType.Flat:
                    Current.text = GameAttribute.BonusOnly.ToString();
                    break;
                case AttributeType.Gauge:
                    Current.text = value.ToString();
                    break;
            }
        }

        private void UpdateBaseValue(float value)
        {
            Base.text = value.ToString();
        }
    }
}
