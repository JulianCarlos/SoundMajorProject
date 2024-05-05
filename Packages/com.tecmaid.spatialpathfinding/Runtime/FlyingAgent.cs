using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Helpers;
using System;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Collider))]
    public class FlyingAgent : MonoBehaviour
    {
        public bool IsTraversing = false;

        public NavigationVolume ActiveVolume { get; private set; }

        public Vector3 InitialPos => transform.position;
        public Vector3 TargetPos;

        [SerializeField] private AnimationCurve startSpeedCurve = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,1));
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float timeToReachMaxSpeed = 3f;
        [SerializeField] private bool interpolateSpeedStart = false;

        [SerializeField] private AnimationCurve stopSpeedCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private float decelerationDistance = 1f;
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private float distanceUntilWaypointReached = 1f;
        [SerializeField] private bool autoBreak = false;

        [SerializeField, Min(1)] private float rotationStrength = 10f;
        [SerializeField] private bool useSmoothRotation = false;

        [SerializeField] private bool showPath = false;

        private NavigationPath activePath;

        private int currentWayPointIndex = 0;
        private int currentSegmentIndex = 0;

        private float speedCurveMultiplier = 1f;
        private float currentAccelerationValue = 0f;

        private void Start()
        {
            MoveTo(TargetPos);
        }

        public void SetActiveVolume(NavigationVolume activeVolume)
        {
            this.ActiveVolume = activeVolume;
        }

        public void SetPath(NavigationPath calculatedPath)
        {
            if (!IsTraversing && calculatedPath.Waypoints.Length > 0)
            {
                activePath = calculatedPath;
                currentSegmentIndex = activePath.Waypoints.Length - 1;
                currentWayPointIndex = activePath.Waypoints[currentSegmentIndex].Waypoints.Length - 1;
            }
        }

        public void MoveTo(float3 targetPos)
        {
            RequestPath(targetPos);
        }

        public void Move()
        {
            if (activePath.Waypoints.Length <= 0)
                return;

            CheckWaypointPosition();
            ApplyPositionAndRotation();
        }

        private void RequestPath(float3 targetPos)
        {
            if (PathingManager.Instance == null)
            {
                Debug.LogWarning($"You need a Pathingmanager in the Scene in order to Calculate Paths");
                return;
            }

            if (ActiveVolume == null)
            {
                Debug.LogWarning($"{this.gameObject} is not inside a Navigation Volume");
                return;
            }

            PathingManager.OnAgentStartedPathing(this);
        }

        private void CheckWaypointPosition()
        {
            if (currentWayPointIndex <= 0 && !IsTraversing && math.distance(transform.position, activePath.Waypoints[currentSegmentIndex].Waypoints[currentWayPointIndex]) <= stoppingDistance)
            {
                if (currentSegmentIndex <= 0 && currentWayPointIndex <= 0)
                {
                    PathingManager.OnAgentFinishedPathing(this);
                    return;
                }
                else
                {
                    IsTraversing = true;
                    currentSegmentIndex--;
                    currentWayPointIndex = activePath.Waypoints[currentSegmentIndex].Waypoints.Length - 1;
                }
            }
            else if (math.distance(transform.position, activePath.Waypoints[currentSegmentIndex].Waypoints[currentWayPointIndex]) <= distanceUntilWaypointReached)
            {
                IsTraversing = false;
                currentWayPointIndex--;
            }
        }

        private void ApplyPositionAndRotation()
        {
            if (interpolateSpeedStart)
            {
                speedCurveMultiplier = startSpeedCurve.Evaluate(currentAccelerationValue);
                currentAccelerationValue += Time.deltaTime / timeToReachMaxSpeed;
            }

            ApplyPosition();

            if (!IsTraversing && currentWayPointIndex > 0)
            {
                Vector3 targetDirection = (CalculationHelper.Float3ToVector3(activePath.Waypoints[currentSegmentIndex].Waypoints[currentWayPointIndex]) - transform.position).normalized;
                
                if (targetDirection != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(targetDirection);

                    ApplyRotation(lookRotation);
                }
            }
        }

        private void ApplyPosition()
        {
            if (useSmoothRotation)
            {
                transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, (speedCurveMultiplier * maxSpeed) * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, activePath.Waypoints[currentSegmentIndex].Waypoints[currentWayPointIndex], maxSpeed * Time.deltaTime);
            }
        }

        private void ApplyRotation(Quaternion lookRotation)
        {
            if (useSmoothRotation)
            {
                if (lookRotation != null)
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationStrength);
            }
            else
            {
                if (lookRotation != null)
                    transform.rotation = lookRotation;
            }
        }

        public void CancelPath()
        {
            PathingManager.OnAgentFinishedPathing(this);
            activePath = new NavigationPath();
        }

        private void OnDrawGizmos()
        {
            if (showPath && activePath.Waypoints.Length > 0)
            {
                for (int i = 0; i < activePath.Waypoints.Length; i++)
                {
                    for (int j = 0; j < activePath.Waypoints[i].Waypoints.Length - 1; j++)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(activePath.Waypoints[i].Waypoints[j], activePath.Waypoints[i].Waypoints[j + 1]);

                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(activePath.Waypoints[i].Waypoints[j], 1f);
                    }
                }
            }
        }
    }
}

