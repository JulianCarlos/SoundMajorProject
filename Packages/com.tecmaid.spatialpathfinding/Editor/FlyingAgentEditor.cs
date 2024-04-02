using UnityEditor;
using UnityEngine.UIElements;

namespace Pathfinding
{
    [CustomEditor(typeof(FlyingAgent))]
    public class FlyingAgentEditor : Editor
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

