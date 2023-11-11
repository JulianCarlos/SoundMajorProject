using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CalculationHelper
{
    public static float CalculateSquaredDistance(float3 point1, float3 point2)
    {
        float dx = point2.x - point1.x;
        float dy = point2.y - point1.y;
        float dz = point2.z - point1.z;

        return dx * dx + dy * dy + dz * dz;
    }

    public static float3 Int3ToVector3(int3 int3Direction)
    {
        return new float3(int3Direction.x, int3Direction.y, int3Direction.z);
    }
}
