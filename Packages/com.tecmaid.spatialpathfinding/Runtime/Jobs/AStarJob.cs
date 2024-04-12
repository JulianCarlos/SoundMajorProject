using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Helpers;

[BurstCompile(CompileSynchronously = false, DisableSafetyChecks = true)]
public struct AStarJob : IJob
{
    [ReadOnly] public int TotalCells;
    [ReadOnly] public int VolumeWidth;
    [ReadOnly] public int VolumeHeight;
    [ReadOnly] public int VolumeDepth;

    [ReadOnly] public float3 TargetPos;
    [ReadOnly] public float3 InitialPos;

    [ReadOnly] public NativeArray<Cell> Cells;
    [ReadOnly] public NativeArray<int> OpenCells;
    [ReadOnly] public NativeArray<NeighborData> CellNeighbors;

    [WriteOnly] public NativeList<float3> WalkPoints;
    [WriteOnly] public NativeArray<TempData> TempData;

    private int endPoint;
    private int currentPoint;
    private int startingPoint;

    private int OpenCellsCount;
    private int ClosedCellsCount;

    private int indexX;
    private int indexY;
    private int indexZ;

    public void Execute()
    {
        FindPoints(InitialPos, TargetPos);
        InitializeBuffers();
        MoveToTarget(TargetPos);
        SearchOrigin();

        //WalkPoints[0] = TargetPos;
        //WalkPoints[^1] = InitialPos;
    }

    private void FindPoints(float3 player, float3 target)
    {
        startingPoint = FindNearestCell(player);
        endPoint = FindNearestCell(target);
    }

    private int FindNearestCell(float3 position)
    {
        int index;
        float tempDistance;
        float distanceX = float.MaxValue;
        float distanceY = float.MaxValue;
        float distanceZ = float.MaxValue;

        for (int x = 0; x < VolumeWidth; x++)
        {
            index = CalculationHelper.FlattenIndex(new int3(x, 0, 0), VolumeWidth, VolumeHeight);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);
            
            if (tempDistance < distanceX)
            {
                distanceX = tempDistance;
                indexX = x;
            }
        }
        for (int y = 0; y < VolumeHeight; y++)
        {
            index = CalculationHelper.FlattenIndex(new int3(0, y, 0), VolumeWidth, VolumeHeight);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);

            if (tempDistance < distanceY)
            {
                distanceY = tempDistance;
                indexY = y;
            }
        }
        for (int z = 0; z < VolumeDepth; z++)
        {
            index = CalculationHelper.FlattenIndex(new int3(0, 0, z), VolumeWidth, VolumeHeight);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);

            if (tempDistance < distanceZ)
            {
                distanceZ = tempDistance;
                indexZ = z;
            }
        }

        return CalculationHelper.FlattenIndex(new int3(indexX, indexY, indexZ), VolumeWidth, VolumeHeight);
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
