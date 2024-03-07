using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    public class FlyingAgent : MonoBehaviour
    {
        public NavigationVolume ActiveVolume { get; private set; }

        public Vector3 TargetPos;
        public Vector3 InitialPos => transform.position;

        [SerializeField] private float speed = 5f;
        [SerializeField] private NavigationPath activePath;
        [Space]
        [SerializeField] private bool showPath = false;

        private int currentWayPointIndex;

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
            if (currentWayPointIndex <= 0 && math.distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= 0.01f)
            {
                PathingManager.OnAgentFinishedPathing(this);
                return;
            }
            else if (math.distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= 0.01f)
            {
                currentWayPointIndex--;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, activePath.Waypoints[currentWayPointIndex], speed * Time.fixedDeltaTime);
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

