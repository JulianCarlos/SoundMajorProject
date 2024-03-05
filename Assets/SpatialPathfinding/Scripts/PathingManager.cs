using Unity.Collections;
using UnityEngine;
using System.Diagnostics;
using Unity.Jobs;
using System.Collections.Generic;
using System.Collections;
using System;
using Unity.Mathematics;
using System.Diagnostics.CodeAnalysis;

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

        [SerializeField] private double miliseconds = 0;

        Stopwatch stopwatch = new Stopwatch();

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
                //stopwatch.Start();

                for (int i = 0; i < movableAgents.Count; i++)
                {
                    movableAgents[i].Move();
                }

                //stopwatch.Stop();
                //miliseconds = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
                //stopwatch.Reset();

                yield return null;
            }
        }

        private IEnumerator CalculateAllAgentPaths()
        {
            while (true)
            {
                yield return null;

                if (calculableAgents.Count <= 0)
                    continue;

                stopwatch.Start();

                for (int i = 0; i < calculableAgents.Count; i++)
                {
                    AStar(calculableAgents[i]);

                    if (!movableAgents.Contains(calculableAgents[i]))
                    {
                        movableAgents.Add(calculableAgents[i]);
                        calculableAgents.Remove(calculableAgents[i]);
                    }

                }

                stopwatch.Stop();
                miliseconds = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
                stopwatch.Reset();
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
                WalkPoints = new NativeList<float3>(Allocator.TempJob),
            };

            JobHandle handle = job.Schedule();

            handle.Complete();

            agent.SetPath(new NavigationPath(job.WalkPoints));

            job.TempData.Dispose();
            job.OpenCells.Dispose();
            job.WalkPoints.Dispose();
        }
    }
}


