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

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public class PathingManager : MonoBehaviour
    {
        public static PathingManager Instance { get; private set; }

        public static Action<FlyingAgent> OnAgentStartedPathing;
        public static Action<FlyingAgent> OnAgentFinishedPathing;

        [SerializeField] private List<FlyingAgent> movableAgents = new List<FlyingAgent>();
        [SerializeField] private List<FlyingAgent> calculableAgents = new List<FlyingAgent>();
        [Space]
        [SerializeField] private double movableExecutionTime = 0;
        [SerializeField] private double calculateExecutionTime = 0;

        Stopwatch moveExecutionStopwatch = new Stopwatch();
        Stopwatch calculateExecutionStopwatch = new Stopwatch();

        private NativeList<float3> wayPoints = new NativeList<float3>(Allocator.Persistent);
        private bool foundTargetVolume = false;

        private AStarJob targetJob;
        private AStarJob originJob;
        private JobHandle aStarHandle;

        private void Awake()
        {
            CreateInstance();

            OnAgentStartedPathing += AddAgentToCalculation;
            OnAgentFinishedPathing += RemoveAgentFromMovable;

            StartCoroutine(nameof(CalculateAllAgentPaths));
            StartCoroutine(nameof(MoveAllAgents));
        }

        private IEnumerator MoveAllAgents()
        {
            while (true)
            {
                yield return null;

                if (movableAgents.Count <= 0)
                    continue;

                moveExecutionStopwatch.Start();

                for (int i = 0; i < movableAgents.Count; i++)
                {
                    movableAgents[i].Move();
                }

                moveExecutionStopwatch.Stop();
                movableExecutionTime = moveExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
                moveExecutionStopwatch.Reset();
            }
        }

        private IEnumerator CalculateAllAgentPaths()
        {
            while (true)
            {
                yield return null;

                if (calculableAgents.Count <= 0)
                    continue;

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
            NavigationVolume originVolume;
            NavigationVolume targetVolume;

            foundTargetVolume = false;

            for (int i = 0; i < agent.ActiveVolume.Links.Count; i++)
            {
                if (BoundingBoxChecker.IsPositionInsideVolume(agent.TargetPos, agent.ActiveVolume.Links[i].LinkedVolume))
                {
                    foundTargetVolume = true;

                    targetVolume = agent.ActiveVolume.Links[i].LinkedVolume;
                    targetJob = JobFactory.GenerateAStarJob(targetVolume, agent.ActiveVolume.Links[i].NeighborLink.transform.position, agent.TargetPos, this.wayPoints);
                    aStarHandle = targetJob.Schedule();
                    aStarHandle.Complete();
                    targetJob.TempData.Dispose();
                    targetJob.OpenCells.Dispose();

                    originVolume = agent.ActiveVolume;
                    originJob = JobFactory.GenerateAStarJob(originVolume, agent.InitialPos, originVolume.Links[i].transform.position, this.wayPoints);
                    aStarHandle = originJob.Schedule();
                    aStarHandle.Complete();
                    originJob.TempData.Dispose();
                    originJob.OpenCells.Dispose();

                    break;
                }
            }

            if (!foundTargetVolume)
            {
                targetJob = JobFactory.GenerateAStarJob(agent.ActiveVolume, agent.InitialPos, agent.TargetPos, this.wayPoints);
                aStarHandle = targetJob.Schedule();
                aStarHandle.Complete();
                targetJob.TempData.Dispose();
                targetJob.OpenCells.Dispose();
            }

            agent.SetPath(new NavigationPath(wayPoints));
            wayPoints.Clear();
        }
    }
}


