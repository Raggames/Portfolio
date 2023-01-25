using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WingsSim.Control;

public class HUDBeacons : MonoBehaviour
{
    public UIPlayerBeacon UIPlayerBeaconPrefab;
    protected List<UIPlayerBeacon> beacons = new List<UIPlayerBeacon>();

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
            return;

        UIPlayerBeacon newPlayerBeacon = Instantiate(UIPlayerBeaconPrefab, this.transform);
        newPlayerBeacon.Initialize(aircraftController);

        beacons.Add(newPlayerBeacon);
    }

    private void NetworkPlayerEventHandler_OnPlayerAircraftDestroyed(AircraftController aircraftController)
    {
        var beacon = beacons.Find(t => t.AircraftController == aircraftController);
        if (beacon == null)
            return;

        beacons.Remove(beacon);
        Destroy(beacon.gameObject);
    }

}
