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

    public int VolumeWidth;
    public int VolumeHeight;
    public int VolumeDepth;

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

    private int indexX;
    private int indexY;
    private int indexZ;

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
        float distanceX = float.MaxValue;
        float distanceY = float.MaxValue;
        float distanceZ = float.MaxValue;

        float tempDistance = 0;

        for (int x = 0; x < VolumeWidth; x++)
        {
            int index = CalculationHelper.FlattenIndex(new int3(x, 0, 0), VolumeWidth, VolumeHeight, VolumeDepth);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);

            if (tempDistance < distanceX)
            {
                distanceX = tempDistance;
                indexX = x;
            }
        }
        for (int y = 0; y < VolumeHeight; y++)
        {
            int index = CalculationHelper.FlattenIndex(new int3(0, y, 0), VolumeWidth, VolumeHeight, VolumeDepth);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);

            if (tempDistance < distanceY)
            {
                distanceY = tempDistance;
                indexY = y;
            }
        }
        for (int z = 0; z < VolumeDepth; z++)
        {
            int index = CalculationHelper.FlattenIndex(new int3(0, 0, z), VolumeWidth, VolumeHeight, VolumeDepth);
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);

            if (tempDistance < distanceZ)
            {
                distanceZ = tempDistance;
                indexZ = z;
            }
        }

        return CalculationHelper.FlattenIndex(new int3(indexX, indexY, indexZ), VolumeWidth, VolumeHeight, VolumeDepth);
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
