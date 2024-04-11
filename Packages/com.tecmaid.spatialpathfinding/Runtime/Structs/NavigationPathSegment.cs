using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public struct NavigationPathSegment
{
    public UnsafeList<float3> Waypoints;

    public NavigationPathSegment(NativeList<float3> waypoints)
    {
        Waypoints = new UnsafeList<float3>(waypoints.Length, Allocator.Persistent);

        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoints.Add(waypoints[i]);
        }
    }
}
