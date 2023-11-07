using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;
using Debug = UnityEngine.Debug;

public class PathingManager : MonoBehaviour
{
    [SerializeField, Range(1, 5)] private int cellSize = 1;
    [SerializeField] private float3 cellAmount;

    [Space]
    [SerializeField] private GameObject targetObject;

    [Space]
    [SerializeField] private bool showGizmos = false;

    private int startingPoint;
    private int currentPoint;
    private int endPoint;
    
    private Heap openCells;
    private NativeList<int> closedCells;

    private NativeArray<Cell> cells;
    private NativeHashMap<float3, int> cellData;
    private Dictionary<int, NativeList<int>> cellNeighbors = new();

    private float3 playerPos;
    private float3 targetPos;

    private int totalCells;

    private NativeArray<float3> directions;

    private void Awake()
    {
        playerPos = transform.position;
        targetPos = targetObject.transform.position;

        totalCells = (int)(cellAmount.x * cellAmount.y * cellAmount.z);

        cells = new NativeArray<Cell>(totalCells, Allocator.Persistent);
        cellData = new NativeHashMap<float3, int>(totalCells, Allocator.Persistent);

        openCells = new(totalCells);
        closedCells = new NativeList<int>(Allocator.Persistent);

        InitializeDirections();
    }

    private void Start()
    {
        InitializeGrid();

        startingPoint = FindNearestCell(playerPos);
        endPoint = FindNearestCell(targetPos);

        cells[startingPoint] = new Cell(cells[startingPoint].CellPos, cells[startingPoint].Index, -1, 1000);

        openCells.Add(cells[startingPoint].Index);
        currentPoint = openCells.Elements[0];

        GetAllCellNeighbors();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            MoveToTarget();
        }
    }

    private int FindNearestCell(float3 position)
    {
        int tempClosest;

        NativeArray<int> closestCellArray = new NativeArray<int>(1, Allocator.TempJob);

        FindNearestCellJob findNearestCell = new FindNearestCellJob()
        {
            Cells = this.cells,
            PlayerPos = position,
            ClosestCell = closestCellArray,
        };

        JobHandle handle = findNearestCell.Schedule();

        handle.Complete();

        tempClosest = closestCellArray[0];

        closestCellArray.Dispose();

        return tempClosest;
    }

    private void InitializeDirections()
    {
        directions = new NativeArray<float3>(6, Allocator.Persistent);

        directions[0] = transform.forward;
        directions[1] = -transform.forward;
        directions[2] = transform.right;
        directions[3] = -transform.right;
        directions[4] = transform.up;
        directions[5] = -transform.up;
    }

    private void MoveToTarget()
    {
        var then = Time.realtimeSinceStartup;

        Cell neighborCell;
        int currentCellIndex;

        int neighbor;
        float cost;

        while (openCells.Size > 0)
        {
            currentCellIndex = cells[currentPoint].Index;

            if (currentPoint == endPoint)
                break;

            closedCells.Add(currentCellIndex);
            openCells.Pop();

            for (int i = 0; i < cellNeighbors[currentCellIndex].Length; i++)
            {
                neighbor = cellNeighbors[currentCellIndex][i];
                neighborCell = cells[neighbor];

                if (neighbor == endPoint)
                    break;

                cost = math.abs(cells[neighbor].CellPos.x - targetPos.x) + math.abs(cells[neighbor].CellPos.y - targetPos.y) + math.abs(cells[neighbor].CellPos.z - targetPos.z);

                cells[neighbor] = new Cell(neighborCell.CellPos, neighborCell.Index, currentCellIndex, cost);

                openCells.Add(neighborCell.Index);
            }

            currentPoint = openCells.Elements[0];
        }

        Debug.Log("Follow Path: " + ((Time.realtimeSinceStartup - then) * 1000f));

        SearchOrigin();
    }

    private void SearchOrigin()
    {
        while (cells[currentPoint].ParentIndex != -1)
        {
            UnityEngine.Debug.DrawLine(cells[currentPoint].CellPos, cells[cells[currentPoint].ParentIndex].CellPos, Color.green, 55f);
            currentPoint = cells[currentPoint].ParentIndex;
        }
    }

    private void GetAllCellNeighbors()
    {
        foreach (var index in cellData)
        {
            GetNeighbours(cells[index.Value].CellPos);
        }
    }

    private void GetNeighbours(float3 position)
    {
        int initialCell;
        int targetCell;

        cellData.TryGetValue(position, out initialCell);

        for (int i = 0; i < directions.Length; i++)
        {
            if (!Physics.Raycast(position, directions[i], cellSize))
            {
                if (cellData.TryGetValue((position + (directions[i] * cellSize)), out targetCell))
                {
                    cellNeighbors[initialCell].Add(cells[targetCell].Index);
                }
            }
        }
    }

    private void InitializeGrid()
    {
        var then = Time.realtimeSinceStartup;

        NativeArray<Cell> cells = new NativeArray<Cell>(totalCells, Allocator.Persistent);

        var job = new InitializeGridJob
        {
            TransformPosition = transform.position,
            CellAmount = (int3)cellAmount,
            CellSize = cellSize,
            Cells = cells,
        };

        JobHandle jobHandle = job.Schedule(totalCells, 200);

        jobHandle.Complete();

        this.cells.CopyFrom(cells);

        for (int i = 0; i < cells.Length; i++)
        {
            cellData.Add(cells[i].CellPos, cells[i].Index);
            cellNeighbors.Add(cells[i].Index, new NativeList<int>(Allocator.Persistent));
        }

        cells.Dispose();

        Debug.Log("Generating Grid: " + (Time.realtimeSinceStartup - then) * 1000f);
    }

    private void OnDestroy()
    {
        cells.Dispose();
        cellData.Dispose();
        closedCells.Dispose();
        directions.Dispose();

        foreach (var item in cellNeighbors.Values)
        {
            item.Dispose();
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
    
            foreach (var item in closedCells)
            {
                Gizmos.DrawCube(cells[item].CellPos, Vector3.one / 10);
            }
    
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(cells[startingPoint].CellPos, (Vector3)cellAmount * cellSize);
        }
    
        if (showGizmos)
        {
            Gizmos.color = Color.red;
            for (int x = 0; x < cellAmount.x; x++)
            {
                for (int y = 0; y < cellAmount.y; y++)
                {
                    for (int z = 0; z < cellAmount.z; z++)
                    {
                        Vector3 cellCenter = new Vector3(
                            transform.position.x + (x - (cellAmount.x - 1) / 2) * cellSize,
                            transform.position.y + (y - (cellAmount.y - 1) / 2) * cellSize,
                            transform.position.z + (z - (cellAmount.z - 1) / 2) * cellSize
                        );
        
                        Gizmos.DrawWireCube(cellCenter, Vector3.one / 10);
                    }
                }
            }
        }
    }
}
