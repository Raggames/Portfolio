using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WingsSim.Control;
using WingsSim.Network;

public class GameManager : MonoBehaviour
{
    public Vector3 Gravity;
    public Transform Respawn;

    private bool mouseState = false;

    private static AircraftController localPlayerAircraftController;
    public static AircraftController LocalPlayerAircraftController => localPlayerAircraftController;

    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = Gravity;
    }

    private void SetMouse(bool state)
    {
        if (!state)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

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
        if (aircraftController.isLocalPlayer)
        {
            mouseState = false;
            SetMouse(mouseState);
            localPlayerAircraftController = aircraftController;
        }
    }

    private void NetworkPlayerEventHandler_OnPlayerAircraftDestroyed(AircraftController aircraftController)
    {
        if (aircraftController.isLocalPlayer)
        {
            Debug.LogError("Local player destroyed plane.");
            localPlayerAircraftController = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (localPlayerAircraftController != null)
            {
                localPlayerAircraftController.transform.position = Respawn.position;
                localPlayerAircraftController.transform.rotation = Respawn.rotation;
                localPlayerAircraftController.Aircraft.rb.velocity = Vector3.zero;
                localPlayerAircraftController.Aircraft.rb.angularVelocity = Vector3.zero;
            }
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            for(int i = 0; i < NetworkRoom.RoomAircrafts.Count; ++i)
            {
                if(NetworkRoom.RoomAircrafts[i] != LocalPlayerAircraftController)
                {
                    localPlayerAircraftController.transform.position = NetworkRoom.RoomAircrafts[i].Aircraft.transform.position + UnityEngine.Random.insideUnitSphere * 15;
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse2))
        {
            mouseState = !mouseState;
            SetMouse(mouseState);
        }
    }
}
