using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct PathSmoothingJob : IJob
{
    public NativeList<float3> WayPoints;

    int currentIndex;

    public void Execute()
    {
        currentIndex = 0;
    }
}
