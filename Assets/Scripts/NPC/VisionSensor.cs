using UnityEngine;
using UnityEditor;
using FPSDemo.Core;
using FPSDemo.Player;
using FPSDemo.Target;

namespace FPSDemo.NPC.Sensors
{
    [RequireComponent(typeof(HumanTarget))]
    public class VisionSensor : MonoBehaviour, ISensor
    {
        // ========================================================= INSPECTOR FIELDS
        // TODO: Add tooltips to all serialized fields

        [SerializeField] private HumanTarget _thisTarget;
        [SerializeField] private LayerMask _raycastMask = 1 | (1 << 2);

        [Header("Detection time")] 
        [SerializeField] private float _timeToNotice = 1f;

        [Header("Vertical vision angles")] 
        [SerializeField] private float _visionVertFocusAngleUp = 25f;
        [SerializeField] private float _visionVertFocusAngleDown = 30f;
        [SerializeField] private float _visionVertUpperAngle = 25f;
        [SerializeField] private float _visionVertLowerAngle = -70f;

        [Header("Vertical distances")] 
        [SerializeField] private float _visionVertClose = 3f;

        [Header("Vertical multipliers")] 
        [SerializeField] private float _multFocusVertical = 1f;
        [SerializeField] private float _multVertUpper = 0.1f;
        [SerializeField] private float _multVertLower = 0.2f;

        [Header("Horizontal distances")] 
        [SerializeField] private float _visionFocusFar = 60f;
        [SerializeField] private float _visionFocusDistance = 20f;
        [SerializeField] private float _visionPeripheralNearDistance = 10f;
        [SerializeField] private float _visionPeripheralRearDistance = 2f;

        [Header("Horizontal angles")] 
        [SerializeField] private float _visionFocusAngle = 60f;
        [SerializeField] private float _visionPeripheralNearAngle = 80f;
        [SerializeField] private float _visionPeripheralMidAngle = 90f;
        [SerializeField] private float _visionPeripheralRearAngle = 20f;

        [Header("Horizontal multipliers")] 
        [SerializeField] private float _multFocusFar = 0.1f;
        [SerializeField] private float _multFocusClose = 1f;
        [SerializeField] private float _multPeripheralClose = 0.8f;
        [SerializeField] private float _multPeripheralFar = 0.2f;


        // ========================================================= PRIVATE FIELDS

        private float _farVisionRatio = 0f;
        private float _peripheralNearVisionRatio = 0f;
        private float _peripheralMidVisionRatio = 0f;
        private float _overallHorizontalAngle = 0f;

        private float _verticalTopRatio = 0f;
        private float _verticalBotRatio = 0f;

        private DetectionDirectionUpdater _detectionDirectionUpdater;


        // ========================================================= PROPERTIES

        public float TickRate => Game.AISettings != null ? Game.AISettings.VisionSensorTickRate : 0f;
        public float NextTickTime { get; set; }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_thisTarget == null)
            {
                _thisTarget = GetComponent<HumanTarget>();
            }
        }

        private void Start()
        {
            // Values for vision cone
            _overallHorizontalAngle = _visionPeripheralMidAngle + _visionPeripheralRearAngle;
            _farVisionRatio = _visionFocusFar - _visionFocusDistance;
            _peripheralNearVisionRatio = _visionFocusDistance - _visionPeripheralNearDistance;
            _verticalTopRatio = _visionVertUpperAngle - _visionVertFocusAngleUp;
            _verticalBotRatio = _visionVertLowerAngle + _visionVertFocusAngleDown;

            _detectionDirectionUpdater = GameObject.FindGameObjectWithTag("DetectionCollisionUpdater")
                .GetComponent<DetectionDirectionUpdater>();
            CheckRangeValues();
        }

        private void OnEnable()
        {
            //playerKilled += DeregisterDetectionGUI;
        }

        private void OnDisable()
        {
            if (_detectionDirectionUpdater != null)
            {
                _detectionDirectionUpdater.UnregisterNewTargetWatching(gameObject);
            }
            //playerKilled -= DeregisterDetectionGUI;
        }


        // ========================================================= VALIDATION METHODS

        private void CheckRangeValues()
        {
            if (_visionFocusFar < _visionFocusDistance)
            {
                Debug.LogError("visionFocusFar is less than visionFocusClose on object " + gameObject.name);
            }

            if (_visionFocusFar < _visionFocusDistance)
            {
                Debug.LogError("visionFocusFar is less than visionFocusNearPeripheralBorder on object " +
                               gameObject.name);
            }

            if (_visionFocusDistance < _visionPeripheralNearDistance)
            {
                Debug.LogError("visionFocusNearPeripheralBorder is less than visionPeripheralNearMidBorder on object " +
                               gameObject.name);
            }

            if (_multFocusClose < _multFocusFar)
            {
                Debug.LogError("multFocusClose is less than multFocusFar on object " + gameObject.name);
            }

            if (_multPeripheralClose < _multPeripheralFar)
            {
                Debug.LogError("multPeripheralClose is less than multPeripheralFar on object " + gameObject.name);
            }
        }


        // ========================================================= TICK

        public void Tick(AIContext context)
        {
            // Enemy targets raycast
            foreach(var kvp in context.enemiesSpecificData)
            {
                var currentTarget = kvp.Key;
                var currentTargetData = kvp.Value;
                currentTargetData.visibleBodyParts.Clear();

                var visionModifier = GetVisionModifier(currentTarget, currentTargetData);
                var awarenessChangeThisTick = visionModifier / _timeToNotice * Time.deltaTime;

                if (visionModifier == 0)
                {
                    context.AwarenessDecrease(currentTargetData);
                    if (currentTarget.IsPlayer)
                    {
                        DeregisterDetectionGUI();
                    }
                }
                else
                {
                    if (visionModifier >= 1 && currentTargetData.awarenessOfThisTarget < 1)
                    {
                        currentTargetData.awarenessOfThisTarget = 1f;
                    }

                    var newAwarenessOfTheTarget = currentTargetData.awarenessOfThisTarget + awarenessChangeThisTick;
                    context.SetAwarenessOfThisEnemy(currentTarget, newAwarenessOfTheTarget);

                    if (currentTarget.IsPlayer)
                    {
                        _detectionDirectionUpdater.RegisterNewTargetWatching(gameObject);
                        _detectionDirectionUpdater.UpdateGUIFill(gameObject,
                            currentTargetData.awarenessOfThisTarget / context.AlertAwarenessThreshold);
                    }
                }
            }
        }


        // ========================================================= ON DEATH

        public void DeathBehavior()
        {
            DeregisterDetectionGUI();
            enabled = false;
        }

        private void DeregisterDetectionGUI()
        {
            _detectionDirectionUpdater.UnregisterNewTargetWatching(gameObject);
        }


        // ========================================================= GETTERS

        private float GetVisionModifier(HumanTarget target, TargetData targetData)
        {
            var directionToTarget = target.eyes.position - _thisTarget.eyes.position;
            var horizontalEyeDir = Vector3.ProjectOnPlane(_thisTarget.eyes.forward, _thisTarget.eyes.up);
            var horizontalDirToTarget = Vector3.ProjectOnPlane(directionToTarget, _thisTarget.eyes.up);
            var distanceToTarget = horizontalDirToTarget.magnitude;

            var horizontalAngle = Vector3.Angle(horizontalEyeDir, horizontalDirToTarget);
            var horizontalModifier = GetHorizontalVisionConeModifier(distanceToTarget, horizontalAngle, target);
            if (horizontalModifier <= 0)
            {
                return 0f;
            }

            var verticalAngle = Vector3.Angle(directionToTarget, horizontalDirToTarget);
            var verticalModifier = GetVerticalVisionConeModifier(distanceToTarget, verticalAngle, target);
            if (verticalModifier <= 0)
            {
                return 0f;
            }

            var rayCastModifier = GetRaycastModifier(target, targetData);
            return horizontalModifier * verticalModifier * rayCastModifier;
        }

        private float GetRaycastModifier(HumanTarget targetToRayCast, TargetData targetData)
        {
            var overallRayCastModifier = 0f;
            foreach (var bodyPart in targetToRayCast.bodyPartsToRaycast)
            {
                var bodyPartVisionModifier = CalculateRayCastModifierForBodyPart(bodyPart);
                if (bodyPartVisionModifier > 0)
                {
                    targetData.visibleBodyParts.Add(bodyPart);
                    overallRayCastModifier =
                        Mathf.Min(overallRayCastModifier + (bodyPart.visibilityModifier * bodyPartVisionModifier), 100);
                }
            }

            return overallRayCastModifier / 100f;
        }

        private float GetHorizontalVisionConeModifier(float distanceToTarget, float horizontalAngle, HumanTarget target)
        {
            if (horizontalAngle > _overallHorizontalAngle)
            {
                return 0f;
            }

            var horizontalModifier = 0f;
            if (distanceToTarget > _visionFocusFar)
            {
                return 0f;
            }

            // Is it in binocular vision of the agent?
            if (horizontalAngle < _visionFocusAngle)
            {
                // It is in focused area?
                if (distanceToTarget < _visionFocusDistance)
                {
                    horizontalModifier = _multFocusClose;
                }
                else if (distanceToTarget < _visionFocusFar)
                {
                    if (!target.IsCrouching)
                    {
                        horizontalModifier = Mathf.Lerp(_multFocusClose, _multFocusFar,
                            (distanceToTarget - _visionFocusDistance) / _farVisionRatio);
                    }
                }
            }
            // Is it in near peripheral focus vision?
            else if (horizontalAngle < _visionPeripheralNearAngle)
            {
                if (distanceToTarget < _visionPeripheralNearDistance)
                {
                    horizontalModifier = _multPeripheralClose;
                }
                else if (distanceToTarget < _visionFocusDistance && !target.IsCrouching)
                {
                    horizontalModifier = Mathf.Lerp(_multPeripheralClose, _multPeripheralFar,
                        (distanceToTarget - _visionPeripheralNearDistance) / _peripheralNearVisionRatio);
                }
            }
            // Is it in mid peripheral vision?
            else if (horizontalAngle < _visionPeripheralMidAngle)
            {
                if (distanceToTarget < _visionPeripheralRearDistance)
                {
                    horizontalModifier = _multPeripheralClose;
                }
                else if (distanceToTarget < _visionPeripheralNearDistance)
                {
                    if (!target.IsCrouching)
                    {
                        horizontalModifier = Mathf.Lerp(_multPeripheralClose, _multPeripheralFar,
                            (distanceToTarget - _visionPeripheralRearDistance) / _peripheralMidVisionRatio);
                    }
                }
            }
            // It is has to be in rear peripheral vision
            else
            {
                if (distanceToTarget < _visionPeripheralRearDistance)
                {
                    horizontalModifier = _multPeripheralClose;
                }
            }

            return horizontalModifier;
        }

        private float GetVerticalVisionConeModifier(float distanceToTarget, float verticalAngle, HumanTarget target)
        {
            // If the target is less than two meters away, he is seen
            if (distanceToTarget < 2.0f)
            {
                return 1f;
            }

            // If is outside the vertical view cone
            if (verticalAngle < _visionVertLowerAngle || verticalAngle > _visionVertUpperAngle)
            {
                return 0f;
            }

            var verticalModifier = 1f;
            // If it is outside of focus area upwards
            if (verticalAngle > 0 && verticalAngle > _visionVertFocusAngleUp)
            {
                if (distanceToTarget > _visionVertClose && target.IsCrouching)
                {
                    verticalModifier = 0f;
                }
                else
                {
                    verticalModifier = Mathf.Lerp(_multVertUpper, _multFocusVertical,
                        (_visionVertUpperAngle - verticalAngle) / _verticalTopRatio);
                }
            }
            // If it is outside of focus area downwards
            else if (Mathf.Abs(verticalAngle) > _visionVertFocusAngleDown)
            {
                if (distanceToTarget > _visionVertClose && target.IsCrouching)
                {
                    verticalModifier = 0f;
                }
                else
                {
                    verticalModifier = Mathf.Lerp(_multVertLower, _multFocusVertical,
                        (_visionVertLowerAngle - verticalAngle) / _verticalBotRatio);
                }
            }

            return verticalModifier;
        }


        // ========================================================= CALCULATIONS

        private float CalculateRayCastModifierForBodyPart(VisibleBodyPart bodyPart)
        {
            var origin = _thisTarget.eyes.transform.position;
            var direction = (bodyPart.transform.position - origin).normalized;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, _visionFocusFar, _raycastMask)
                && hit.transform.TryGetComponent(out VisibleBodyPart bodyPartHit) &&
                bodyPartHit.owner == bodyPart.owner)
            {
                return 1f;
            }

            return 0f;
        }


        // ========================================================= EDITOR / DEBUG

#if UNITY_EDITOR
        [CustomEditor(typeof(VisionSensor))]
        public class ExampleEditor : Editor
        {
            public void OnSceneGUI()
            {
                var t = target as VisionSensor;
                if (t.enabled)
                {
                    // Focus far
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionFocusAngle, 0) * t._thisTarget.eyes.forward, t._visionFocusAngle * 2,
                        t._visionFocusFar);

                    // Focus near
                    Handles.color = new Color(1f, 0f, 0f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionFocusAngle, 0) * t._thisTarget.eyes.forward, t._visionFocusAngle * 2,
                        t._visionFocusDistance);

                    // Near peripheral far
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralNearAngle, 0) * t._thisTarget.eyes.forward,
                        t._visionPeripheralNearAngle - t._visionFocusAngle, t._visionFocusDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralNearAngle, 0) * t._thisTarget.eyes.forward,
                        -t._visionPeripheralNearAngle + t._visionFocusAngle, t._visionFocusDistance);

                    // Near peripheral close
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralNearAngle, 0) * t._thisTarget.eyes.forward,
                        t._visionPeripheralNearAngle - t._visionFocusAngle, t._visionPeripheralNearDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralNearAngle, 0) * t._thisTarget.eyes.forward,
                        -t._visionPeripheralNearAngle + t._visionFocusAngle, t._visionPeripheralNearDistance);

                    // Mid peripheral far
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralMidAngle, 0) * t._thisTarget.eyes.forward,
                        t._visionPeripheralMidAngle - t._visionPeripheralNearAngle, t._visionPeripheralNearDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralMidAngle, 0) * t._thisTarget.eyes.forward,
                        -t._visionPeripheralMidAngle + t._visionPeripheralNearAngle, t._visionPeripheralNearDistance);

                    // Mid peripheral close
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralMidAngle, 0) * t._thisTarget.eyes.forward,
                        t._visionPeripheralMidAngle - t._visionPeripheralNearAngle, t._visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralMidAngle, 0) * t._thisTarget.eyes.forward,
                        -t._visionPeripheralMidAngle + t._visionPeripheralNearAngle, t._visionPeripheralRearDistance);

                    // Peripheral rear
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralRearAngle - t._visionPeripheralMidAngle, 0) *
                        t._thisTarget.eyes.forward, t._visionPeripheralRearAngle, t._visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralRearAngle + t._visionPeripheralMidAngle, 0) *
                        t._thisTarget.eyes.forward, -t._visionPeripheralRearAngle, t._visionPeripheralRearDistance);
                    // For the same shade as the above
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t._visionPeripheralRearAngle - t._visionPeripheralMidAngle, 0) *
                        t._thisTarget.eyes.forward, t._visionPeripheralRearAngle, t._visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t._thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t._visionPeripheralRearAngle + t._visionPeripheralMidAngle, 0) *
                        t._thisTarget.eyes.forward, -t._visionPeripheralRearAngle, t._visionPeripheralRearDistance);
                }
            }
        }
#endif
    }
}