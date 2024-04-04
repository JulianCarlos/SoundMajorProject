using Unity.Collections;
using UnityEngine;
using System.Diagnostics;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using Pathfinding.Helpers;

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public class PathingManager : MonoBehaviour
    {
        public static PathingManager Instance { get; private set; }
        public static Action<FlyingAgent> OnAgentStartedPathing { get; private set; }   
        public static Action<FlyingAgent> OnAgentFinishedPathing { get; private set; }

        public string AgentLayerName => agentLayerName;
        public string VolumeLayerName => volumeLayerName;
        public LayerMask DetectableLayer => detectableLayer;

        [SerializeField] private List<FlyingAgent> movableAgents = new List<FlyingAgent>();
        [SerializeField] private List<FlyingAgent> calculableAgents = new List<FlyingAgent>();
        
        [SerializeField] private Modifiers modifiers = Modifiers.NONE;

        [SerializeField] private string agentLayerName = "FlyingAgent";
        [SerializeField] private string volumeLayerName = "NavigationVolume";
        [SerializeField] private LayerMask detectableLayer = ~0;

        [SerializeField] private double movableExecutionTime = 0;
        [SerializeField] private double calculateExecutionTime = 0;

        private Stopwatch moveExecutionStopwatch = new Stopwatch();
        private Stopwatch calculateExecutionStopwatch = new Stopwatch();

        private NativeList<float3> wayPoints = new NativeList<float3>(Allocator.Persistent);

        private AStarJob targetJob;
        private AStarJob originJob;
        private JobHandle aStarHandle;

        private NavigationVolume originVolume;
        private NavigationVolume targetVolume;

        private void Awake()
        {
            CreateInstance();

            OnAgentStartedPathing += AddAgentToCalculation;
            OnAgentFinishedPathing += RemoveAgentFromMovable;
        }

        private void Update()
        {
            CalculateAllAgentPaths();
            MoveAllAgents();
        }

        private void CreateInstance()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private void AddAgentToCalculation(FlyingAgent agent)
        {
            calculableAgents.Add(agent);
        }

        private void RemoveAgentFromMovable(FlyingAgent agent)
        {
            movableAgents.Remove(agent);
        }

        private void CalculateAllAgentPaths()
        {
            if (calculableAgents.Count <= 0)
                return;

            calculateExecutionStopwatch.Start();

            for (int i = 0; i < calculableAgents.Count; i++)
            {
                CalculateAStarPath(calculableAgents[i]);

                if (!movableAgents.Contains(calculableAgents[i]))
                {
                    movableAgents.Add(calculableAgents[i]);
                    calculableAgents.Remove(calculableAgents[i]);
                }
            }

            calculateExecutionStopwatch.Stop();
            calculateExecutionTime = calculateExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
            calculateExecutionStopwatch.Reset();
        }

        private void MoveAllAgents()
        {
            if (movableAgents.Count <= 0)
                return;

            moveExecutionStopwatch.Start();

            for (int i = 0; i < movableAgents.Count; i++)
            {
                movableAgents[i].Move();
            }

            moveExecutionStopwatch.Stop();
            movableExecutionTime = moveExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
            moveExecutionStopwatch.Reset();
        }

        private void CalculateAStarPath(FlyingAgent agent)
        {
            originVolume = agent.ActiveVolume;
            targetVolume = agent.ActiveVolume;

            int tempLinkIndex = -1;
            float tempDistance = float.MaxValue;
            float distance = CalculationHelper.CalculateSquaredDistance(originVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);
            List<NavigationSubLink> links = agent.ActiveVolume.Links;

            CalculateClosestVolumes(agent, ref tempLinkIndex, ref tempDistance, ref distance, links);
            GenerateWaypoints(agent, tempLinkIndex, links);

            if (this.modifiers == Modifiers.PATHSMOOTHING)
            {
                wayPoints = SmoothPath(wayPoints, originVolume.GetDetectionRadius());
            }

            agent.SetPath(new NavigationPath(wayPoints));
            wayPoints.Clear();
        }

        private NativeList<float3> SmoothPath(NativeList<float3> waypoints, float detectionRadius)
        {
            int index = 0;
            NativeList<float3> newWaypoints = new NativeList<float3>(Allocator.Persistent);
            newWaypoints.Add(waypoints[index]);

            for (int i = 1; i < waypoints.Length - 1; i++)
            {
                if (Physics.BoxCast(waypoints[index], Vector3.one * detectionRadius, waypoints[i] - waypoints[index], Quaternion.identity, math.distance(waypoints[i], waypoints[index]))) 
                {
                    newWaypoints.Add(waypoints[i - 1]);
                    index = i - 1;
                }
            }

            newWaypoints.Add(waypoints[^1]);

            return newWaypoints;
        }

        private void CalculateClosestVolumes(FlyingAgent agent, ref int tempLinkIndex, ref float tempDistance, ref float distance, List<NavigationSubLink> links)
        {
            for (int i = 0; i < links.Count; i++)
            {
                tempDistance = CalculationHelper.CalculateSquaredDistance(links[i].LinkedVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);

                if (links[i].RootLink.CheckTraverseAccess(links[i]) && tempDistance < distance)
                {
                    distance = tempDistance;

                    targetVolume = links[i].LinkedVolume;
                    tempLinkIndex = i;
                }
            }
        }

        private void GenerateWaypoints(FlyingAgent agent, int tempLinkIndex, List<NavigationSubLink> links)
        {
            if (originVolume == targetVolume)
            {
                targetJob = JobFactory.GenerateAStarJob(originVolume, agent.InitialPos, agent.TargetPos, this.wayPoints);
                aStarHandle = targetJob.Schedule();
                aStarHandle.Complete();
                targetJob.TempData.Dispose();
                targetJob.OpenCells.Dispose();
            }
            else
            {
                targetJob = JobFactory.GenerateAStarJob(targetVolume, links[tempLinkIndex].NeighborLink.transform.position, agent.TargetPos, this.wayPoints);
                aStarHandle = targetJob.Schedule();
                aStarHandle.Complete();
                targetJob.TempData.Dispose();
                targetJob.OpenCells.Dispose();

                originVolume = agent.ActiveVolume;
                originJob = JobFactory.GenerateAStarJob(originVolume, agent.InitialPos, links[tempLinkIndex].transform.position, this.wayPoints);
                aStarHandle = originJob.Schedule();
                aStarHandle.Complete();
                originJob.TempData.Dispose();
                originJob.OpenCells.Dispose();
            }
        }
    }
}


