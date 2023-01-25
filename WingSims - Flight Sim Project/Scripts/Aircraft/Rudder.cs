using Assets.WingsSim.Scripts.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Control
{
    public class Rudder : AeroPart
    {
        [Header("Rudder")]
        public List<MovablePart> MovableParts;

        public bool Revert;
        public bool RevertMovablePart;
        
        public float RudderAngleEfficiency;
        public AnimationCurve RudderAngleEfficiencyCurve;

        public float Sensivity = .01f;
        public int DebattementAngle = 40;

        public float CurrentAngle;
        public float CurrentDeltaCz;
                
        public virtual void UpdateRudder(float input) // From -1 to 1 (input)
        {
            if (Revert)
                input = -input;          
        }

        public override void ApplyPortance()
        {
        }

        public override void ApplyDrag()
        {
            /*CurrentCx = RudderCxAngleCurve.Evaluate(CurrentAngle);

            float speed = flightController.rb.velocity.magnitude;
            float drag = .5f * flightController.Rho * CurrentCx * speed * speed * RudderSurface * flightController.DragConstant;
            Vector3 dragVector = -flightController.rb.velocity * drag;

            Debug.DrawLine(transform.position, transform.position + dragVector, Color.red);
            flightController.rb.AddTorque(dragVector * Time.deltaTime);*/
        }
    }
}
