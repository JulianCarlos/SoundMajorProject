using Unity.Collections;
using Unity.Mathematics;

public struct NavigationPath
{
    public NativeArray<float3> Waypoints;
    public float3 StartingPoint => Waypoints[0];
    public float3 EndingPoint => Waypoints[^1];

    public NavigationPath(NativeList<float3> waypoints)
    {
        Waypoints = new NativeArray<float3>(waypoints.Length, Allocator.Persistent);

        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoints[i] = waypoints[i];
        }
    }
}
