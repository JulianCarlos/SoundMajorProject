using Mono.Cecil;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    public class FlyingAgent : MonoBehaviour
    {
        public NavigationVolume ActiveVolume { get; private set; }
        public Vector3 InitialPos => transform.position;
        public Vector3 TargetPos;

        [Space]
        [SerializeField] private AnimationCurve startSpeedCurve;
        [SerializeField] private AnimationCurve stopSpeedCurve;
        [Space]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float timeToReachMaxSpeed = 3f;
        [SerializeField] private bool interpolateSpeedStart = false;
        [Space]
        [SerializeField] private float rotationStrength = 10f;
        [SerializeField] private bool useSmoothRotation = false;
        [Space]
        [SerializeField] private float decelerationDistance = 1f;
        [Space]
        [SerializeField] private float stoppingDistance = 1f;
        [SerializeField] private bool autoBreak = false;
        [Space]
        [SerializeField] private NavigationPath activePath;
        [SerializeField] private bool showPath = false;

        private int currentWayPointIndex = 0;
        private float speedCurveMultiplier = 1f;
        private float currentAccelerationValue = 0f;

        private void Start()
        {
            MoveTo(TargetPos);
        }

        public void AddActiveVolume(NavigationVolume activeVolume)
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

        private void RequestPath(float3 targetPos)
        {
            if (ActiveVolume == null)
            {
                Debug.LogWarning($"{this.gameObject} is not inside a Navigation Volume");
                return;
            }

            PathingManager.OnAgentStartedPathing(this);
        }

        public void Move()
        {
            if (currentWayPointIndex <= 0 && math.distance(transform.position, activePath.Waypoints[0]) <= stoppingDistance)
            {
                PathingManager.OnAgentFinishedPathing(this);
                return;
            }
            else if (math.distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= stoppingDistance)
            {
                currentWayPointIndex--;
            }
            else
            {
                if (interpolateSpeedStart)
                {
                    speedCurveMultiplier = startSpeedCurve.Evaluate(currentAccelerationValue);
                    currentAccelerationValue += Time.deltaTime / timeToReachMaxSpeed;
                }
                
                if (useSmoothRotation)
                {
                    Vector3 targetDirection = (CalculationHelper.Float3ToVector3(activePath.Waypoints[currentWayPointIndex]) - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationStrength);

                    transform.position += transform.forward * (speedCurveMultiplier * maxSpeed) * Time.deltaTime;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, activePath.Waypoints[currentWayPointIndex], maxSpeed * Time.deltaTime);
                }
                
                Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.red);
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

