using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding
{
    public class NavigationObstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleShape shape = ObstacleShape.Box;
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 2);
        [SerializeField] private bool isDynamic = false;

        private void OnDrawGizmos()
        {
            switch (shape)
            {
                case ObstacleShape.Box:
                    Gizmos.DrawWireCube(transform.position + bounds.center, bounds.extents);
                    break;

                case ObstacleShape.Capsule:
                    break;

                case ObstacleShape.Sphere:
                    break;
                default:
                    break;
            }
        }
    }
}

