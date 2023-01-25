using Assets.WingsSim.Scripts.Math;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroPart : MonoBehaviour
{
    [Header("Aeropart")]

    protected Rigidbody rb;
    protected Aircraft flightController;

    public float AeroSurface;
    public float MinSurface;
    public float CalageAngle;
    public float Cz;
    public float Cx;

    public AnimationCurve IncidenceCzRatio;
    [ReadOnly] public float CurrentIncidenceCzRatio;

    public AnimationCurve ResistanceSurfaceToCxRatioCurve;
    [ReadOnly] public float CurrentCxRatio;
    [ReadOnly] public float CurrentPortanceForce;
    [ReadOnly] public float CurrentDragForce;
    [ReadOnly] public float CurrentResistantSurface;

    public Transform CornerA;
    public Transform CornerB;
    public Transform CornerC;
    public Transform CornerD;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (CornerA != null && CornerB != null && CornerC != null && CornerD != null)
            ComputeAeroSurface();
    }

    public void Initialize(Aircraft flightController)
    {
        this.flightController = flightController;
    }

    public float ComputeIncidenceRatio()
    {
        return IncidenceCzRatio.Evaluate(flightController.CurrentIncidence + CalageAngle);
    }

    public virtual void ComputeAeroSurface()
    {
        AeroSurface = RectangularPlaneProjection.ComputeRectangularSurfaceOnPlane(Vector3.up, CornerA.position, CornerB.position, CornerC.position, CornerD.position);
    }

    public virtual void ApplyPortance()
    {
        CurrentIncidenceCzRatio = IncidenceCzRatio.Evaluate(flightController.CurrentIncidence + CalageAngle);
        
        CurrentPortanceForce = 0.5f * flightController.Rho * flightController.rb.velocity.sqrMagnitude * Cz * CurrentIncidenceCzRatio * AeroSurface * flightController.PortanceConstant * flightController.BalancingConstant; // Divided by ten to convert newtons to kilograms with Earth gravity..

        //CurrentPortanceForce = Mathf.Clamp(CurrentPortanceForce, 0, flightController.CurrentWeight);
        Vector3 portanceVector = Vector3.Cross(flightController.rb.velocity.normalized, transform.right) * CurrentPortanceForce;

        Debug.DrawLine(transform.position, transform.position + portanceVector, Color.green);
        flightController.rb.AddForce(portanceVector * Time.fixedDeltaTime);
    }

    public virtual void ApplyDrag()
    {
        /*float ratio = ComputeAeroResistanceRatio();
        CurrentAeroResistance = AeroResistanceCurve.Evaluate(ratio);*/
        CurrentResistantSurface = ComputeResistantSurface();
        CurrentCxRatio = ResistanceSurfaceToCxRatioCurve.Evaluate(CurrentResistantSurface / AeroSurface);

        CurrentDragForce = .5f * flightController.Rho * Cx * flightController.rb.velocity.sqrMagnitude * AeroSurface * CurrentCxRatio * flightController.CurrentDragConstant * flightController.BalancingConstant;
        Vector3 dragVector = -flightController.rb.velocity * CurrentDragForce;

        Debug.DrawLine(transform.position, transform.position + dragVector, Color.red);
        flightController.rb.AddForce(dragVector * Time.fixedDeltaTime);
    }

    /*
        public virtual float ComputeAeroResistanceRatio()
        {
            // Cas de l'aile, orientée dans le plan x, z
            ratio_x_axis = Mathf.Sin((Vector3.Angle(flightController.rb.velocity, flightController.transform.up) - 90) * Mathf.Deg2Rad);
            ratio_y_axis = Mathf.Sin((Vector3.Angle(flightController.rb.velocity, flightController.transform.right) - 90) * Mathf.Deg2Rad);

            return Mathf.Abs((ratio_x_axis + ratio_y_axis) / 2);
        }*/

    public float ComputeResistantSurface()
    {
        return Mathf.Max(RectangularPlaneProjection.ComputeRectangularSurfaceOnPlane(flightController.rb.velocity, CornerA.position, CornerB.position, CornerC.position, CornerD.position), MinSurface);
    }
}
