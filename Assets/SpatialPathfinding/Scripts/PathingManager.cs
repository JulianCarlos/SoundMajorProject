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

        private JobHandle aStarHandle;

        private NativeList<float3> wayPoints = new NativeList<float3>(Allocator.Persistent);

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
            if (BoundingBoxChecker.IsPositionInsideVolume(agent.TargetPos, agent.ActiveVolume))
            {
                NavigationVolume targetVolume = agent.ActiveVolume;

                AStarJob job = new AStarJob()
                {
                    TotalCells = targetVolume.TotalCells,
                    TotalCellsPerCore = targetVolume.TotalCellsPerCore,
                    TotalCores = targetVolume.TotalCores,

                    Cores = targetVolume.Cores,
                    Cells = targetVolume.Cells,
                    CellNeighbors = targetVolume.CellNeighbors,

                    InitialPos = agent.InitialPos,
                    TargetPos = agent.TargetPos,

                    TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.TempJob),
                    OpenCells = new NativeArray<int>(targetVolume.TotalCells, Allocator.TempJob),
                    WalkPoints = wayPoints,
                };

                aStarHandle = job.Schedule();

                aStarHandle.Complete();

                agent.SetPath(new NavigationPath(wayPoints));

                job.TempData.Dispose();
                job.OpenCells.Dispose();
                wayPoints.Clear();
            }
            else if (agent.ActiveVolume.Links.Count > 0)
            {
                NavigationVolume targetVolume;
                NavigationVolume originVolume;

                bool found = false;

                for (int i = 0; i < agent.ActiveVolume.Links.Count; i++)
                {
                    if (BoundingBoxChecker.IsPositionInsideVolume(agent.TargetPos, agent.ActiveVolume.Links[i].LinkedVolume))
                    {
                        found = true;

                        targetVolume = agent.ActiveVolume.Links[i].LinkedVolume;

                        AStarJob job = new AStarJob()
                        {
                            TotalCells = targetVolume.TotalCells,
                            TotalCellsPerCore = targetVolume.TotalCellsPerCore,
                            TotalCores = targetVolume.TotalCores,

                            Cores = targetVolume.Cores,
                            Cells = targetVolume.Cells,
                            CellNeighbors = targetVolume.CellNeighbors,

                            InitialPos = agent.ActiveVolume.Links[i].NeighborLink.transform.position,
                            TargetPos = agent.TargetPos,

                            TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.TempJob),
                            OpenCells = new NativeArray<int>(targetVolume.TotalCells, Allocator.TempJob),
                            WalkPoints = wayPoints,
                        };

                        aStarHandle = job.Schedule();
                        aStarHandle.Complete();

                        job.TempData.Dispose();
                        job.OpenCells.Dispose();

                        //--------

                        originVolume = agent.ActiveVolume;

                        AStarJob originJob = new AStarJob()
                        {
                            TotalCells = originVolume.TotalCells,
                            TotalCellsPerCore = originVolume.TotalCellsPerCore,
                            TotalCores = originVolume.TotalCores,

                            Cores = originVolume.Cores,
                            Cells = originVolume.Cells,
                            CellNeighbors = originVolume.CellNeighbors,

                            InitialPos = agent.InitialPos,
                            TargetPos = originVolume.Links[i].transform.position,

                            TempData = new NativeArray<TempData>(originVolume.TotalCells, Allocator.TempJob),
                            OpenCells = new NativeArray<int>(originVolume.TotalCells, Allocator.TempJob),
                            WalkPoints = wayPoints,
                        };

                        aStarHandle = originJob.Schedule();
                        aStarHandle.Complete();

                        agent.SetPath(new NavigationPath(wayPoints));

                        originJob.TempData.Dispose();
                        originJob.OpenCells.Dispose();
                        wayPoints.Clear();

                        break;
                    }
                }

                if (!found)
                {
                    targetVolume = agent.ActiveVolume;

                    AStarJob job = new AStarJob()
                    {
                        TotalCells = targetVolume.TotalCells,
                        TotalCellsPerCore = targetVolume.TotalCellsPerCore,
                        TotalCores = targetVolume.TotalCores,

                        Cores = targetVolume.Cores,
                        Cells = targetVolume.Cells,
                        CellNeighbors = targetVolume.CellNeighbors,

                        InitialPos = agent.InitialPos,
                        TargetPos = agent.TargetPos,

                        TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.TempJob),
                        OpenCells = new NativeArray<int>(targetVolume.TotalCells, Allocator.TempJob),
                        WalkPoints = wayPoints,
                    };

                    aStarHandle = job.Schedule();

                    aStarHandle.Complete();

                    agent.SetPath(new NavigationPath(wayPoints));

                    job.TempData.Dispose();
                    job.OpenCells.Dispose();
                    wayPoints.Clear();
                }
            }
        }
    }
}


