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

    [SerializeField, Range(1, 15)] private int cellSize = 1;
    [SerializeField] private int3 cellAmount;
    [SerializeField] private int amountOfCellsPerMainCell;

    [Space]
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private bool ShowGrid = false;

    private int startingPoint;
    private int currentPoint;
    private int endPoint;

    private float3 playerPos;
    private float3 targetPos;

    private NativeList<Cell> cells;
    private NativeList<int> openCells;
    private List<Vector3> Walkpoints = new();

    private NativeArray<int3> directions;
    private NativeArray<TempData> tempData;
    private NativeArray<NeighborData> cellNeighbors;

    private GridCore[] cores;

    private int totalCells;
    private int openCellsCount = 0;

    private int totalCores;
    private int totalCellsPerCore;

    private void Awake()
    {
        CreateInstance();

        totalCells = ((cellAmount.x * amountOfCellsPerMainCell) * (cellAmount.y * amountOfCellsPerMainCell) * (cellAmount.z * amountOfCellsPerMainCell));

        openCells = new NativeList<int>(Allocator.Persistent);

        cells = new NativeList<Cell>(totalCells, Allocator.Persistent);
        cores = new GridCore[cellAmount.x * cellAmount.y * cellAmount.z];

        directions = new NativeArray<int3>(6, Allocator.Persistent);

        cellNeighbors = new NativeArray<NeighborData>(totalCells, Allocator.Persistent);

        InitializeDirections();
    }

    private void Start()
    {
        InitializeGrid();

        totalCores = cores.Count();
        totalCellsPerCore = amountOfCellsPerMainCell * amountOfCellsPerMainCell * amountOfCellsPerMainCell;

        GetAllCellNeighbors();
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

    public Vector3[] AStar(Vector3 initialPos, Vector3 targetPos)
    {
        FindPoints(initialPos, targetPos);

        InitializeBuffers();

        MoveToTarget();

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
        tempData = new NativeArray<TempData>(totalCells, Allocator.Temp);
        tempData[startingPoint] = new TempData(-1, 1000);

        openCells.Add(cells[startingPoint].Index);
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

    private void MoveToTarget()
    {
        int neighborIndex;
        NeighborData neighborData;

        while (currentPoint != endPoint && openCellsCount > 0)
        {
            currentPoint = openCells[0];

            neighborData = cellNeighbors[currentPoint];

            openCells.RemoveAt(0);
            openCellsCount--;

            for (int i = 0; i < 6; i++)
            {
                neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || tempData[neighborIndex].FCost > 0)
                    continue;

                tempData[neighborIndex] = new TempData(currentPoint, CalculationHelper.CalculateSquaredDistance(cells[neighborIndex].CellPos, targetPos));

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
            UnityEngine.Debug.DrawLine(cells[currentPoint].CellPos, cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 60f);
            Walkpoints.Add(cells[currentPoint].CellPos);
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

        for (int i = 0; i < totalCores; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(cores[i].CorePos, position);
        
            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCore = i;
            }
        }

        distance = float.MaxValue;
        int closestCell = 0;

        int[] subCells = cores[closestCore].SubCells;

        for (int i = 0; i < totalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(cells[subCells[i]].CellPos, position);
        
            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i];
            }
        }
        
        return closestCell;
    }

    private void GetAllCellNeighbors()
    {
        for (int i = 0; i < totalCells; i++)
        {
            GetNeighbours(cells[i].CellPos, i);
        }
    }

    private void GetNeighbours(float3 position, int index)
    {
        NativeArray<int> neighbors = new NativeArray<int>(6, Allocator.Temp);

        for (int i = 0;  i < 6; i++)
        {
            if (!Physics.Raycast(position, CalculationHelper.Int3ToVector3(directions[i]), cellSize))
            {
                int targetCellIndex = FindNearestCell(position + (directions[i] * cellSize));

                neighbors[i] = targetCellIndex;
            }
            else
            {
                neighbors[i] = -1;
            }
        }

        cellNeighbors[index] = new NeighborData(neighbors.ToArray());

        neighbors.Dispose();
    }

    private void InitializeGrid()
    {
        int index = 0;
        int coreIndex = 0;
        List<int> tempSubCells;

        for (int x = 0; x < cellAmount.x; x++)
        {
            for (int y = 0; y < cellAmount.y; y++)
            {
                for (int z = 0; z < cellAmount.z; z++)
                {
                    float3 mainCellCenter = new float3(
                    transform.position.x + ((x - (cellAmount.x - 1) / 2) * cellSize) * amountOfCellsPerMainCell,
                    transform.position.y + ((y - (cellAmount.y - 1) / 2) * cellSize) * amountOfCellsPerMainCell,
                    transform.position.z + ((z - (cellAmount.z - 1) / 2) * cellSize) * amountOfCellsPerMainCell);

                    tempSubCells = new();

                    for (int a = 0; a < amountOfCellsPerMainCell; a++)
                    {
                        for (int b = 0; b < amountOfCellsPerMainCell; b++)
                        {
                            for (int c = 0; c < amountOfCellsPerMainCell; c++)
                            {
                                float3 subcellCenter = new float3(
                                    mainCellCenter.x + (a - (amountOfCellsPerMainCell - 1) / 2) * cellSize,
                                    mainCellCenter.y + (b - (amountOfCellsPerMainCell - 1) / 2) * cellSize,
                                    mainCellCenter.z + (c - (amountOfCellsPerMainCell - 1) / 2) * cellSize
                                );

                                Cell cell = new Cell(subcellCenter, index);

                                cells.Add(cell);
                                tempSubCells.Add(cell.Index);

                                index++;
                            }
                        }
                    }

                    GridCore core = new GridCore(mainCellCenter, tempSubCells.ToArray());
                    cores[coreIndex] = (core);
                    coreIndex++;
                }
            }
        }
    }

    private void OnDestroy()
    {
        cells.Dispose();
        directions.Dispose();
        openCells.Dispose();
        cellNeighbors.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (ShowGrid)
        {
            for (int x = 0; x < cellAmount.x; x++)
            {
                for (int y = 0; y < cellAmount.y; y++)
                {
                    for (int z = 0; z < cellAmount.z; z++)
                    {
                        Vector3 mainCellCenter = new Vector3(
                        transform.position.x + ((x - (cellAmount.x - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.y + ((y - (cellAmount.y - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.z + ((z - (cellAmount.z - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(mainCellCenter, Vector3.one);

                        for (int a = 0; a < amountOfCellsPerMainCell; a++)
                        {
                            for (int b = 0; b < amountOfCellsPerMainCell; b++)
                            {
                                for (int c = 0; c < amountOfCellsPerMainCell; c++)
                                {
                                    Vector3 subcellCenter = new Vector3(
                                        mainCellCenter.x + (a - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.y + (b - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.z + (c - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize
                                    );

                                    Gizmos.color = Color.red;
                                    Gizmos.DrawWireCube(subcellCenter, Vector3.one * 0.1f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
