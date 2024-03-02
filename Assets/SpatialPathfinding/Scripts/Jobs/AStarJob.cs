using Codice.Utils;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct AStarJob : IJob
{
    public Vector3 InitialPos;
    public Vector3 TargetPos;
    public NavigationVolume TargetVolume;
    public NativeArray<TempData> TempData;
    public NativeList<int> OpenCells;
    public NativeList<Vector3> WalkPoints;

    public int startingPoint;
    public int currentPoint;
    public int endPoint;

    public int OpenCellsCount;

    public void Execute()
    {
        FindPoints(InitialPos, TargetPos, TargetVolume);
        InitializeBuffers(TargetVolume);
        MoveToTarget(TargetPos, TargetVolume);
        SearchOrigin(TargetVolume);
    }

    private void FindPoints(float3 player, float3 target, NavigationVolume targetVolume)
    {
        startingPoint = FindNearestCell(player, targetVolume);
        endPoint = FindNearestCell(target, targetVolume);
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

        NativeArray<int> subCells = new NativeArray<int>(targetVolume.Cores[closestCore].SubCells.Length, Allocator.Temp);
        subCells.CopyFrom(targetVolume.Cores[closestCore].SubCells);

        //int[] subCells = targetVolume.Cores[closestCore].SubCells;

        for (int i = 0; i < targetVolume.TotalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(targetVolume.Cells[subCells[i]].CellPos, position);

            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i];
            }
        }

        subCells.Dispose();

        return closestCell;
    }

    private void InitializeBuffers(NavigationVolume targetVolume)
    {
        TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.Temp);
        TempData[startingPoint] = new TempData(-1, 1000);

        OpenCells.Add(targetVolume.Cells[startingPoint].Index);
        OpenCellsCount++;

        currentPoint = OpenCells[0];
    }

    private unsafe void MoveToTarget(Vector3 targetPos, NavigationVolume targetVolume)
    {
        int neighborIndex = 0;
        NeighborData neighborData;

        while (currentPoint != endPoint && OpenCellsCount > 0)
        {
            currentPoint = OpenCells[0];

            OpenCells.RemoveAt(0);
            OpenCellsCount--;

            neighborData = targetVolume.CellNeighbors[currentPoint];

            for (int i = 0; i < 6; i++)
            {
                neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || TempData[neighborIndex].FCost > 0)
                    continue;

                TempData[neighborIndex] = new TempData(currentPoint, CalculationHelper.CalculateSquaredDistance(targetVolume.Cells[neighborIndex].CellPos, targetPos));

                OpenCells.Add(neighborIndex);
                OpenCellsCount++;
            }
        }
    }

    private void SearchOrigin(NavigationVolume targetVolume)
    {
        var data = TempData[currentPoint];

        while (currentPoint != startingPoint)
        {
            //UnityEngine.Debug.DrawLine(targetVolume.Cells[currentPoint].CellPos, targetVolume.Cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 60f);
            WalkPoints.Add(targetVolume.Cells[currentPoint].CellPos);
            currentPoint = data.ParentIndex;

            data = TempData[currentPoint];
        }
    }
}
