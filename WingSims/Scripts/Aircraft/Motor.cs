using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim
{
    public class Motor : MonoBehaviour
    {
        public Aircraft aircraft;
        public float EnginePower;
        public AnimationCurve thrustPowerCurve;

        public void Initialize(Aircraft aircraft)
        {
            this.aircraft = aircraft;
        }

        public void AddForce(float thrust)
        {
            float power = thrustPowerCurve.Evaluate(thrust / 100f) * EnginePower;

            Vector3 engineForce = transform.forward * power *  aircraft.BalancingConstant;
            aircraft.rb.AddForce(engineForce * Time.fixedDeltaTime);
            Debug.DrawLine(transform.position, transform.position + engineForce, Color.blue);
        }
    }
}
