using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    public class FlyingAgent : MonoBehaviour
    {
        public NavigationVolume ActiveVolume { get; private set; }

        [SerializeField] private float speed = 5f;
        public Vector3 targetPos;
        public Vector3 initialPos => transform.position;

        [SerializeField] private NavigationPath activePath;

        private int currentWayPointIndex;

        private void Start()
        {
            //StartCoroutine(nameof(TestMethod));

            MoveTo(targetPos);
        }

        IEnumerator TestMethod()
        {
            yield return new WaitForSeconds(0.2f);

            MoveTo(targetPos);
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

        public void MoveTo(Vector3 targetPos)
        {
            RequestPath(targetPos);
        }

        private void RequestPath(Vector3 targetPos)
        {
            if (ActiveVolume == null)
            {
                Debug.LogWarning($"{this.gameObject} is not inside a Navigation Volume");
                return;
            }

            //activePath = PathingManager.Instance.AStar(this, transform.position, targetPos, this.ActiveVolume);
            //currentWayPointIndex = activePath.Waypoints.Length - 1;

            PathingManager.OnAgentStartedPathing(this);
        }

        public void Move()
        {
            if (currentWayPointIndex <= 0)
                return;

            if (Vector3.Distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= 0.01f)
            {
                currentWayPointIndex--;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, activePath.Waypoints[currentWayPointIndex], speed * Time.fixedDeltaTime);
            }
        }
    }
}

