using Unity.Collections;
using Unity.Mathematics;

public struct NavigationPath
{
    public NativeArray<NavigationPathSegment> Waypoints;

    public NavigationPath(NativeList<NavigationPathSegment> waypoints)
    {
        Waypoints = new NativeArray<NavigationPathSegment>(waypoints.Length, Allocator.Persistent);

        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoints[i] = waypoints[i];
        }
    }
}
