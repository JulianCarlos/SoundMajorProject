using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Helpers;

[BurstCompile(CompileSynchronously = false)]
public struct AStarJob : IJob
{
    public int TotalCells;
    public int TotalCores;
    public int TotalCellsPerCore;

    public float3 TargetPos;
    public float3 InitialPos;

    public NativeList<float3> WalkPoints;

    public NativeArray<Cell> Cells;
    public NativeArray<int> OpenCells;
    public NativeArray<GridCore> Cores;
    public NativeArray<TempData> TempData;
    public NativeArray<NeighborData> CellNeighbors;

    private int endPoint;
    private int currentPoint;
    private int startingPoint;

    private int OpenCellsCount;
    private int ClosedCellsCount;

    public void Execute()
    {
        FindPoints(InitialPos, TargetPos);
        InitializeBuffers();
        MoveToTarget(TargetPos);
        SearchOrigin();
    }

    private void FindPoints(float3 player, float3 target)
    {
        startingPoint = FindNearestCell(player);
        endPoint = FindNearestCell(target);
    }

    private int FindNearestCell(float3 position)
    {
        float tempDistance;
        int closestCore = 0;
        float distance = float.MaxValue;

        for (int i = 0; i < TotalCores; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cores[i].CorePos, position);

            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCore = i;
            }
        }

        distance = float.MaxValue;
        int closestCell = 0;

        NativeArray<int> subCells = new NativeArray<int>(Cores[closestCore].SubCells.Length, Allocator.Temp);

        for (int i = 0; i < Cores[closestCore].SubCells.Length; i++)
        {
            subCells[i] = Cores[closestCore].SubCells[i];
        }

        for (int i = 0; i < TotalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[subCells[i]].CellPos, position);

            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i];
            }
        }

        subCells.Dispose();

        return closestCell;
    }

    private void InitializeBuffers()
    {
        OpenCellsCount = 0;
        ClosedCellsCount = 0;

        TempData[startingPoint] = new TempData(-1, 1000);
        OpenCells[OpenCellsCount] = (Cells[startingPoint].Index);
        OpenCellsCount++;

        currentPoint = OpenCells[ClosedCellsCount];
    }

    private unsafe void MoveToTarget(Vector3 targetPos)
    {
        int neighborIndex = 0;
        NeighborData neighborData;

        while (currentPoint != endPoint && OpenCellsCount > 0)
        {
            currentPoint = OpenCells[ClosedCellsCount];

            ClosedCellsCount++;
            OpenCellsCount--;

            neighborData = CellNeighbors[currentPoint];

            for (int i = 0; i < 6; i++)
            {
                neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || TempData[neighborIndex].FCost > 0)
                    continue;

                TempData[neighborIndex] = new TempData(currentPoint, CalculationHelper.CalculateSquaredDistance(Cells[neighborIndex].CellPos, targetPos));

                OpenCells[ClosedCellsCount + OpenCellsCount] = (neighborIndex);
                OpenCellsCount++;
            }
        }
    }

    private void SearchOrigin()
    {
        var data = TempData[currentPoint];

        while (currentPoint != startingPoint)
        {
            WalkPoints.Add(Cells[currentPoint].CellPos);
            currentPoint = data.ParentIndex;

            data = TempData[currentPoint];
        }
    }
}
