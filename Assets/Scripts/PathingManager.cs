using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEditor.Progress;
using Debug = UnityEngine.Debug;

public unsafe class PathingManager : MonoBehaviour
{
    [SerializeField, Range(1, 15)] private int cellSize = 1;
    [SerializeField] private int3 cellAmount;

    [Space]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject playerObject;

    [SerializeField] private TextMeshProUGUI msText;

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

    private NativeArray<NeighborData> cellNeighbors;

    private int totalCells;

    private void Awake()
    {
        totalCells = (cellAmount.x * cellAmount.y * cellAmount.z);

        openCells = new NativeList<int>(Allocator.Persistent);
        Walkpoints = new NativeList<float3>(totalCells, Allocator.Persistent);

        cells = new NativeArray<Cell>(totalCells, Allocator.Persistent);
        directions = new NativeArray<int3>(6, Allocator.Persistent);

        cellNeighbors = new NativeArray<NeighborData>(totalCells, Allocator.Persistent);

        InitializeDirections();
    }

    private void Start()
    {
        InitializeGrid();

        GetAllCellNeighbors();

        StartCoroutine(AStar(playerObject.transform.position, targetObject.transform.position));
    }

    private IEnumerator AStar(float3 player, float3 target)
    {
        while (true)
        {
            var then = Time.realtimeSinceStartup;

            FindPoints(player, target);

            InitializeBuffers();

            MoveToTarget();

            SearchOrigin();

            ClearBuffers();

            msText.text = "ms: " + ((Time.realtimeSinceStartup - then) * 1000f);

            yield return new WaitForSeconds(0.1f);
        }
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
        NeighborData neighborData;
        Cell neighborCell;

        while (currentPoint != endPoint && openCells.Length > 0)
        {
            currentPoint = openCells[0];

            neighborData = cellNeighbors[currentPoint];

            openCells.RemoveAt(0);

            for (int i = 0; i < 6; i++)
            {
                int neighborIndex = neighborData.Neighbors[i];

                if (neighborIndex < 0 || neighborIndex >= totalCells)
                    continue;

                neighborCell = cells[neighborIndex];

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
            if (!Physics.Raycast(position, Int3ToVector3(directions[i]), cellSize))
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

    private float3 Int3ToVector3(int3 int3Direction)
    {
        return new float3(int3Direction.x, int3Direction.y, int3Direction.z);
    }

    private void InitializeGrid()
    {
        NativeArray<Cell> cells = new NativeArray<Cell>(totalCells, Allocator.TempJob);

        var job = new InitializeGridJob
        {
            TransformPosition = transform.position,
            CellAmount = cellAmount,
            CellSize = cellSize,
            Cells = cells,
        };

        JobHandle jobHandle = job.Schedule(totalCells, 200);

        jobHandle.Complete();

        this.cells.CopyFrom(cells);

        cells.Dispose();
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
                            transform.position.x + (x - (cellAmount.x - 1) * 0.5f) * cellSize,
                            transform.position.y + (y - (cellAmount.y - 1) * 0.5f) * cellSize,
                            transform.position.z + (z - (cellAmount.z - 1) * 0.5f) * cellSize
                        );
            
                        Gizmos.DrawWireCube(cellCenter, Vector3.one * 0.1f);
                    }
                }
            }
        }
    }
}
