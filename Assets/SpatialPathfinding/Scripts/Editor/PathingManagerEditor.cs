using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;

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
