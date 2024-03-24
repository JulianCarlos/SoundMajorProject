using Mono.Cecil;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Collider))]
    public class FlyingAgent : MonoBehaviour
    {
        public NavigationVolume ActiveVolume { get; private set; }

        public Vector3 InitialPos => transform.position;
        public Vector3 TargetPos;

        [Space, Header("Starting Speed Settings")]
        [SerializeField] private AnimationCurve startSpeedCurve = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,1));
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float timeToReachMaxSpeed = 3f;
        [SerializeField] private bool interpolateSpeedStart = false;

        [Space, Header("Stopping Speed Settings")]
        [SerializeField] private AnimationCurve stopSpeedCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private float decelerationDistance = 1f;
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private float distanceUntilWaypointReached = 1f;
        [SerializeField] private bool autoBreak = false;

        [Space, Header("Rotation Settings")]
        [SerializeField, Min(1)] private float rotationStrength = 10f;
        [SerializeField] private bool useSmoothRotation = false;

        [Space, Header("Path Settings")]
        [SerializeField] private bool showPath = false;

        private NavigationPath activePath;

        private int currentWayPointIndex = 0;
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
            activePath = calculatedPath;

            currentWayPointIndex = activePath.Waypoints.Length - 1;
        }

        public void MoveTo(float3 targetPos)
        {
            RequestPath(targetPos);
        }

        public void Move()
        {
            CheckWaypointPosition();
            ApplyRotationAndPosition();
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
            if (currentWayPointIndex <= 0)
            {
                if (math.distance(transform.position, activePath.Waypoints[0]) <= stoppingDistance)
                {
                    PathingManager.OnAgentFinishedPathing(this);
                    return;
                }
            }
            else if (math.distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= distanceUntilWaypointReached)
            {
                currentWayPointIndex--;
            }
        }

        private void ApplyRotationAndPosition()
        {
            if (interpolateSpeedStart)
            {
                speedCurveMultiplier = startSpeedCurve.Evaluate(currentAccelerationValue);
                currentAccelerationValue += Time.deltaTime / timeToReachMaxSpeed;
            }

            Vector3 targetDirection = (CalculationHelper.Float3ToVector3(activePath.Waypoints[currentWayPointIndex]) - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(targetDirection);

            if (useSmoothRotation)
            {
                transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position, transform.position + transform.forward, (speedCurveMultiplier * maxSpeed) * Time.deltaTime), Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationStrength));
            }
            else
            {
                transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position, activePath.Waypoints[currentWayPointIndex], maxSpeed * Time.deltaTime), lookRotation);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;

            if (showPath && activePath.Waypoints.Length > 0)
            {
                for (int i = 0; i < activePath.Waypoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(activePath.Waypoints[i], activePath.Waypoints[i + 1]);
                }
            }
        }
    }
}

