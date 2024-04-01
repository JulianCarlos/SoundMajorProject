using Pathfinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(NavigationVolume))]
public class NavigationVolumeEditor : Editor
{
    public VisualTreeAsset VisualTree;

    public override VisualElement CreateInspectorGUI()
    {
        //return base.CreateInspectorGUI();

        VisualElement root = new VisualElement();

        VisualTree.CloneTree(root);

        return root;
    }
}
