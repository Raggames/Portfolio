using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.BattleGame.Scripts.Controllers
{
    public class CameraOperator : Singleton<CameraOperator>, IInputDownListenner, IInputUpListenner
    {
        public bool HasRotationControl = true;
        public bool HasControl = true;
        public bool ComputeArcOnUpdate = false;

        private bool isEasingTo = false;

        #region Fields

        private Character owner;

        [SerializeField]
        private Transform _follow;
        [SerializeField]
        private Camera _camera;

        // Parameters
        public GameObject actualColliding;

        [Space]
        public float cameraRotationSensivity = 250;
        [Space]
        public float zoomSpeed = -200;
        public float zoomLerp = 7f;
        public float minZoom = 0.85f;
        public float maxZoom = 2.5f;
        public float aimingZoom = 0.8f;
        [Space]
        public int circleRatio = 20;
        public float arcSmoothness = 60;
        public float normalSmoothness = .01f;
        public float dampingSmoothness = .02f;
        public float collisionSmoothness = .03f;
        public float cameraRotationSmoothness = 120f;
        public float cameraLookSmoothness = 2f;
        [Space]
        public int aimingFow = 40;
        public int normalFow = 60;
        [Space]
        public float dampingTime = 1;
        public float aimingTime = 0.25f; // vitesse de smooth entre fow normal et fow aiming.
        [Space]
        public Vector3 baseOffset = Vector3.zero;
        public Vector3 shakingOffset = Vector3.zero;
        private Vector3 normalRefCamPosition;
        private Vector3 collisionRefCamPosition;
        [Space]
        public bool canAim = true;
        [Space]
        public float lowOrbitRadius = 2;
        public float lowOrbitAltitude = -0.15f;

        public float midOrbitRadius = 6;
        public float midOrbitAltitude = 0;

        public float heightOrbitRadius = 3;
        public float heightOrbitAltitude = 5.5f;
        [Space]
        public float collisionHeadLockDistance = 1.5f;
        public float collisionSecurityDistance = 1f;
        [Space]
        public float camTargetDistanceMultiplier = 1f;
        public float camBoxSize = 0.6f;
        public float playerBoxSize = 0.5f;
        public float sphereCastRadius = .4f;

        [ShowInInspector, ReadOnly] private List<RaycastHit> currentlyColliding = new List<RaycastHit>();

        // Variables
        [SerializeField] private Vector3 angles = Vector3.zero; // ANGLE SUR L'AXE UP et RIGHT (coordonnées polaires 3D)
        [SerializeField] private float currentSmoothness = 0;
        [SerializeField] private float cameraHeadDist = 0;
        [SerializeField] private float zoom = 0;
        [ShowInInspector, ReadOnly] private float curZoom = 0;
        private float lastZoom = 0;
        [SerializeField] private float encounterZoom = 0;
        private float axisX = 0;
        private float axisY = 0;
        private Vector2 easeInput;
        private Vector2 easeInputDamp;
        private float easeSmoothness = 0.4f;
        private Vector3[] arcArray = null;
        private Vector3[] arc = null;
        private Vector3 headPos = Vector3.zero;
        private Vector3[] offsetCircle = null;
        private Vector3 lookPosition = Vector3.zero;
        private Vector3 computedLookPosition = Vector3.zero;
        private Vector3 operatorPosition = Vector3.zero;
        private Vector3 baseCamAxle = Vector3.zero;
        private bool isColliding = false;
        private bool isAiming = false;
        private RaycastHit playerToOperator;
        private Coroutine _dampRoutine = null;
        private Coroutine _startAimRoutine = null;
        private Coroutine _endAimRoutine = null;
        private Coroutine _encounterHandlingRoutine = null;
        [SerializeField]
        private Material[] _mats;
        public Renderer[] rends;

        public float CamToPlayerDistance
        {
            get
            {
                return Vector3.Distance(Camera.main.transform.position, owner.chestTransform.position);
            }
        }
        #endregion

        #region Init

        private void Awake()
        {
            this.gameObject.SetActive(false);
        }

        public void OnEnable()
        {
            InputsManager.AddCallbackTarget(this, this, null);
        }

        public void OnDisable()
        {
            InputsManager.RemoveCallbackTarget(this, this, null);
        }

        public void Init(Character player)
        {
            owner = player;

            angles.x = 0;
            angles.y = 130;

            if(baseOffset.x != 0)
            {
                offsetCircle = new Vector3[(int)(360 * circleRatio)];
                for (int i = 0; i < offsetCircle.Length; ++i)
                {
                    offsetCircle[i] = PositionOnCircle(new Vector3(0, baseOffset.y, 0), baseOffset.x, i);
                }
            }
            else
            {
                offsetCircle = new Vector3[1];
                offsetCircle[0] = new Vector3(0, baseOffset.y, 0);
            }

            arcArray = new Vector3[3]
            {
                new Vector3(0, lowOrbitAltitude, -lowOrbitRadius),
                new Vector3(0, midOrbitAltitude, -midOrbitRadius),
                new Vector3(0, heightOrbitAltitude, -heightOrbitRadius),
            };
            arc = Vector3Utils.MakeSmoothCurve(arcArray, arcSmoothness * 10f);

            currentSmoothness = normalSmoothness;

            if (player != null)
            {
                Transform[] childs = player.transform.GetComponentsInChildren<Transform>();
                for (int i = 0; i < childs.Length; ++i)
                {
                    if (childs[i].name == "camFollow")
                    {
                        _follow = childs[i];
                    }
                }
                if (_follow == null)
                {
                    Debug.LogError("CamOperator initialisation issue : follow transform not found in player.");
                }
            }

            _camera = Camera.main;

            rends = owner.gameObject.GetComponentsInChildren<Renderer>();
            _mats = new Material[rends.Length];
            for (int i = 0; i < rends.Length; ++i)
            {
                _mats[i] = rends[i].material;
            }

            // Initialisation de la Position/Rotation
            lookPosition = GetLookPosition();
            operatorPosition = GetOperatorPosition(angles.x, angles.y / 180f);
            _camera.transform.position = operatorPosition;
            _camera.transform.LookAt(lookPosition);

            this.gameObject.SetActive(true);
        }

        #endregion

        #region Update

        public void ForcePosition(Vector3 position)
        {
            operatorPosition = position;
            _camera.transform.position = position;
        }

        private void Update()
        {
            if (!HasControl)
                return;

            if (ComputeArcOnUpdate)
            {
                offsetCircle = new Vector3[(int)(360 * circleRatio)];
                for (int i = 0; i < offsetCircle.Length; ++i)
                {
                    offsetCircle[i] = PositionOnCircle(new Vector3(0, baseOffset.y, 0), baseOffset.x, i);
                }

                arcArray = new Vector3[3]
                {
                new Vector3(0, lowOrbitAltitude, -lowOrbitRadius),
                new Vector3(0, midOrbitAltitude, -midOrbitRadius),
                new Vector3(0, heightOrbitAltitude, -heightOrbitRadius),
                };
                arc = Vector3Utils.MakeSmoothCurve(arcArray, arcSmoothness * 10f);
            }

            headPos = _follow.transform.position + new Vector3(0, baseOffset.y, 0);

            // Compute Axis
            if (!isEasingTo)
            {
                if (HasRotationControl)
                {
                    axisX = Input.GetAxis("Mouse X");
                    axisY = Input.GetAxis("Mouse Y");

                    angles.x += axisX * Time.deltaTime * cameraRotationSensivity;
                    angles.y -= axisY * Time.deltaTime * cameraRotationSensivity;
                }
            }

            if (angles.x < 0)
            {
                angles.x = 360 + angles.x;
            }

            angles.x = angles.x % 360;
            angles.y = Mathf.Clamp(angles.y, 0f, 180f);

            if (!isAiming)
            {
                curZoom += Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomSpeed;
                curZoom = Mathf.Clamp(curZoom, minZoom, maxZoom);
                zoom = Mathf.Lerp(zoom, curZoom, Time.deltaTime * zoomLerp);
                zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            }

            // COMPUTING FOLLOW POSITION
            lookPosition = GetLookPosition();

            // COMPUTING OPERATOR POSITION
            operatorPreviousPosition = operatorPosition;
            operatorPosition = GetOperatorPosition(angles.x, angles.y / 180f);
            operatorSpeed = (operatorPreviousPosition - operatorPosition).magnitude / Time.deltaTime;

            //transform.position = Vector3.Lerp(transform.position, operatorPosition, Time.deltaTime * operatorSmoothness);
            transform.position = operatorPosition;

            HandleCameraPosition();
        }

        public bool UseNormalSmooth = true;
        public float operatorSpeed = 0;
        public Vector3 operatorPreviousPosition;
        private void HandleCameraPosition()
        {
            Vector3 lookHeadAxis = lookPosition - headPos;
            baseCamAxle = headPos - operatorPosition;

            int mask = 1 << 11;
            mask |= 1 << 13;
            mask |= 1 << 6;
            mask = ~mask;

            RaycastHit[] allHits = RaycastManager.SphereCastHitAllTo(_follow.position, operatorPosition, mask, sphereCastRadius);
            bool collide = false;
            bool ignoreCam = false;

            currentlyColliding.Clear();
            for (int i = 0; i < allHits.Length; ++i)
            {
                Debug.DrawLine(headPos, allHits[i].point, Color.red);
                if (allHits[i].collider.gameObject.CompareTag("World"))
                {
                    collide = true;
                    currentlyColliding.Add(allHits[i]);
                    break;
                }
                else if (allHits[i].collider.gameObject.CompareTag("WorldIgnoreCam"))
                {
                    ignoreCam = true;
                    currentlyColliding.Add(allHits[i]);
                }
            }

            // Si on a un objet qui permet à la camera de se placer derriere, on vérifie tout de meme que la cam n'est pas dans l'objet
            if (ignoreCam)
            {
                var colls = Physics.OverlapSphere(_camera.transform.position, camBoxSize, mask);
                for (int i = 0; i < colls.Length; ++i)
                {
                    if (colls[i].gameObject.CompareTag("WorldIgnoreCam"))
                    {
                        collide = true;
                        break;
                    }
                }
            }

            if (!collide)
            {
                currentSmoothness = normalSmoothness;

                if (isColliding)
                {
                    if (_dampRoutine != null)
                    {
                        StopCoroutine(_dampRoutine);
                    }
                    _dampRoutine = StartCoroutine(Damp());
                    isColliding = false;
                }

                if (UseNormalSmooth)
                {
                    _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, operatorPosition, ref normalRefCamPosition, currentSmoothness / 10f);
                }
                else
                {
                    _camera.transform.position = operatorPosition; // Vector3.SmoothDamp(_camera.transform.position, operatorPosition, ref refCamPosition, currentNormalSmoothness / 10f);
                }
            }
            else
            {
                isColliding = true;
                currentSmoothness = dampingSmoothness;

                float minDist = GetClosestHitPoint(baseCamAxle.magnitude, currentlyColliding);
                Vector3 computedPos = headPos + ((operatorPosition - headPos).normalized * minDist);
                //_camera.transform.position = Vector3.Lerp(_camera.transform.position, computedPos, Time.deltaTime * collisionSmoothness);
                _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, computedPos, ref collisionRefCamPosition, collisionSmoothness / 10);
            }

            cameraHeadDist = Vector3.Distance(_camera.transform.position, headPos);
            float wantedDist = Vector3.Distance(operatorPosition, headPos);
            float computedDist = lookHeadAxis.magnitude * camTargetDistanceMultiplier;
            computedLookPosition = headPos + lookHeadAxis.normalized * computedDist;// + baseCamAxle.normalized;
            computedLookPosition += (computedLookPosition - operatorPosition).normalized * 2;

            Debug.DrawLine(operatorPosition, lookPosition, Color.green);
            Debug.DrawLine(operatorPosition, headPos, Color.green);
            Debug.DrawLine(operatorPosition, computedLookPosition, Color.blue);
            Debug.DrawLine(headPos, computedLookPosition, Color.red);

            UpdateMaterialsOpacity(cameraHeadDist, wantedDist);

            Quaternion targetRotation = Quaternion.LookRotation(computedLookPosition - _camera.transform.position);
            // Smoothly rotate towards the target point.
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRotation, cameraRotationSmoothness * Time.deltaTime);
            _camera.transform.position += shakingOffset;
        }

        #endregion

        #region Aiming
        public void StartAim()
        {
            isAiming = true;
            if (_endAimRoutine != null)
            {
                StopCoroutine(_endAimRoutine);
            }

            if (_startAimRoutine != null)
            {
                StopCoroutine(_startAimRoutine);
            }

            _startAimRoutine = StartCoroutine(StartAimRoutine());
        }

        public void EndAim()
        {
            if (_startAimRoutine != null)
            {
                StopCoroutine(_startAimRoutine);
            }

            if (_endAimRoutine != null)
            {
                StopCoroutine(_endAimRoutine);
            }

            _endAimRoutine = StartCoroutine(EndAimRoutine());
        }

        IEnumerator StartAimRoutine()
        {
            float _fow = _camera.fieldOfView;
            float timer = 0;
            lastZoom = zoom;
            // Manage zoom

            while (timer < aimingTime)
            {
                timer += Time.deltaTime;
                _camera.fieldOfView = RMath.LinearTween(_fow, aimingFow, timer / aimingTime);
                zoom = RMath.LinearTween(lastZoom, aimingZoom, timer / aimingTime);
                zoom = Mathf.Clamp(zoom, aimingZoom, maxZoom);
                yield return null;
            }

            _startAimRoutine = null;
        }

        IEnumerator EndAimRoutine()
        {
            float _fow = _camera.fieldOfView;
            float timer = 0;
            float startZoom = zoom;
            // Manage zoom

            while (timer < aimingTime)
            {
                timer += Time.deltaTime;
                _camera.fieldOfView = RMath.LinearTween(_fow, normalFow, timer / aimingTime);
                zoom = RMath.LinearTween(startZoom, lastZoom, timer / aimingTime);
                //zoom = Mathf.Clamp(zoom, aimingZoom < minZoom ? aimingZoom : minZoom, maxZoom);

                yield return null;
            }

            _endAimRoutine = null;
            isAiming = false;
        }

        private LTDescr fowTween;
        public void SetFowTween(int targetValue, float time)
        {
            if(fowTween!= null)
            {
                LeanTween.cancel(this.gameObject);
            }

            fowTween = LeanTween.value(_camera.fieldOfView, targetValue, time).setOnUpdate((value) => _camera.fieldOfView = value); 
        }

        #endregion

        #region Utils

        IEnumerator Damp()
        {
            float timer = 0;
            currentSmoothness = dampingSmoothness;

            while (timer < 1)
            {
                timer += (Time.deltaTime / dampingTime);
                currentSmoothness = RMath.LinearTween(dampingSmoothness, normalSmoothness, timer);
                yield return null;
            }

            _dampRoutine = null;
        }

        private float GetClosestHitPoint(float maxDistance, List<RaycastHit> hits)
        {
            float bestDistance = maxDistance;
            for (int i = 0; i < hits.Count; ++i)
            {
                float d = Vector3.Distance(headPos, hits[i].point);
                actualColliding = hits[i].collider.gameObject;
                if (d < bestDistance)
                {
                    bestDistance = d;
                }
            }

            return bestDistance == maxDistance ? maxDistance : bestDistance;
        }

        private Vector3 GetLookPosition()
        {
            if(baseOffset.x > 0)
            {
                float position = Mathf.Abs(angles.x / 360f);
                int pos = 360 - (int)(position * 360f);

                if (pos < 0)
                    pos = 0;
                if (pos > 359)
                    pos = 359;

                return offsetCircle[pos] + _follow.position;
            }
            else
            {
                return offsetCircle[0] + _follow.position;
            }
        }

        public Vector3 PositionOnCircle(Vector3 center, float radius, float angle)
        {
            float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
            float y = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
            return new Vector3(x, center.y, y);
        }

        protected Vector3 RotateVector(Vector3 toRotate, Vector3 axis, float angle)
        {
            Vector3 origin = new Vector3(lookPosition.x, toRotate.y, lookPosition.z);
            Vector3 dir = toRotate - origin;
            dir = Quaternion.AngleAxis(angle, axis) * dir;
            Vector3 result = dir + origin;
            return result;
        }

        private Vector3 EvaluateArray(float y)
        {
            // t should be between 0 and 1
            y = Mathf.Clamp(y, 0f, 1f);
            int position = Mathf.RoundToInt(((float)(arc.Length - 1)) * y);
            return arc[position];
        }

        private Vector3 GetOperatorPosition(float x, float y)
        {
            // Get Position on the arc
            Vector3 pos = EvaluateArray(y);
            pos *= (zoom + encounterZoom);
            // Translate it to world pos
            pos += lookPosition - new Vector3(0, baseOffset.y, 0);
            // rotate it to angle
            pos = RotateVector(pos, Vector3.up, x);
            return pos;// RotateVectorAroundAxis(pos, Vector3.up, x);
        }

        private void UpdateMaterialsOpacity(float x, float y)
        {
            float r = x / y < 0.75 ? x / y : 1;

            Color curCol = new Color();
            for (int i = 0; i < _mats.Length; ++i)
            {
                curCol = _mats[i].color;
                curCol.a = r;
                _mats[i].color = curCol;
            }
        }

        [Button("TestMoveTo")]
        public void EaseTo(float x, float y)
        {
            isEasingTo = false;
            StopAllCoroutines();
            StartCoroutine(EaseToPosition(x, y));
        }

        private IEnumerator EaseToPosition(float Xangle, float YAngle)
        {
            Vector2 targetCamPos = new Vector2(Xangle, YAngle);
            Vector2 flatAngle = new Vector2(angles.x, angles.y);

            while ((flatAngle - targetCamPos).magnitude > .1f)
            {
                isEasingTo = true;

                easeInput = Vector2.SmoothDamp(easeInput, targetCamPos, ref easeInputDamp, easeSmoothness);
                flatAngle = easeInput;
                angles.x = flatAngle.x;
                angles.y = flatAngle.y;

                yield return null;
            }

            isEasingTo = false;
        }

        #endregion

        #region Input Handling

        public void OnInputDown(InputAction inputAction)
        {
            if (inputAction.actionType == ActionType.Zoom)
                StartAim();
        }

        public void OnInputUp(InputAction inputAction)
        {
            if (inputAction.actionType == ActionType.Zoom)
                EndAim();
        }

        #endregion

        /* public void StartEncounterCameraHandling()
        {
            _encounterHandlingRoutine = StartCoroutine(EncouterCameraRoutine());
        }

        public void StopEncounterCameraHandling()
        {
            StopCoroutine(_encounterHandlingRoutine);
        }

        IEnumerator EncouterCameraRoutine()
        {
            while (true)
            {
                encounterRefreshTimer += Time.deltaTime;
                if (encounterRefreshTimer >= encounterRefreshRate)
                {
                    List<Vector3> ennemies = EncounterManager.Instance.CurrentEncounter.GetEnemiesPosition();
                    ennemies.Sort((a, b) => Vector3.Distance(a, _follow.position).CompareTo(Vector3.Distance(b, _follow.position)));

                    float ratio = Vector3.Distance(ennemies[0], _follow.position) / 10f;
                    ratio = Mathf.Clamp(ratio, 0f, 1f);
                    encounterZoom = encounterZoomCurve.Evaluate(ratio) * encounterZoomRatio;
                    encounterZoom = Mathf.Clamp(encounterZoom, 0f, encounterMaxZoom);
                    encounterRefreshTimer = 0;
                }

                yield return null;
            }
        }*/

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_follow != null)
            {
                Gizmos.DrawWireSphere(_camera.transform.position, collisionHeadLockDistance);
                Handles.DrawWireDisc(_follow.position + new Vector3(0, baseOffset.y, 0), new Vector3(0, baseOffset.y, 0), baseOffset.x);
                Vector3[] arcArray = new Vector3[3]
                {
                  new Vector3(0, lowOrbitAltitude, -lowOrbitRadius),
                  new Vector3(0, midOrbitAltitude, -midOrbitRadius),
                  new Vector3(0, heightOrbitAltitude, -heightOrbitRadius),
                };
                arc = Vector3Utils.MakeSmoothCurve(arcArray, arcSmoothness);


                Handles.DrawPolyLine(arc);
            }
        }


#endif
    }
}
