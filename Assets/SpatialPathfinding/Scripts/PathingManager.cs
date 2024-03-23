using Unity.Collections;
using UnityEngine;
using System.Diagnostics;
using Unity.Jobs;
using System.Collections.Generic;
using System.Collections;
using System;
using Unity.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Entities.UniversalDelegates;

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public class PathingManager : MonoBehaviour
    {
        public static PathingManager Instance { get; private set; }
        public static Action<FlyingAgent> OnAgentStartedPathing { get; private set; }   
        public static Action<FlyingAgent> OnAgentFinishedPathing { get; private set; }

        [SerializeField] private List<FlyingAgent> movableAgents = new List<FlyingAgent>();
        [SerializeField] private List<FlyingAgent> calculableAgents = new List<FlyingAgent>();
        [Space]
        [SerializeField] private double movableExecutionTime = 0;
        [SerializeField] private double calculateExecutionTime = 0;

        Stopwatch moveExecutionStopwatch = new Stopwatch();
        Stopwatch calculateExecutionStopwatch = new Stopwatch();

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

        private void CalculateAllAgentPaths()
        {
            if (calculableAgents.Count <= 0)
                return;

            calculateExecutionStopwatch.Start();

            for (int i = 0; i < calculableAgents.Count; i++)
            {
                AStar(calculableAgents[i]);

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

        public void AddAgentToCalculation(FlyingAgent agent)
        {
            calculableAgents.Add(agent);
        }

        public void RemoveAgentFromMovable(FlyingAgent agent)
        {
            movableAgents.Remove(agent);
        }

        public void CreateInstance()
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

        public void AStar(FlyingAgent agent)
        {
            originVolume = agent.ActiveVolume;
            targetVolume = agent.ActiveVolume;
            
            int tempLinkIndex = -1;
            float tempDistance = float.MaxValue;
            float distance = math.distance(originVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);
            List<NavigationSubLink> links = agent.ActiveVolume.Links;

            for (int i = 0; i < links.Count; i++)
            {
                tempDistance = math.distance(links[i].LinkedVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);

                if (tempDistance < distance)
                {
                    distance = tempDistance;

                    targetVolume = links[i].LinkedVolume;
                    tempLinkIndex = i;
                }
            }

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

            agent.SetPath(new NavigationPath(wayPoints));
            wayPoints.Clear();
        }
    }
}


