using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NavigationPath
{
    public Vector3 StartingPoint { get; private set; }
    public Vector3 EndingPoint { get; private set; }
    public Vector3[] Waypoints { get; private set; }

    public NavigationPath(Vector3 startingPoint, Vector3 endingPoint, Vector3[] waypoints)
    {
        StartingPoint = startingPoint;
        EndingPoint = endingPoint;
        Waypoints = waypoints;
    }
}
