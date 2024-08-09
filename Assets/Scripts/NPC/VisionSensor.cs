using UnityEngine;
using UnityEditor;
using FPSDemo.Core;
using FPSDemo.Player;
using FPSDemo.Target;

namespace FPSDemo.NPC.Sensors
{
    public class VisionSensor : MonoBehaviour, ISensor
    {
        // TODO: Add tooltips to all serialized fields

        [SerializeField] HumanTarget thisTarget;
        DetectionDirectionUpdater detectionDirectionUpdater;

        [SerializeField] LayerMask raycastMask = 1 | (1 << 2);

        [Header("Detection time")] [SerializeField]
        float timeToNotice = 1f;

        [Header("Vertical vision angles")] [SerializeField]
        float visionVertFocusAngleUp = 25f;

        [SerializeField] float visionVertFocusAngleDown = 30f;
        [SerializeField] float visionVertUpperAngle = 25f;
        [SerializeField] float visionVertLowerAngle = -70f;

        [Header("Vertical distances")] [SerializeField]
        float visionVertClose = 3f;

        [Header("Vertical multipliers")] [SerializeField]
        float multFocusVertical = 1f;

        [SerializeField] float multVertUpper = 0.1f;
        [SerializeField] float multVertLower = 0.2f;

        [Header("Horizontal distances")] [SerializeField]
        float visionFocusFar = 60f;

        [SerializeField] float visionFocusDistance = 20f;
        [SerializeField] float visionPeripheralNearDistance = 10f;
        [SerializeField] float visionPeripheralRearDistance = 2f;

        [Header("Horizontal angles")] [SerializeField]
        float visionFocusAngle = 60f;

        [SerializeField] float visionPeripheralNearAngle = 80f;
        [SerializeField] float visionPeripheralMidAngle = 90f;
        [SerializeField] float visionPeripheralRearAngle = 20f;

        [Header("Horizontal multipliers")] [SerializeField]
        float multFocusFar = 0.1f;

        [SerializeField] float multFocusClose = 1f;
        [SerializeField] float multPeripheralClose = 0.8f;
        [SerializeField] float multPeripheralFar = 0.2f;

        float farVisionRatio = 0f;
        float peripheralNearVisionRatio = 0f;
        float peripheralMidVisionRatio = 0f;
        float overallHorizontalAngle = 0f;

        float verticalTopRatio = 0f;
        float verticalBotRatio = 0f;

        public float TickRate => Game.AISettings != null ? Game.AISettings.VisionSensorTickRate : 0f;
        public float NextTickTime { get; set; }

        public void DeathBehavior()
        {
            DeregisterDetectionGUI();
            enabled = false;
        }

        public void DeregisterDetectionGUI()
        {
            detectionDirectionUpdater.UnregisterNewTargetWatching(gameObject);
        }

        void Start()
        {
            // Values for vision cone
            overallHorizontalAngle = visionPeripheralMidAngle + visionPeripheralRearAngle;
            farVisionRatio = visionFocusFar - visionFocusDistance;
            peripheralNearVisionRatio = visionFocusDistance - visionPeripheralNearDistance;
            verticalTopRatio = visionVertUpperAngle - visionVertFocusAngleUp;
            verticalBotRatio = visionVertLowerAngle + visionVertFocusAngleDown;

            detectionDirectionUpdater = GameObject.FindGameObjectWithTag("DetectionCollisionUpdater")
                .GetComponent<DetectionDirectionUpdater>();
            CheckRangeValues();
        }

        void CheckRangeValues()
        {
            if (visionFocusFar < visionFocusDistance)
            {
                Debug.LogError("visionFocusFar is less than visionFocusClose on object " + gameObject.name);
            }

            if (visionFocusFar < visionFocusDistance)
            {
                Debug.LogError("visionFocusFar is less than visionFocusNearPeripheralBorder on object " +
                               gameObject.name);
            }

            if (visionFocusDistance < visionPeripheralNearDistance)
            {
                Debug.LogError("visionFocusNearPeripheralBorder is less than visionPeripheralNearMidBorder on object " +
                               gameObject.name);
            }

            if (multFocusClose < multFocusFar)
            {
                Debug.LogError("multFocusClose is less than multFocusFar on object " + gameObject.name);
            }

            if (multPeripheralClose < multPeripheralFar)
            {
                Debug.LogError("multPeripheralClose is less than multPeripheralFar on object " + gameObject.name);
            }
        }

        public void Tick(AIContext context)
        {
            // Enemy targets raycast
            foreach(var kvp in context.enemiesSpecificData)
            {
                var currentTarget = kvp.Key;
                var currentTargetData = kvp.Value;
                currentTargetData.visibleBodyParts.Clear();

                var visionModifier = GetVisionModifier(currentTarget, currentTargetData);
                var awarenessChangeThisTick = visionModifier / timeToNotice * Time.deltaTime;

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
                        detectionDirectionUpdater.RegisterNewTargetWatching(gameObject);
                        detectionDirectionUpdater.UpdateGUIFill(gameObject,
                            currentTargetData.awarenessOfThisTarget / context.AlertAwarenessThreshold);
                    }
                }
            }
        }

        void OnEnable()
        {
            //playerKilled += DeregisterDetectionGUI;
        }

        void OnDisable()
        {
            if (detectionDirectionUpdater != null)
            {
                detectionDirectionUpdater.UnregisterNewTargetWatching(gameObject);
            }
            //playerKilled -= DeregisterDetectionGUI;
        }

        float GetVisionModifier(HumanTarget target, TargetData targetData)
        {
            var directionToTarget = target.eyes.position - thisTarget.eyes.position;
            var horizontalEyeDir = Vector3.ProjectOnPlane(thisTarget.eyes.forward, thisTarget.eyes.up);
            var horizontalDirToTarget = Vector3.ProjectOnPlane(directionToTarget, thisTarget.eyes.up);
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

        float GetRaycastModifier(HumanTarget targetToRayCast, TargetData targetData)
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

        float CalculateRayCastModifierForBodyPart(VisibleBodyPart bodyPart)
        {
            var origin = thisTarget.eyes.transform.position;
            var direction = (bodyPart.transform.position - origin).normalized;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, visionFocusFar, raycastMask)
                && hit.transform.TryGetComponent(out VisibleBodyPart bodyPartHit) &&
                bodyPartHit.owner == bodyPart.owner)
            {
                return 1f;
            }

            return 0f;
        }

        float GetHorizontalVisionConeModifier(float distanceToTarget, float horizontalAngle, HumanTarget target)
        {
            if (horizontalAngle > overallHorizontalAngle)
            {
                return 0f;
            }

            var horizontalModifier = 0f;
            if (distanceToTarget > visionFocusFar)
            {
                return 0f;
            }

            // Is it in binocular vision of the agent?
            if (horizontalAngle < visionFocusAngle)
            {
                // It is in focused area?
                if (distanceToTarget < visionFocusDistance)
                {
                    horizontalModifier = multFocusClose;
                }
                else if (distanceToTarget < visionFocusFar)
                {
                    if (!target.IsCrouching)
                    {
                        horizontalModifier = Mathf.Lerp(multFocusClose, multFocusFar,
                            (distanceToTarget - visionFocusDistance) / farVisionRatio);
                    }
                }
            }
            // Is it in near peripheral focus vision?
            else if (horizontalAngle < visionPeripheralNearAngle)
            {
                if (distanceToTarget < visionPeripheralNearDistance)
                {
                    horizontalModifier = multPeripheralClose;
                }
                else if (distanceToTarget < visionFocusDistance && !target.IsCrouching)
                {
                    horizontalModifier = Mathf.Lerp(multPeripheralClose, multPeripheralFar,
                        (distanceToTarget - visionPeripheralNearDistance) / peripheralNearVisionRatio);
                }
            }
            // Is it in mid peripheral vision?
            else if (horizontalAngle < visionPeripheralMidAngle)
            {
                if (distanceToTarget < visionPeripheralRearDistance)
                {
                    horizontalModifier = multPeripheralClose;
                }
                else if (distanceToTarget < visionPeripheralNearDistance)
                {
                    if (!target.IsCrouching)
                    {
                        horizontalModifier = Mathf.Lerp(multPeripheralClose, multPeripheralFar,
                            (distanceToTarget - visionPeripheralRearDistance) / peripheralMidVisionRatio);
                    }
                }
            }
            // It is has to be in rear peripheral vision
            else
            {
                if (distanceToTarget < visionPeripheralRearDistance)
                {
                    horizontalModifier = multPeripheralClose;
                }
            }

            return horizontalModifier;
        }

        float GetVerticalVisionConeModifier(float distanceToTarget, float verticalAngle, HumanTarget target)
        {
            // If the target is less than two meters away, he is seen
            if (distanceToTarget < 2.0f)
            {
                return 1f;
            }

            // If is outside the vertical view cone
            if (verticalAngle < visionVertLowerAngle || verticalAngle > visionVertUpperAngle)
            {
                return 0f;
            }

            var verticalModifier = 1f;
            // If it is outside of focus area upwards
            if (verticalAngle > 0 && verticalAngle > visionVertFocusAngleUp)
            {
                if (distanceToTarget > visionVertClose && target.IsCrouching)
                {
                    verticalModifier = 0f;
                }
                else
                {
                    verticalModifier = Mathf.Lerp(multVertUpper, multFocusVertical,
                        (visionVertUpperAngle - verticalAngle) / verticalTopRatio);
                }
            }
            // If it is outside of focus area downwards
            else if (Mathf.Abs(verticalAngle) > visionVertFocusAngleDown)
            {
                if (distanceToTarget > visionVertClose && target.IsCrouching)
                {
                    verticalModifier = 0f;
                }
                else
                {
                    verticalModifier = Mathf.Lerp(multVertLower, multFocusVertical,
                        (visionVertLowerAngle - verticalAngle) / verticalBotRatio);
                }
            }

            return verticalModifier;
        }

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
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionFocusAngle, 0) * t.thisTarget.eyes.forward, t.visionFocusAngle * 2,
                        t.visionFocusFar);

                    // Focus near
                    Handles.color = new Color(1f, 0f, 0f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionFocusAngle, 0) * t.thisTarget.eyes.forward, t.visionFocusAngle * 2,
                        t.visionFocusDistance);

                    // Near peripheral far
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralNearAngle, 0) * t.thisTarget.eyes.forward,
                        t.visionPeripheralNearAngle - t.visionFocusAngle, t.visionFocusDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralNearAngle, 0) * t.thisTarget.eyes.forward,
                        -t.visionPeripheralNearAngle + t.visionFocusAngle, t.visionFocusDistance);

                    // Near peripheral close
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralNearAngle, 0) * t.thisTarget.eyes.forward,
                        t.visionPeripheralNearAngle - t.visionFocusAngle, t.visionPeripheralNearDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralNearAngle, 0) * t.thisTarget.eyes.forward,
                        -t.visionPeripheralNearAngle + t.visionFocusAngle, t.visionPeripheralNearDistance);

                    // Mid peripheral far
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralMidAngle, 0) * t.thisTarget.eyes.forward,
                        t.visionPeripheralMidAngle - t.visionPeripheralNearAngle, t.visionPeripheralNearDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralMidAngle, 0) * t.thisTarget.eyes.forward,
                        -t.visionPeripheralMidAngle + t.visionPeripheralNearAngle, t.visionPeripheralNearDistance);

                    // Mid peripheral close
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralMidAngle, 0) * t.thisTarget.eyes.forward,
                        t.visionPeripheralMidAngle - t.visionPeripheralNearAngle, t.visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralMidAngle, 0) * t.thisTarget.eyes.forward,
                        -t.visionPeripheralMidAngle + t.visionPeripheralNearAngle, t.visionPeripheralRearDistance);

                    // Peripheral rear
                    Handles.color = new Color(1f, 0.5f, 0.05f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralRearAngle - t.visionPeripheralMidAngle, 0) *
                        t.thisTarget.eyes.forward, t.visionPeripheralRearAngle, t.visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralRearAngle + t.visionPeripheralMidAngle, 0) *
                        t.thisTarget.eyes.forward, -t.visionPeripheralRearAngle, t.visionPeripheralRearDistance);
                    // For the same shade as the above
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, -t.visionPeripheralRearAngle - t.visionPeripheralMidAngle, 0) *
                        t.thisTarget.eyes.forward, t.visionPeripheralRearAngle, t.visionPeripheralRearDistance);
                    Handles.DrawSolidArc(t.thisTarget.eyes.position, Vector3.up,
                        Quaternion.Euler(0, t.visionPeripheralRearAngle + t.visionPeripheralMidAngle, 0) *
                        t.thisTarget.eyes.forward, -t.visionPeripheralRearAngle, t.visionPeripheralRearDistance);
                }
            }
        }
#endif
    }
}