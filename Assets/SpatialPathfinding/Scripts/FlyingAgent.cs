using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(200)]
    public class FlyingAgent : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private Vector3 targetPos;

        private NavigationPath activePath;

        public NavigationVolume activeVolume;

        private void Start()
        {
            StartCoroutine(nameof(TestMethod));
        }

        IEnumerator TestMethod()
        {
            yield return new WaitForSeconds(2f);

            MoveTo(targetPos);
        }

        public void AddActiveVolume(NavigationVolume activeVolume)
        {
            this.activeVolume = activeVolume;
        }

        public void MoveTo(Transform transform)
        {
            MoveTo(transform.position);
        }

        public void MoveTo(Vector3 targetPos)
        {
            activePath = PathingManager.Instance.AStar(transform.position, targetPos, this.activeVolume);

            StartCoroutine(C_MoveTo());
        }

        private IEnumerator C_MoveTo()
        {
            int currentWayPointIndex = activePath.Waypoints.Length - 1; 

            while (currentWayPointIndex >= 0)
            {
                if (Vector3.Distance(transform.position, activePath.Waypoints[currentWayPointIndex]) <= 0.01f)
                {
                    currentWayPointIndex--;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, activePath.Waypoints[currentWayPointIndex], speed * Time.deltaTime);
                }
                yield return null;
            }
        }
    }
}

