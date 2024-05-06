using Codice.Client.BaseCommands.BranchExplorer;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding
{
    public class NavigationObstacle : MonoBehaviour
    {
        public Bounds ObstacleBounds => bounds;

        [SerializeField, Min(0)] private float padding = 0f;
        [SerializeField] private bool showBounds = false;

        private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 2);
        private ObstacleShape shape = ObstacleShape.Box;
        private Collider localCollider;

        private void Awake()
        {
            localCollider = GetComponent<Collider>();
        }

        public Vector3 GetMinCorner()
        {
            return transform.position - bounds.extents / 2;
        }

        public Vector3 GetMaxCorner()
        {
            return transform.position + bounds.extents / 2;
        }

        private void OnValidate()
        {
            localCollider = GetComponent<Collider>();

            CalculateBounds();
        }

        private void FixedUpdate()
        {
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            bounds.extents = (2 * localCollider.bounds.extents) + (Vector3.one * padding);
        }

        private void OnDrawGizmos()
        {
            if (!showBounds)
                return;

            switch (shape)
            {
                case ObstacleShape.Box:
                    Gizmos.DrawWireCube(transform.position + bounds.center, bounds.extents);
                    break;
            }
        }
    }
}

