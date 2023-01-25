using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.AircraftModules
{
    public class AircraftModule : MonoBehaviour
    {
        protected Aircraft aircraft;

        protected virtual void Awake()
        {
            aircraft = GetComponentInParent<Aircraft>();
        }
    }
}
