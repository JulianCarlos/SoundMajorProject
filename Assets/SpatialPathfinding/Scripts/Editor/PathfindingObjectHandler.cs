using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathfindingObjectHandler : Editor
{
    [MenuItem("GameObject/Pathfinding/Pathfinding Manager", false, 1)]
    static void SpawnNavigationPathfinder(MenuCommand menuCommand)
    {
        // Load your custom prefab
        GameObject prefab = Resources.Load<GameObject>("PathingManager");

        // Check if the prefab is loaded
        if (prefab != null)
        {
            // Create an instance of the prefab
            GameObject instance = Instantiate(prefab);

            instance.name = prefab.name;

            // Ensure it's not nested under any other GameObject in the scene
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);

            // Register the creation in the Undo system
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Custom Prefab");
            Selection.activeObject = instance;
        }
        else
        {
            Debug.LogError("Prefab not found at specified path.");
        }
    }

    [MenuItem("GameObject/Pathfinding/Navigation Volume", false, 1)]
    static void SpawnNavigationVolume(MenuCommand menuCommand)
    {
        // Load your custom prefab
        GameObject prefab = Resources.Load<GameObject>("NavigationVolume");

        // Check if the prefab is loaded
        if (prefab != null)
        {
            // Create an instance of the prefab
            GameObject instance = Instantiate(prefab);

            instance.name = prefab.name;

            // Ensure it's not nested under any other GameObject in the scene
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);

            // Register the creation in the Undo system
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Custom Prefab");
            Selection.activeObject = instance;
        }
        else
        {
            Debug.LogError("Prefab not found at specified path.");
        }
    }
}
