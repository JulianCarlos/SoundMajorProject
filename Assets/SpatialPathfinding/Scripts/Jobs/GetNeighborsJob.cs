using log4net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GetNeighborsJob : IJobFor
{
    public int TotalCells;
    public int TotalCores;
    public int TotalCellsPerCore;

    public uint CellSize;
    public float DetectionRadius;

    public NativeArray<Cell> Cells;
    public NativeArray<GridCore> Cores;
    public NativeArray<NeighborData> CellNeighbors;

    private int directionCount;

    private RaycastHit directionHit;
    private NativeArray<int3> directions;

    public void Execute(int index)
    {
        GetNeighbours(Cells[index].CellPos, index);
    }

    private void GetNeighbours(float3 position, int index)
    {
        NativeArray<int> neighbors = new NativeArray<int>(directionCount, Allocator.Temp);

        for (int i = 0; i < directionCount; i++)
        {
            if (!Physics.BoxCast(position, Vector3.one * DetectionRadius, CalculationHelper.Int3ToVector3(directions[i]), out directionHit, Quaternion.identity, CellSize))
            {
                int targetCellIndex = FindNearestCell(position + (directions[i] * (int)CellSize));

                neighbors[i] = targetCellIndex;
            }
            else
            {
                neighbors[i] = -1;
            }
        }

        CellNeighbors[index] = new NeighborData(neighbors);

        neighbors.Dispose();
    }

    private int FindNearestCell(float3 position)
    {
        int closestCore = 0;
        float tempDistance;
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
}
