using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pathfinding
{
    [CustomEditor(typeof(PathingManager))]
    public class PathingManagerEditor : Editor
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            VisualTree.CloneTree(root);

            return root;
        }
    }
}


