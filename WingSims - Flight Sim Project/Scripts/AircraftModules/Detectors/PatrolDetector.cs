using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.AircraftModules
{
    public class InPatrolAllyInfo
    {
        public Aircraft aircraft;
        public float duration;
    }

    public class PatrolDetector : MonoBehaviour
    {
        public List<InPatrolAllyInfo> PatrolInfo = new List<InPatrolAllyInfo>();


        private void OnTriggerEnter(Collider other)
        {
            
        }

        private void OnTriggerExit(Collider other)
        {
            
        }

        private void Update()
        {
            // Updating patrol info datas
        }
    }
}
