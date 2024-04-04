using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Pathfinding.Helpers;
using UnityEngine.PlayerLoop;
using System;
using UnityEditor.UIElements;

namespace Pathfinding
{
    [CustomEditor(typeof(PathingManager))]
    public class PathingManagerEditor : Editor
    {
        public VisualTreeAsset VisualTree;
        public PathingManager Manager;

        public Button createLayersButton;
        public Button applyLayersButton;

        public VisualElement CurrentCachedAgentLayer;
        public VisualElement CurrentCachedVolumeLayer;

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

            CurrentCachedAgentLayer = root.Q<VisualElement>("AgentCachedLayer");
            CurrentCachedVolumeLayer = root.Q<VisualElement>("VolumeCachedLayer");
            CurrentCachedAgentLayer.SetEnabled(false);
            CurrentCachedVolumeLayer.SetEnabled(false);

            return root;
        }

        public void GenerateLayers(ClickEvent evt)
        {
            TagFactory.CreateLayer(Manager.agentLayerInput);
            TagFactory.CreateLayer(Manager.volumeLayerInput);
        }

        public void ApplyLayersToObjects(ClickEvent evt)
        {
            if (TagFactory.LayerExists(Manager.agentLayerInput) && TagFactory.LayerExists(Manager.volumeLayerInput))
            {
                Manager.SetLayers();

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


