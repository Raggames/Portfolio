using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WingsSim.Control;

namespace WingsSim.Network
{
    public class NetworkRoom : MonoBehaviour
    {
        private static List<AircraftController> roomAircrafts = new List<AircraftController>();
        public static List<AircraftController> RoomAircrafts => roomAircrafts;

        private void OnEnable()
        {
            NetworkPlayerEventHandler.OnPlayerAircraftSpawned += NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
            NetworkPlayerEventHandler.OnPlayerAircraftDestroyed += NetworkPlayerEventHandler_OnPlayerAircraftDestroyed;
        }

        private void OnDisable()
        {
            NetworkPlayerEventHandler.OnPlayerAircraftSpawned -= NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
            NetworkPlayerEventHandler.OnPlayerAircraftDestroyed -= NetworkPlayerEventHandler_OnPlayerAircraftDestroyed;
        }

        private void NetworkPlayerEventHandler_OnPlayerAircraftSpawned(AircraftController aircraftController)
        {
            roomAircrafts.Add(aircraftController);
        }

        private void NetworkPlayerEventHandler_OnPlayerAircraftDestroyed(AircraftController aircraftController)
        {
            roomAircrafts.Remove(aircraftController);
        }
    }
}
