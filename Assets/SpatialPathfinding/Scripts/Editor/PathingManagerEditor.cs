using UnityEngine;
using UnityEditor;

namespace Pathfinding
{
    [CustomEditor(typeof(PathingManager))]
    public class PathingManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PathingManager manager = (PathingManager)target;

            if (GUILayout.Button("Generate Grids"))
            {

            }
        }
    }
}


