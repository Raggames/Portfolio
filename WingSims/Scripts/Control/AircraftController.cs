using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.Control
{
    public static class NetworkPlayerEventHandler
    {
        public delegate void OnPlayerAircraftSpawnedHandler(AircraftController aircraftController);
        public static event OnPlayerAircraftSpawnedHandler OnPlayerAircraftSpawned;
        public static void OnPlayerAircraftSpawnedRequest(this AircraftController aircraftController) => OnPlayerAircraftSpawned?.Invoke(aircraftController);

        public delegate void OnPlayerAircraftDestroyedHandler(AircraftController aircraftController);
        public static event OnPlayerAircraftDestroyedHandler OnPlayerAircraftDestroyed;
        public static void OnPlayerAircraftDestroyedRequest(this AircraftController aircraftController) => OnPlayerAircraftDestroyed?.Invoke(aircraftController);

        public delegate void OnPlayerLeaveRoomHandler(AircraftController aircraftController);
        public static event OnPlayerAircraftSpawnedHandler OnPlayerLeaveRoom;
        public static void OnPlayerLeaveRoomRequest(this AircraftController aircraftController) => OnPlayerLeaveRoom?.Invoke(aircraftController);
    }

    public class AircraftController : NetworkBehaviour
    {
        protected Aircraft aircraft;
        public Aircraft Aircraft => aircraft;

        public NetworkPlayerInfo NetworkPlayerInfo;

        public float AileronsSensivity;
        public float ElevatorsSensivity;
        public float DirectionSensivity;
        public float ThrustSensivity;
        public bool RevertElevator = false;


        private void Awake()
        {
            aircraft = GetComponent<Aircraft>();
            NetworkPlayerInfo = new NetworkPlayerInfo();

            RevertElevator = PlayerPrefs.GetInt("PlayerControl.ReverElevator") == 1 ? true : false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Trigger event for the instanciation of the new aircraft
            this.OnPlayerAircraftSpawnedRequest();

            SynchronizeNetworkPlayerInfo("Enzo");

            if (!isLocalPlayer)
            {
                aircraft.enabled = false;
                Destroy(aircraft.rb);
                this.enabled = false;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            // Trigger event for leaving
            this.OnPlayerLeaveRoomRequest();

            this.OnPlayerAircraftDestroyedRequest();
            Destroy(this.gameObject);
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;

            InputAilerons();
            InputElevators();
            InputDirection();
            InputThrust();

            if (Input.GetKeyDown(KeyCode.I))
            {
                RevertElevator = !RevertElevator;
                PlayerPrefs.SetInt("PlayerControl.ReverElevator", RevertElevator ? 1 : 0);
            }
        }

        public void InputAilerons()
        {
            aircraft.InputAilerons(Input.GetAxis("Mouse X") * AileronsSensivity);
        }

        public void InputElevators()
        {
            if (!RevertElevator)
                aircraft.InputElevators(Input.GetAxis("Mouse Y") * ElevatorsSensivity);
            else
                aircraft.InputElevators(-Input.GetAxis("Mouse Y") * ElevatorsSensivity);
        }

        public void InputDirection()
        {
            aircraft.InputDirection(Input.GetAxis("Horizontal") * DirectionSensivity);
        }

        public void InputThrust()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z))
            {
                aircraft.InputThrust(Time.deltaTime * ThrustSensivity);
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                aircraft.InputThrust(-Time.deltaTime * ThrustSensivity);
            }
        }

        [Command]
        public void SynchronizeNetworkPlayerInfo(string nickname)
        {
            Server_SynchronizeNetworkPlayerInfo(nickname);
        }

        [ClientRpc]
        public void Server_SynchronizeNetworkPlayerInfo(string nickname)
        {
            NetworkPlayerInfo.Nickname = nickname;
            NetworkPlayerInfo.NetworkIdentity = netIdentity;
        }
    }
}
