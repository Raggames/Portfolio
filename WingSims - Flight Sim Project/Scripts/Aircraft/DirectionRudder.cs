using Assets.Scripts.Control;
using Assets.WingsSim.Scripts.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class DirectionRudder : Rudder
{
    public override void UpdateRudder(float input) // From -1 to 1 (input)
    {
        if (Revert)
            input = -input;

        CurrentAngle = input * DebattementAngle;
        CurrentAngle = Mathf.Clamp(CurrentAngle, -DebattementAngle, DebattementAngle);

        if (MovableParts.Count > 0)
        {
            float movAngle = -CurrentAngle;
            if (RevertMovablePart)
                movAngle = -movAngle;

            for (int i = 0; i < MovableParts.Count; ++i)
            {
                MovableParts[i].Move(0, movAngle, 0);
            }
        }

        RudderAngleEfficiency = RudderAngleEfficiencyCurve.Evaluate(Mathf.Abs(CurrentAngle));

        CurrentDeltaCz = flightController.RuddersSensivity * Sensivity * RudderAngleEfficiency * AeroSurface * CurrentAngle * flightController.CurrentSpeed * flightController.Rho * flightController.BalancingConstant * Time.fixedDeltaTime;

        Vector3 forceDirection = transform.up * CurrentDeltaCz;
        Debug.DrawLine(transform.position, transform.position + forceDirection, Color.cyan);
        flightController.rb.AddTorque(forceDirection);
    }

    public override void ComputeAeroSurface()
    {
        AeroSurface = RectangularPlaneProjection.ComputeRectangularSurfaceOnPlane(Vector3.right, CornerA.position, CornerB.position, CornerC.position, CornerD.position);
    }

}

