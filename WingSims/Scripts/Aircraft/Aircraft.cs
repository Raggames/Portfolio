using Assets.Scripts.Control;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WingsSim;


public class Aircraft : MonoBehaviour
{
    // Anchors
    public Transform CameraFollowAnchor;
    public Transform CameraFPVAnchor;

    // Parts
    public Rigidbody rb;
    private AeroPart[] aeroParts;
    public AileronRudder[] ailerons;
    public ElevatorRudder[] elevators;
    public DirectionRudder[] directions;
    public Motor[] motors;

    // Paramètres
    public float RuddersSensivity = 1f;
    public float BalancingConstant = 1f;
    public float PortanceConstant = 1;
    public float MaxDragConstant = 5;
    public float MinDragConstant = .8f;    
    [Range(0, 100)] public float Thrust;

    // Constantes
    public float Rho = 1.295f;
    public float MaxSpeed = 150;

    // Variables
    public float CurrentSpeed = 0;
    public float CurrentIncidence = 0;
    public float CurrentWeightForce;
    public float CurrentLiftForce;
    public float CurrenDragForce;
    public float CurrentDragConstant = 1;
    public Vector3 CurrentCineticEnergy;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aeroParts = GetComponentsInChildren<AeroPart>();

        for (int i = 0; i < aeroParts.Length; ++i)
        {
            aeroParts[i].Initialize(this);
        }

        motors = GetComponentsInChildren<Motor>();
        for (int i = 0; i < motors.Length; ++i)
        {
            motors[i].Initialize(this);
        }

        var rbs = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rbs.Length; ++i)
        {
            CurrentWeightForce += rbs[i].mass;
        }
        CurrentWeightForce *= Physics.gravity.magnitude;

        var rudders = GetComponentsInChildren<Rudder>();
        for (int i = 0; i < rudders.Length; ++i)
        {
            rudders[i].Initialize(this);
        }        
    }

    // Used to recalculte all the aeroSurfaces areas (in square meters)
    [Button("Compute Aero Surface")]
    private void ComputeAeroSurface()
    {
        aeroParts = GetComponentsInChildren<AeroPart>();

        for (int i = 0; i < aeroParts.Length; ++i)
        {
            aeroParts[i].ComputeAeroSurface();
        }
    }

    private void FixedUpdate()
    {       
        CurrentDragConstant = Thrust / 100 * (MaxDragConstant - MinDragConstant) + MinDragConstant;
        CurrentIncidence = -Vector3.SignedAngle(rb.velocity, transform.forward, transform.right);

        ComputeThrustPower();
        ComputePortance();
        ComputeDrag();

        CurrentSpeed = rb.velocity.magnitude;
        CurrentCineticEnergy = rb.velocity.normalized * (rb.velocity.sqrMagnitude * (CurrentWeightForce / Physics.gravity.magnitude));

        Debug.DrawLine(transform.position, transform.position + Physics.gravity * rb.mass, Color.red);
        Debug.DrawLine(transform.position, transform.position + rb.velocity, Color.green);
    }

    public void InputAilerons(float input)
    {
        for (int i = 0; i < ailerons.Length; ++i)
        {
            if (ailerons[i].gameObject.activeSelf)
                ailerons[i].UpdateRudder(input);
        }
    }

    public void InputElevators(float input)
    {
        for (int i = 0; i < elevators.Length; ++i)
        {
            if (elevators[i].gameObject.activeSelf)
                elevators[i].UpdateRudder(input);
        }
    }

    public void InputDirection(float input)
    {
        for (int i = 0; i < directions.Length; ++i)
        {
            if (directions[i].gameObject.activeSelf)
                directions[i].UpdateRudder(input);
        }
    }

    public void InputThrust(float input)
    {
        Thrust += input;       
        Thrust = Mathf.Clamp(Thrust, 0, 100);
    }

    private void ComputeThrustPower()
    {
        for (int i = 0; i < motors.Length; ++i)
        {
            motors[i].AddForce(Thrust);
        }
    }

    private void ComputePortance()
    {
        CurrentLiftForce = 0;
        for (int i = 0; i < aeroParts.Length; ++i)
        {
            if (aeroParts[i].gameObject.activeSelf)
            {
                aeroParts[i].ApplyPortance();
                CurrentLiftForce += aeroParts[i].CurrentPortanceForce;
            }
        }
    }

    private void ComputeDrag()
    {
        CurrenDragForce = 0;
        for (int i = 0; i < aeroParts.Length; ++i)
        {
            if (aeroParts[i].gameObject.activeSelf)
            {
                aeroParts[i].ApplyDrag();
                CurrenDragForce += aeroParts[i].CurrentDragForce;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("World"))
        {
            this.Thrust = 0;
            /* this.HasControl = false;
             Camera.main.GetComponent<ActionCamera>().OnCrash();*/
        }
    }

}
