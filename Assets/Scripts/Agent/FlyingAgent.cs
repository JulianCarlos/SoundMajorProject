using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingAgent : MonoBehaviour
{
    [SerializeField] private float Speed = 5f;
    [SerializeField] private Vector3 targetPos;

    [SerializeField] private Vector3[] waypoints;

    private int currentWayPoint = 0;

    private void Start()
    {
        MoveTo(targetPos);
    }

    public void MoveTo(Vector3 targetPos)
    {
        waypoints = PathingManager.Instance.AStar(transform.position, targetPos);

        //System.Array.Reverse(waypoints);

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
