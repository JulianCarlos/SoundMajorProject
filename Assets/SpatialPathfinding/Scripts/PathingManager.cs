using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;

[DefaultExecutionOrder(100)]
public unsafe class PathingManager : MonoBehaviour
{
    public static PathingManager Instance { get; private set; }
    public NativeArray<int3> Directions => directions;

    private NativeArray<int3> directions = new NativeArray<int3>(6, Allocator.Persistent);
    private NativeList<int> openCells = new NativeList<int>(Allocator.Persistent);
    private NativeArray<TempData> tempData;
    private List<Vector3> walkpoints = new();

    private int openCellsCount = 0;

    private int startingPoint = 0;
    private int currentPoint = 0;
    private int endPoint = 0;

    private void Awake()
    {
        CreateInstance();

        InitializeDirections();
    }

    private void CreateInstance()
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

        print(stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency));

        return waypoints;
    }

    private void FindPoints(float3 player, float3 target, NavigationVolume targetVolume)
    {
        startingPoint = FindNearestCell(player, targetVolume);
        endPoint = FindNearestCell(target, targetVolume);
    }

    private void InitializeBuffers(NavigationVolume targetVolume)
    {
        tempData = new NativeArray<TempData>(targetVolume.totalCells, Allocator.Temp);
        tempData[startingPoint] = new TempData(-1, 1000);

        openCells.Add(targetVolume.cells[startingPoint].Index);
        openCellsCount++;

        currentPoint = openCells[0];
    }

    private void InitializeDirections()
    {
        //Horizontal
        directions[0] = new int3( 0,  0,  1);
        directions[1] = new int3( 0,  0, -1);
        directions[2] = new int3( 1,  0,  0);
        directions[3] = new int3(-1,  0 , 0);

        //Vertical
        directions[4] = new int3( 0,  1,  0);
        directions[5] = new int3( 0, -1,  0);
    }

    private void MoveToTarget(Vector3 targetPos, NavigationVolume targetVolume)
    {
        int neighborIndex;
        NeighborData neighborData;

        while (currentPoint != endPoint && openCellsCount > 0)
        {
            currentPoint = openCells[0];

            neighborData = targetVolume.cellNeighbors[currentPoint];

            openCells.RemoveAt(0);
            openCellsCount--;

            for (int i = 0; i < 6; i++)
            {
                neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || tempData[neighborIndex].FCost > 0)
                    continue;

                tempData[neighborIndex] = new TempData(currentPoint, CalculationHelper.CalculateSquaredDistance(targetVolume.cells[neighborIndex].CellPos, targetPos));

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
            UnityEngine.Debug.DrawLine(targetVolume.cells[currentPoint].CellPos, targetVolume.cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 60f);
            walkpoints.Add(targetVolume.cells[currentPoint].CellPos);
            currentPoint = data.ParentIndex;

            data = tempData[currentPoint];
        }

        return walkpoints;
    }

    private void ClearBuffers()
    {
        openCells.Clear();
        openCellsCount = 0;

        walkpoints.Clear();
        tempData.Dispose();
    }

    private int FindNearestCell(float3 position, NavigationVolume targetVolume)
    {
        int closestCore = 0;
        float tempDistance;
        float distance = float.MaxValue;

        for (int i = 0; i < targetVolume.totalCores; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(targetVolume.cores[i].CorePos, position);
        
            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCore = i;
            }
        }

        distance = float.MaxValue;
        int closestCell = 0;

        int[] subCells = targetVolume.cores[closestCore].SubCells;

        for (int i = 0; i < targetVolume.totalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(targetVolume.cells[subCells[i]].CellPos, position);
        
            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i];
            }
        }
        
        return closestCell;
    }

    private void OnDestroy()
    {
        directions.Dispose();
        openCells.Dispose();
    }
}
