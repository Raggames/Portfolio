using Assets.WingsSim.Scripts.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.WingsSim.Scripts.Control
{
    public enum StabilizerAxis
    {
        Direction,
        Profondeur
    }

    public class Stabilizer : AeroPart
    {
        public StabilizerAxis StabilizerAxis;
        private Vector3 stabilizationForce;

        public float NormalAngle;

        public override void ApplyPortance()
        {
        }

        public override void ApplyDrag()
        {
            Vector3 stabilizerPlaneNormal = Vector3.Cross(CornerA.position - CornerB.position, CornerA.position - CornerC.position);
            Debug.DrawRay(transform.position, stabilizerPlaneNormal * 10, Color.black);
            NormalAngle = Vector3.Angle(stabilizerPlaneNormal, flightController.rb.velocity) - 90;

            CurrentResistantSurface = ComputeResistantSurface();
            CurrentCxRatio = ResistanceSurfaceToCxRatioCurve.Evaluate(CurrentResistantSurface / AeroSurface);

            float ratio = Mathf.Sin(NormalAngle * Mathf.Deg2Rad) * CurrentCxRatio * AeroSurface * Cx * flightController.rb.velocity.sqrMagnitude * flightController.Rho * flightController.BalancingConstant * flightController.CurrentDragConstant * GetLevierRadius();

            switch (StabilizerAxis)
            {
                case StabilizerAxis.Direction:
                    stabilizationForce = transform.up * ratio;
                    Debug.DrawLine(transform.position, transform.position + stabilizationForce, Color.yellow);
                    break;
                case StabilizerAxis.Profondeur:
                    stabilizationForce = transform.right * ratio;
                    Debug.DrawLine(transform.position, transform.position + stabilizationForce, Color.yellow);
                    break;
            }

            flightController.rb.AddTorque(stabilizationForce * Time.fixedDeltaTime, ForceMode.Force);
        }

        public float GetLevierRadius()
        {
            switch (StabilizerAxis)
            {
                case StabilizerAxis.Direction:
                    return Mathf.Abs(this.transform.position.z);
                case StabilizerAxis.Profondeur:
                    return Mathf.Abs(this.transform.position.z);
            }

            return 1;
        }

        public override void ComputeAeroSurface()
        {
            switch (StabilizerAxis)
            {
                case StabilizerAxis.Direction:
                    AeroSurface = RectangularPlaneProjection.ComputeRectangularSurfaceOnPlane(Vector3.right, CornerA.position, CornerB.position, CornerC.position, CornerD.position);
                    break;
                case StabilizerAxis.Profondeur:
                    base.ComputeAeroSurface();
                    break;
            }


        }
    }
}
