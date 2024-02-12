using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;

[DefaultExecutionOrder(100)]
public unsafe class PathingManager : MonoBehaviour
{
    public static PathingManager Instance { get; private set; }
    public NativeArray<int3> Directions;

    [Space]
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private bool ShowGrid = false;

    private int startingPoint;
    private int currentPoint;
    private int endPoint;

    //private GridCore[] cores;
    //private NativeList<Cell> cells;
    private NativeList<int> openCells;
    private List<Vector3> Walkpoints = new();

    private NativeArray<TempData> tempData;
    //private NativeArray<NeighborData> cellNeighbors;

    private int openCellsCount = 0;

    public NavigationVolume targetVolume = new ();

    private void Awake()
    {
        CreateInstance();

        openCells = new NativeList<int>(Allocator.Persistent);

        Directions = new NativeArray<int3>(6, Allocator.Persistent);

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
        this.targetVolume = targetVolume;
        //targetVolume = FindObjectOfType<NavigationVolume>();

        Debug.Log(targetVolume != null);

        FindPoints(initialPos, targetPos);

        InitializeBuffers();

        MoveToTarget(targetPos);

        Vector3[] waypoints = SearchOrigin().ToArray();
        System.Array.Reverse(waypoints);

        ClearBuffers();

        return waypoints;
    }

    private void FindPoints(float3 player, float3 target)
    {
        startingPoint = FindNearestCell(player);
        endPoint = FindNearestCell(target);
    }

    private void InitializeBuffers()
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
        Directions[0] = new int3( 0,  0,  1);
        Directions[1] = new int3( 0,  0, -1);
        Directions[2] = new int3( 1,  0,  0);
        Directions[3] = new int3(-1,  0 , 0);

        //Vertical
        Directions[4] = new int3( 0,  1,  0);
        Directions[5] = new int3( 0, -1,  0);
    }

    private void MoveToTarget(Vector3 targetPos)
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

    private List<Vector3> SearchOrigin()
    {
        var data = tempData[currentPoint];

        while (currentPoint != startingPoint)
        {
            UnityEngine.Debug.DrawLine(targetVolume.cells[currentPoint].CellPos, targetVolume.cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 60f);
            Walkpoints.Add(targetVolume.cells[currentPoint].CellPos);
            currentPoint = data.ParentIndex;

            data = tempData[currentPoint];
        }

        return Walkpoints;
    }

    private void ClearBuffers()
    {
        Walkpoints.Clear();
        tempData.Dispose();
        openCells.Clear();
        openCellsCount = 0;
    }

    private int FindNearestCell(float3 position)
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
        //cells.Dispose();
        Directions.Dispose();
        openCells.Dispose();
        //cellNeighbors.Dispose();
    }
}
