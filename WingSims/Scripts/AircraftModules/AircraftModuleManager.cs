using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.AircraftModules
{
    public class AircraftModuleManager : MonoBehaviour
    {
        public void AddModuleToAircraft(AircraftModule module, Aircraft aircraft)
        {
            GameObject go = Instantiate(module.gameObject, aircraft.transform);
            go.name = module.GetType().ToString();
        }
    }
}
