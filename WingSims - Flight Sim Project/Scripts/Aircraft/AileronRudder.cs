using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Control
{
    public class AileronRudder : Rudder
    {
        public float LacetRatio = 1;

        public override void UpdateRudder(float input) // From -1 to 1 (input)
        {
            if (Revert)
                input = -input;

            CurrentAngle = -input * DebattementAngle;
            CurrentAngle = Mathf.Clamp(CurrentAngle, -DebattementAngle, DebattementAngle);

            if (MovableParts.Count > 0)
            {
                float movAngle = -CurrentAngle;
                if (RevertMovablePart)
                    movAngle = -movAngle;

                for(int i = 0; i < MovableParts.Count; ++i)
                {
                    MovableParts[i].Move(movAngle, 0, 0);
                }
            }

            RudderAngleEfficiency = RudderAngleEfficiencyCurve.Evaluate(Mathf.Abs(CurrentAngle));
            CurrentDeltaCz = flightController.RuddersSensivity * Sensivity * RudderAngleEfficiency * AeroSurface * CurrentAngle * flightController.CurrentSpeed * flightController.Rho * flightController.BalancingConstant * Time.fixedDeltaTime;

            Vector3 forceDirection = transform.forward * CurrentDeltaCz;
            Debug.DrawLine(transform.position, transform.position + forceDirection, Color.cyan);
            flightController.rb.AddTorque(forceDirection);
        }

        public override void ApplyDrag()
        {
            float speed = flightController.rb.velocity.magnitude;
            float drag = .5f * flightController.Rho * Mathf.Sin(CurrentAngle * Mathf.Deg2Rad) * speed * speed * AeroSurface * flightController.CurrentDragConstant * Time.fixedDeltaTime * LacetRatio * flightController.BalancingConstant;
            Vector3 dragVector = -flightController.transform.up * drag;

            Debug.DrawLine(transform.position, transform.position + dragVector, Color.yellow);
            flightController.rb.AddTorque(dragVector );
        }
    }
}
