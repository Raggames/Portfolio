using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WingsSim.AircraftModules;
using WingsSim.Control;

public class HUDManager : MonoBehaviour
{
    public Aircraft aircraft;

    public TextMeshProUGUI Speed;
    public TextMeshProUGUI Altitude;
    public TextMeshProUGUI GroundAltitude;
    public TextMeshProUGUI Thrust;

    public TextMeshProUGUI VertSpeed;
    public TextMeshProUGUI Dist;
    public TextMeshProUGUI AY;
    public TextMeshProUGUI AZ;

    public TextMeshProUGUI Lift;
    public TextMeshProUGUI Drag;

    private float totalTime;
    private float distanceTravelled = 0;
    private Vector3 lastPos = Vector3.zero;

    public Transform Cross;
    public Transform CrossCenter;

    public float Xangle = 0;
    public float Yangle = 0;
    public float billeSmooth = .2f;
    private float yanglelevel;


    private void OnEnable()
    {
        NetworkPlayerEventHandler.OnPlayerAircraftSpawned += NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
    }

    private void NetworkPlayerEventHandler_OnPlayerAircraftSpawned(AircraftController aircraftController)
    {
        if (!aircraftController.isLocalPlayer)
            return;

        aircraft = aircraftController.Aircraft;
        distanceTravelled = 0;
        lastPos = aircraft.transform.position;
    }

    private void OnDisable()
    {
        NetworkPlayerEventHandler.OnPlayerAircraftSpawned -= NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
    }

    private void Update()
    {
        if (aircraft == null)
            return;

        Speed.text = Mathf.Round(aircraft.CurrentSpeed* 3.6f).ToString() + " km/h";
        VertSpeed.text = Mathf.Round(aircraft.rb.velocity.y).ToString() + " vert.km/h";
        Altitude.text = Mathf.Round(aircraft.transform.position.y).ToString() + " m";

        HeightDetectorEventHandler.GetHeighInfoRequest(aircraft, (heigh, prox) => GroundAltitude.text = Mathf.Round(heigh).ToString() + " m");
                
        Thrust.text = aircraft.Thrust.ToString() + " %";

        Xangle =  RMath.WrapAngle(aircraft.transform.eulerAngles.x) / 180;
        Yangle = Mathf.SmoothDamp(Yangle, -Vector3.SignedAngle(aircraft.rb.velocity, aircraft.transform.forward, Vector3.up) / 180, ref yanglelevel, billeSmooth);

        Cross.transform.position = CrossCenter.position + new Vector3(Yangle * Screen.width / 2, Xangle * Screen.height);
        Cross.transform.eulerAngles = new Vector3(Cross.transform.eulerAngles.x, Cross.transform.eulerAngles.y, -aircraft.transform.eulerAngles.z );


        distanceTravelled += (aircraft.transform.position - lastPos).magnitude;
        Dist.text = Mathf.Round(distanceTravelled) + " meters";
        lastPos = aircraft.transform.position;
        totalTime += Time.deltaTime;

        AZ.text = (Physics.gravity.magnitude * aircraft.rb.mass).ToString();
        AY.text = (distanceTravelled / totalTime).ToString();
        Lift.text = aircraft.CurrentLiftForce.ToString();
        Drag.text = aircraft.CurrenDragForce.ToString();
/*
        Vector3 accel = (fc.rb.velocity - lastVelocity) / Time.deltaTime;
        AX.text = accel.x.ToString() + " g";
        AY.text = accel.y.ToString() + " g";
        AZ.text = accel.z.ToString() + " g";
        lastVelocity = fc.rb.velocity;*/
    }
}
