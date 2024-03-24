using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding
{
    public static class BoundingBoxChecker
    {
        public static bool IsPositionInsideVolume(float3 position, NavigationVolume targetVolume)
        {
            BoxCollider volumeCollider = targetVolume.GetComponent<BoxCollider>();

            return
                position.x >= volumeCollider.bounds.min.x &&
                position.x <= volumeCollider.bounds.max.x &&
                position.y >= volumeCollider.bounds.min.y &&
                position.y <= volumeCollider.bounds.max.y &&
                position.z >= volumeCollider.bounds.min.z &&
                position.z <= volumeCollider.bounds.max.z;
        }
    }
}

