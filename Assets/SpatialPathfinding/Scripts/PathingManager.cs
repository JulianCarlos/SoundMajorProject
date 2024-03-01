using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using System;
using Unity.Burst;
using Unity.Jobs;

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public unsafe class PathingManager : MonoBehaviour
    {
        public static PathingManager Instance { get; private set; }

        [SerializeField] private double miliseconds = 0;

        private int openCellsCount = 0;

        private int startingPoint = 0;
        private int currentPoint = 0;
        private int endPoint = 0;

        private NativeArray<TempData> tempData;
        private NativeList<int> openCells = new NativeList<int>(Allocator.Persistent);
        private List<Vector3> walkpoints = new List<Vector3>();

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

        public Vector3[] AStar(Vector3 initialPos, Vector3 targetPos, NavigationVolume targetVolume)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            FindPoints(initialPos, targetPos, targetVolume);

            InitializeBuffers(targetVolume);

            MoveToTarget(targetPos, targetVolume);

            Vector3[] waypoints = SearchOrigin(targetVolume).ToArray();

            System.Array.Reverse(waypoints);

            ClearBuffers();

            stopwatch.Stop();
            miliseconds = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);

            return waypoints;
        }

        private void FindPoints(float3 player, float3 target, NavigationVolume targetVolume)
        {
            startingPoint = FindNearestCell(player, targetVolume);
            endPoint = FindNearestCell(target, targetVolume);
        }

        private void InitializeBuffers(NavigationVolume targetVolume)
        {
            tempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.Temp);
            tempData[startingPoint] = new TempData(-1, 1000);

            openCells.Add(targetVolume.Cells[startingPoint].Index);
            openCellsCount++;

            currentPoint = openCells[0];
        }

        private void MoveToTarget(Vector3 targetPos, NavigationVolume targetVolume)
        {
            int neighborIndex = 0;
            NeighborData neighborData;

            while (currentPoint != endPoint && openCellsCount > 0)
            {
                currentPoint = openCells[0];

                openCells.RemoveAt(0);
                openCellsCount--;

                neighborData = targetVolume.CellNeighbors[currentPoint];

                for (int i = 0; i < 6; i++)
                {
                    neighborIndex = neighborData.Neighbors[i];

                    if (neighborIndex < 0 || tempData[neighborIndex].FCost > 0)
                        continue;

                    tempData[neighborIndex] = new TempData(currentPoint, CalculationHelper.CalculateSquaredDistance(targetVolume.Cells[neighborIndex].CellPos, targetPos));

                    openCells.Add(neighborIndex);
                    openCellsCount++;
                }
            }
        }

        private List<Vector3> SearchOrigin(NavigationVolume targetVolume)
        {
            var data = tempData[currentPoint];

            while (currentPoint != startingPoint)
            {
                UnityEngine.Debug.DrawLine(targetVolume.Cells[currentPoint].CellPos, targetVolume.Cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 60f);
                walkpoints.Add(targetVolume.Cells[currentPoint].CellPos);
                currentPoint = data.ParentIndex;

                data = tempData[currentPoint];
            }

            return walkpoints;
        }

        private int FindNearestCell(float3 position, NavigationVolume targetVolume)
        {
            float tempDistance;
            int closestCore = 0;
            float distance = float.MaxValue;

            for (int i = 0; i < targetVolume.TotalCores; i++)
            {
                tempDistance = CalculationHelper.CalculateSquaredDistance(targetVolume.Cores[i].CorePos, position);

                if (tempDistance < distance)
                {
                    distance = tempDistance;
                    closestCore = i;
                }
            }

            distance = float.MaxValue;
            int closestCell = 0;

            int[] subCells = targetVolume.Cores[closestCore].SubCells;

            for (int i = 0; i < targetVolume.TotalCellsPerCore; i++)
            {
                tempDistance = CalculationHelper.CalculateSquaredDistance(targetVolume.Cells[subCells[i]].CellPos, position);

                if (tempDistance < distance)
                {
                    distance = tempDistance;
                    closestCell = subCells[i];
                }
            }

            return closestCell;
        }

        private void ClearBuffers()
        {
            openCellsCount = 0;

            openCells.Clear();
            walkpoints.Clear();
            tempData.Dispose();
        }

        private void OnDestroy()
        {
            openCells.Dispose();
        }
    }
}


