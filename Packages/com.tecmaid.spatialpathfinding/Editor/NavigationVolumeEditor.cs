using UnityEditor;
using UnityEngine.UIElements;

namespace Pathfinding
{
    [CustomEditor(typeof(NavigationVolume))]
    public class NavigationVolumeEditor : Editor
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

