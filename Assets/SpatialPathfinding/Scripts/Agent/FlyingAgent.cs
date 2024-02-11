using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class FlyingAgent : MonoBehaviour
{
    [SerializeField] private float Speed = 5f;
    [SerializeField] private Vector3 targetPos;

    private Vector3[] waypoints;
    private int currentWayPoint = 0;

    private NavigationVolume activeVolume;

    private void Start()
    {
        MoveTo(targetPos);
    }

    public void UpdateActiveVolume(NavigationVolume activeVolume)
    {
        this.activeVolume = activeVolume;
    }

    public void MoveTo(Vector3 targetPos)
    {
        waypoints = PathingManager.Instance.AStar(transform.position, targetPos);

        StartCoroutine(C_MoveTo());
    }

    private IEnumerator C_MoveTo()
    {
        while (currentWayPoint < waypoints.Length)
        {
            if(Vector3.Distance(transform.position, waypoints[currentWayPoint]) <= 0.01f)
            {
                currentWayPoint++;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWayPoint], Speed * Time.deltaTime);
            }
            yield return null;
        }
    }
}
