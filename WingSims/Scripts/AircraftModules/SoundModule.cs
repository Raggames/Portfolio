using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.AircraftModules
{
    public enum UpdateMode
    {
        Speed, 
        Throttle
    }

    public class SoundModule : AircraftModule
    {
        [Header("Relative Wind")]
        public UpdateMode UpdateMode;

        public AudioClip Sound;
        public AudioSource Source;
        public float MaxPitch = 3;
        public float MinPitch = .2f;
        public float MinVolume = .5f;
        public float MaxVolume= 1f;

        protected override void Awake()
        {
            base.Awake();

            UpdateWithSpeed();
            Source.clip = Sound;
            Source.Play();
        }

        private void Update()
        {
            if (aircraft == null)
                return;

            switch (UpdateMode)
            {
                case UpdateMode.Speed:
                    UpdateWithSpeed();
                    break;
                case UpdateMode.Throttle:
                    UpdateWithThrottle();
                    break;
            }
        }

        public void UpdateWithSpeed()
        {
            float ratio = aircraft.CurrentSpeed / aircraft.MaxSpeed;
            Source.pitch = MinPitch + ratio * (MaxPitch - MinPitch);
            Source.volume = MinVolume + ratio * (MaxVolume - MinVolume);
        }

        public void UpdateWithThrottle()
        {
            float ratio = aircraft.Thrust / 100;
            Source.pitch = MinPitch + ratio * (MaxPitch - MinPitch);
            Source.volume = MinVolume + ratio * (MaxVolume - MinVolume);
        }
    }
}
