using Unity.Collections;
using UnityEngine;
using System.Diagnostics;
using Unity.Jobs;

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public unsafe class PathingManager : MonoBehaviour
    {
        public static PathingManager Instance { get; private set; }

        [SerializeField] private double miliseconds = 0;

        private void Awake()
        {
            CreateInstance();
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

        public NavigationPath AStar(Vector3 initialPos, Vector3 targetPos, NavigationVolume targetVolume)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            AStarJob job = new AStarJob()
            {
                TotalCells = targetVolume.TotalCells,
                TotalCellsPerCore = targetVolume.TotalCellsPerCore,
                TotalCores = targetVolume.TotalCores,

                Cores = targetVolume.Cores,
                Cells = targetVolume.Cells,
                CellNeighbors = targetVolume.CellNeighbors,

                InitialPos = initialPos,
                TargetPos = targetPos,

                TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.TempJob),
                OpenCells = new NativeArray<int>(targetVolume.TotalCells, Allocator.TempJob),
                WalkPoints = new NativeList<Vector3>(Allocator.TempJob),
            };

            JobHandle handle = job.Schedule();

            handle.Complete();

            NativeList<Vector3> tempWayPoints = new NativeList<Vector3>(Allocator.Temp);
            tempWayPoints.CopyFrom(job.WalkPoints);

            job.TempData.Dispose();
            job.OpenCells.Dispose();
            job.WalkPoints.Dispose();

            stopwatch.Stop();
            miliseconds = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);

            return new NavigationPath(tempWayPoints);
        }
    }
}


