using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Pathfinding.Helpers;
using UnityEngine.PlayerLoop;

namespace Pathfinding
{
    [CustomEditor(typeof(PathingManager))]
    public class PathingManagerEditor : Editor
    {
        public VisualTreeAsset VisualTree;
        public PathingManager Manager;

        public Button createLayersButton;
        public Button applyLayersButton;

        private void OnEnable()
        {
            Manager = (PathingManager)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            VisualTree.CloneTree(root);

            createLayersButton = root.Q<Button>("CreateLayersButton");
            createLayersButton.RegisterCallback<ClickEvent>(GenerateLayers);

            applyLayersButton = root.Q<Button>("ApplyLayersButton");
            applyLayersButton.RegisterCallback<ClickEvent>(ApplyLayersToObjects);

            return root;
        }

        public void GenerateLayers(ClickEvent evt)
        {
            TagFactory.CreateLayer(Manager.AgentLayerName);
            TagFactory.CreateLayer(Manager.VolumeLayerName);
        }

        public void ApplyLayersToObjects(ClickEvent evt)
        {
            if (TagFactory.LayerExists(Manager.AgentLayerName) && TagFactory.LayerExists(Manager.VolumeLayerName))
            {
                FlyingAgent[] agents = FindObjectsOfType<FlyingAgent>();
                for (int i = 0; i < agents.Length; i++)
                {
                    agents[i].gameObject.layer = LayerMask.NameToLayer(Manager.AgentLayerName);
                }

                NavigationVolume[] volumes = FindObjectsOfType<NavigationVolume>();
                for (int i = 0; i < volumes.Length; i++)
                {
                    volumes[i].gameObject.layer = LayerMask.NameToLayer(Manager.VolumeLayerName);
                }
            }
            else
            {
                Debug.LogWarning("Layers are not generated yet, make sure to first generate them");
            }
        }
    }
}


