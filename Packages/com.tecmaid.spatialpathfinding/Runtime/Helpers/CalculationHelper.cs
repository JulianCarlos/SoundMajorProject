using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding.Helpers
{
    public static class CalculationHelper
    {
        public static float CalculateSquaredDistance(float3 point1, float3 point2)
        {
            float dx = point2.x - point1.x;
            float dy = point2.y - point1.y;
            float dz = point2.z - point1.z;

            return dx * dx + dy * dy + dz * dz;
        }

        public static float3 Int3ToFloat3(int3 int3Direction)
        {
            return new float3(int3Direction.x, int3Direction.y, int3Direction.z);
        }

        public static Vector3 Float3ToVector3(float3 floatValue)
        {
            return new Vector3(floatValue.x, floatValue.y, floatValue.z);
        }

        public static int FlattenIndex(int3 index, int totalWidth, int totalHeight)
        {
            return index.x + totalWidth * (index.y + totalHeight * index.z);
        }

        public static bool CheckIfIndexValid(int3 index, int totalWidth, int totalHeight, int totalDepth)
        {
            return
                index.x >= 0 && index.x < totalWidth &&
                index.y >= 0 && index.y < totalHeight &&
                index.z >= 0 && index.z < totalDepth;
        }
    }
}

