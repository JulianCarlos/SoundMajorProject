using log4net.Util;
using Pathfinding.Helpers;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class GetNeighborsJob : IJobParallelFor
{
    public int TotalCells;
    public int TotalCores;
    public int TotalCellsPerCore;
    public int directionCount = 6;

    public uint cellSize = 2;
    public float detectionRadius = 2f;

    public NativeArray<Cell> Cells;
    public NativeArray<GridCore> Cores;
    public NativeArray<NeighborData> CellNeighbors;

    public NativeArray<int> neighbors;
    public NativeArray<int3> directions;

    public void Execute(int index)
    {
        GetNeighbours(index);
    }

    private void GetNeighbours(int index)
    {
        float3 position = Cells[index].CellPos;
        NativeArray<int> neighbors = new NativeArray<int>(directionCount, Allocator.Temp);

        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(1, Allocator.Temp);
        NativeArray<BoxcastCommand> commands = new NativeArray<BoxcastCommand>(1, Allocator.Temp);

        for (int i = 0; i < directionCount; i++)
        {
            commands[i] = new BoxcastCommand(position, Vector3.one * detectionRadius, Quaternion.identity, CalculationHelper.Int3ToFloat3(directions[i]), cellSize);
        }

        JobHandle handle = BoxcastCommand.ScheduleBatch(commands, results, 1);
        handle.Complete();

        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].collider == null)
            {
                int targetCellIndex = FindNearestCell(position + (directions[i] * (int)cellSize));
            
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
