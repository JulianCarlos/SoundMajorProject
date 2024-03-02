using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;

public struct NavigationPath
{
    public NativeArray<Vector3> Waypoints;
    public Vector3 StartingPoint => Waypoints[0];
    public Vector3 EndingPoint => Waypoints[Waypoints.Length - 1];

    public NavigationPath(NativeList<Vector3> waypoints)
    {
        Waypoints = new NativeArray<Vector3>(waypoints.Length, Allocator.Persistent);
        Waypoints.CopyFrom(waypoints.AsArray());
    }
}
