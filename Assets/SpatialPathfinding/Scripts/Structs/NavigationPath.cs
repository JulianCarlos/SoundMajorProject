using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;

public struct NavigationPath
{
    public Vector3[] Waypoints;
    public Vector3 StartingPoint => Waypoints[0];
    public Vector3 EndingPoint => Waypoints[Waypoints.Length - 1];

    public NavigationPath(Vector3[] waypoints)
    {
        Waypoints = waypoints;
    }

}
