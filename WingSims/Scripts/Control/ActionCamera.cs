using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WingsSim.AircraftModules;
using WingsSim.Control;

namespace Assets.Scripts.Control
{
    public enum ActionCamMode
    {
        FPV,
        Follow,
        Cinematic
    }

    public class ActionCamera : MonoBehaviour
    {
        [Header("Action Camera Parameters")]
        public ActionCamMode ActionCamMode;

        [Header("FPV Mode")]
        public int FPVFow = 60;

        [Header("Cinematic Mode")]
        public float CinematicLookAtSmoothness = 10;
        public float CinematicSmoothTime = 3f;

        [Header("FollowMode")]
        public float LookAtSmoothness = .5f;
        public int NormalFow = 60;
        public int MaxActionFow = 110;
        public float FowSmoothTime = .3f;

        private Vector3 currentVelocity;
        private float currentFowVel;
        protected Aircraft aircraft;

        private void OnEnable()
        {
            NetworkPlayerEventHandler.OnPlayerAircraftSpawned += NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
        }

        private void OnDisable()
        {
            NetworkPlayerEventHandler.OnPlayerAircraftSpawned -= NetworkPlayerEventHandler_OnPlayerAircraftSpawned;
        }

        private void NetworkPlayerEventHandler_OnPlayerAircraftSpawned(AircraftController aircraftController)
        {
            if (aircraftController.isLocalPlayer)
            {
                ActionCamMode = ActionCamMode.FPV;

                this.transform.position = aircraftController.Aircraft.CameraFollowAnchor.position;
                this.transform.LookAt(aircraftController.Aircraft.transform);

                this.aircraft = aircraftController.Aircraft;
            }
        }

        public void OnCrash()
        {
            this.transform.SetParent(null);
            ActionCamMode = ActionCamMode.Cinematic;
        }

        public void SetCameraMode(ActionCamMode actionCamMode)
        {
            ActionCamMode = actionCamMode;
        }

        private void Update()
        {
            if (aircraft == null)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetCameraMode(ActionCamMode.FPV);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SetCameraMode(ActionCamMode.Follow);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                SetCameraMode(ActionCamMode.Cinematic);

            switch (ActionCamMode)
            {
                case ActionCamMode.FPV:
                    UpdateFPVMode();
                    break;
                case ActionCamMode.Follow:
                    UpdateFollowMode();
                    break;
                case ActionCamMode.Cinematic:
                    UpdateCinematicMode();
                    break;
            }
        }

        private void UpdateFollowMode()
        {
            //this.transform.position = Vector3.SmoothDamp(this.transform.position, aircraft.CameraFollowAnchor.position, ref currentVelocity, SmoothTime);
            this.transform.position = aircraft.CameraFollowAnchor.position;

            Quaternion targetRotation = Quaternion.LookRotation(aircraft.transform.position - this.transform.position, aircraft.transform.up);
            //this.transform.rotation = Quaternion.LookRotation(aircraft.transform.position - this.transform.position);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, LookAtSmoothness * Time.deltaTime);

            HeightDetectorEventHandler.GetHeighInfoRequest(aircraft, (heigh, proxalt) =>
            {

                if (heigh <= proxalt)
                {
                    float delta = MaxActionFow - NormalFow;
                    float fowPerMeter = delta / proxalt;

                    Camera.main.fieldOfView = Mathf.SmoothDamp(Camera.main.fieldOfView, fowPerMeter * (proxalt - heigh) + NormalFow, ref currentFowVel, FowSmoothTime);
                }
                else
                {
                    Camera.main.fieldOfView = Mathf.SmoothDamp(Camera.main.fieldOfView, NormalFow, ref currentFowVel, FowSmoothTime);
                }
            });


        }

        private void UpdateCinematicMode()
        {
            this.transform.position = Vector3.SmoothDamp(this.transform.position, aircraft.CameraFollowAnchor.position, ref currentVelocity, CinematicSmoothTime);

            Quaternion targetRotation = Quaternion.LookRotation(aircraft.transform.position - this.transform.position);
            //this.transform.rotation = Quaternion.LookRotation(aircraft.transform.position - this.transform.position);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, CinematicLookAtSmoothness * Time.deltaTime);
        }

        private void UpdateFPVMode()
        {
            Camera.main.fieldOfView = FPVFow;
            this.transform.position = aircraft.CameraFPVAnchor.position;
            this.transform.rotation = aircraft.transform.rotation;
        }
    }
}
