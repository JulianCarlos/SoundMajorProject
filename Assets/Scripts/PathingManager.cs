using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;

public unsafe class PathingManager : MonoBehaviour
{
    [SerializeField, Range(1, 15)] private int cellSize = 1;
    [SerializeField] private int3 cellAmount;
    [SerializeField] private int amountOfCellsPerMainCell;

    [Space]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject playerObject;

    [SerializeField] private TextMeshProUGUI msText;

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
    private NativeList<float3> Walkpoints;

    private NativeArray<int3> directions;
    private NativeArray<TempData> tempData;
    private NativeArray<NeighborData> cellNeighbors;

    private List<GridCore> cores = new List<GridCore>();

    private int totalCells;
    private int openCellsCount = 0;

    private int totalCores;
    private int totalCellsPerCore;


    private void Awake()
    {
        totalCells = ((cellAmount.x * amountOfCellsPerMainCell) * (cellAmount.y * amountOfCellsPerMainCell) * (cellAmount.z * amountOfCellsPerMainCell));

        openCells = new NativeList<int>(Allocator.Persistent);
        Walkpoints = new NativeList<float3>(totalCells, Allocator.Persistent);

        cells = new NativeList<Cell>(totalCells, Allocator.Persistent);
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

        InvokeRepeating(nameof(AStar), 0, 0.1f);
    }

    private void AStar()
    {
        var then = Time.realtimeSinceStartup;

        FindPoints(playerObject.transform.position, targetObject.transform.position);

        InitializeBuffers();

        MoveToTarget();

        SearchOrigin();

        ClearBuffers();

        msText.text = "ms: " + ((Time.realtimeSinceStartup - then) * 1000f);
    }

    private void FindPoints(float3 player, float3 target)
    {
        playerPos = player;
        targetPos = target;

        startingPoint = FindNearestCell(playerPos);
        endPoint = FindNearestCell(targetPos);
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
        directions[0] = new int3( 0,  0,  1);
        directions[1] = new int3( 0,  0, -1);
        directions[2] = new int3( 1,  0,  0);
        directions[3] = new int3(-1,  0 , 0);
        directions[4] = new int3( 0,  1,  0);
        directions[5] = new int3( 0, -1,  0);
    }

    private void MoveToTarget()
    {
        float cost;
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

                cost = CalculationHelper.CalculateSquaredDistance(cells[neighborIndex].CellPos, targetPos);

                tempData[neighborIndex] = new TempData(currentPoint, cost);

                openCells.Add(neighborIndex);
                openCellsCount++;
            }
        }
    }

    private void SearchOrigin()
    {
        var data = tempData[currentPoint];
        while (data.ParentIndex != -1)
        {
            //UnityEngine.Debug.DrawLine(cells[currentPoint].CellPos, cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 0.1f);
            Walkpoints.Add(cells[currentPoint].CellPos);
            currentPoint = data.ParentIndex;

            data = tempData[currentPoint];
        }
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
        Cell[] subCells;

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
        subCells = cores[closestCore].SubCells;

        for (int i = 0; i < totalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(subCells[i].CellPos, position);
        
            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i].Index;
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
        List<Cell> tempCells;

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

                    tempCells = new();

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
                                tempCells.Add(cell);

                                index++;
                            }
                        }
                    }

                    GridCore core = new GridCore(mainCellCenter, tempCells.ToArray());
                    cores.Add(core);
                }
            }
        }
    }

    private void OnDestroy()
    {
        cells.Dispose();
        directions.Dispose();
        Walkpoints.Dispose();
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
                        transform.position.x + ((x - (cellAmount.x - 1) / 2) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.y + ((y - (cellAmount.y - 1) / 2) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.z + ((z - (cellAmount.z - 1) / 2) * cellSize) * amountOfCellsPerMainCell);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(mainCellCenter, Vector3.one);

                        for (int a = 0; a < amountOfCellsPerMainCell; a++)
                        {
                            for (int b = 0; b < amountOfCellsPerMainCell; b++)
                            {
                                for (int c = 0; c < amountOfCellsPerMainCell; c++)
                                {
                                    Vector3 subcellCenter = new Vector3(
                                        mainCellCenter.x + (a - (amountOfCellsPerMainCell - 1) / 2) * cellSize,
                                        mainCellCenter.y + (b - (amountOfCellsPerMainCell - 1) / 2) * cellSize,
                                        mainCellCenter.z + (c - (amountOfCellsPerMainCell - 1) / 2) * cellSize
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
