using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.AircraftModules
{
    public static class HeightDetectorEventHandler
    {
        public static event Action<HeightDetector, float, bool> OnHeighDetect;
        public static void OnHeighDetectRequest(HeightDetector hd, float heigh, bool isProximityFlying) => OnHeighDetect?.Invoke(hd, heigh, isProximityFlying);

        public static event Action<Aircraft, Action<float, float>> GetHeighInfo;
        public static void GetHeighInfoRequest(Aircraft aircraft, Action<float, float> result) => GetHeighInfo?.Invoke(aircraft, result);
    }

    public class HeightDetector : AircraftModule
    {
        public int ProximityHeight = 50;
        public float GroundAlt;

        private void OnEnable()
        {
            HeightDetectorEventHandler.GetHeighInfo += HeightDetectorEventHandler_GetHeighInfo;
        }

        private void OnDisable()
        {
            HeightDetectorEventHandler.GetHeighInfo -= HeightDetectorEventHandler_GetHeighInfo;
        }

        private void HeightDetectorEventHandler_GetHeighInfo(Aircraft arg1, Action<float, float> arg2)
        {
            if(aircraft == arg1)
            {
                arg2?.Invoke(GroundAlt, ProximityHeight);
            }
        }

        private void Update()
        {
            GroundAlt = GetGroundAltitude();

            if (GroundAlt <= ProximityHeight)
            {
                HeightDetectorEventHandler.OnHeighDetectRequest(this, GroundAlt, true);
            }
            else
            {
                HeightDetectorEventHandler.OnHeighDetectRequest(this, GroundAlt, true);
            }
        }

        public float GetGroundAltitude()
        {
            int mask = LayerMask.NameToLayer("World");
            mask |= mask;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.down, Vector3.down, out hit, 100000))
            {
                return Vector3.Distance(hit.point, transform.position);
            }
            return 0;
        }
    }
}
