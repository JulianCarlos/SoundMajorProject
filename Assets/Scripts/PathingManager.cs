using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEditor.Progress;
using Debug = UnityEngine.Debug;

public class PathingManager : MonoBehaviour
{
    [SerializeField, Range(1, 5)] private int cellSize = 1;
    [SerializeField] private int3 cellAmount;

    [Space]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject playerObject;

    [Space]
    [SerializeField] private bool showGizmos = false;

    private int startingPoint;
    private int currentPoint;
    private int endPoint;

    private float3 playerPos;
    private float3 targetPos;

    private NativeArray<Cell> cells;
    private NativeArray<TempData> tempData;
    private NativeArray<int3> directions;

    private NativeList<int> openCells;
    private NativeList<float3> Walkpoints;

    private NeighborData[] cellNeighbors;

    private int totalCells;

    private void Awake()
    {
        totalCells = (int)(cellAmount.x * cellAmount.y * cellAmount.z);

        cellNeighbors = new NeighborData[totalCells];

        cells = new NativeArray<Cell>(totalCells, Allocator.Persistent);
        directions = new NativeArray<int3>(6, Allocator.Persistent);

        openCells = new NativeList<int>(Allocator.Persistent);
        Walkpoints = new NativeList<float3>(totalCells, Allocator.Persistent);

        InitializeDirections();
    }

    private void Start()
    {
        InitializeGrid();

        GetAllCellNeighbors();
    }

    private void Update()
    {
        AStar(playerObject.transform.position, targetObject.transform.position);
    }

    private void AStar(float3 player, float3 target)
    {
        var then = Time.realtimeSinceStartup;

        FindPoints(player, target);

        InitializeBuffers();

        MoveToTarget();

        SearchOrigin();

        ClearBuffers();

        Debug.Log("Finding Path: " + (Time.realtimeSinceStartup - then) * 1000f);
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
        tempData = new NativeArray<TempData>(totalCells, Allocator.TempJob);
        tempData[startingPoint] = new TempData(-1, 1000);

        //openCells = new(totalCells);
        openCells.Add(cells[startingPoint].Index);
        currentPoint = openCells[0];
    }

    private void InitializeDirections()
    {
        directions[0] = new int3(0, 0, 1);
        directions[1] = new int3(0, 0, -1);
        directions[2] = new int3(1, 0, 0);
        directions[3] = new int3(-1, 0 , 0);
        directions[4] = new int3(0, 1, 0);
        directions[5] = new int3(0, -1, 0);
    }

    private void MoveToTarget()
    {
        float cost;
        NeighborData neighborData;
        Cell neighborCell;

        while (currentPoint != endPoint && openCells.Length > 0)
        {
            currentPoint = openCells[0];

            neighborData = cellNeighbors[currentPoint];

            openCells.RemoveAt(0);

            for (int i = 0; i < neighborData.NeighborsCount; i++)
            {
                neighborCell = cells[neighborData.Neighbors[i]];

                if (tempData[neighborCell.Index].FCost > 0)
                    continue;
                
                cost = math.distance(neighborCell.CellPos, targetPos);

                tempData[neighborCell.Index] = new TempData(cells[currentPoint].Index, cost);

                openCells.Add(neighborCell.Index);
            }
        }
    }

    private void SearchOrigin()
    {
        while (tempData[currentPoint].ParentIndex != -1)
        {
            UnityEngine.Debug.DrawLine(cells[currentPoint].CellPos, cells[tempData[currentPoint].ParentIndex].CellPos, Color.green, 0.1f);
            Walkpoints.Add(cells[currentPoint].CellPos);
            currentPoint = tempData[currentPoint].ParentIndex;
        }
    }

    private void ClearBuffers()
    {
        Walkpoints.Clear();
        openCells.Clear();
        tempData.Dispose();
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

    private void GetAllCellNeighbors()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            GetNeighbours(cells[i].CellPos, i);
        }
    }

    private void GetNeighbours(float3 position, int index)
    {
        List<int> neighbors = new List<int>();

        for (int i = 0; i < directions.Length; i++)
        {
            if (!Physics.Raycast(position, Int3ToVector3(directions[i]), cellSize))
            {
                int targetCellIndex = FindNearestCell(position + (directions[i] * cellSize));
                neighbors.Add(targetCellIndex);
            }
        }

        cellNeighbors[index] = new NeighborData(neighbors.ToArray());
    }

    private float3 Int3ToVector3(int3 int3Direction)
    {
        return new float3(int3Direction.x, int3Direction.y, int3Direction.z);
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

        cells.Dispose();

        Debug.Log("Generating Grid: " + (Time.realtimeSinceStartup - then) * 1000f);
    }

    private void OnDestroy()
    {
        cells.Dispose();
        directions.Dispose();
        Walkpoints.Dispose();
        openCells.Dispose();
    }

    private void OnDrawGizmos()
    {
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
