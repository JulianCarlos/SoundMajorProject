using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct MoveToTargetJob : IJob
{
    public float3 TargetPos;

    public NativeArray<int> CurrentPoint;
    public NativeArray<int> EndPoint;
    public int OpenCellsCount;

    public NativeList<Cell> Cells;
    public NativeList<int> OpenCells;
    public NativeArray<TempData> TempData;
    public NativeArray<NeighborData> CellNeighbors;

    public void Execute()
    {
        int neighborIndex;
        NeighborData neighborData;

        while (CurrentPoint != EndPoint && OpenCellsCount > 0)
        {
            CurrentPoint[0] = OpenCells[0];

            neighborData = CellNeighbors[CurrentPoint[0]];

            OpenCells.RemoveAt(0);
            OpenCellsCount--;

            for (int i = 0; i < 6; i++)
            {
                neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || TempData[neighborIndex].FCost > 0)
                    continue;

                TempData[neighborIndex] = new TempData(CurrentPoint[0], CalculationHelper.CalculateSquaredDistance(Cells[neighborIndex].CellPos, TargetPos));

                OpenCells.Add(neighborIndex);
                OpenCellsCount++;
            }
        }
    }
}
