using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(150)]
    [RequireComponent(typeof(BoxCollider))]
    public class NavigationVolume : MonoBehaviour
    {
        public int TotalCells { get; private set; }
        public int TotalCores { get; private set; }
        public int TotalCellsPerCore { get; private set; }

        [SerializeField] private uint cellSize = 1;
        [SerializeField] private uint amountOfCellsPerMainCell = 5;
        [SerializeField] private uint3 cellAmount = new uint3(3, 3, 3);
        [Space]
        [SerializeField] private float detectionRadius = 1f;
        [Space]
        [SerializeField] private VisualMode visualMode = VisualMode.None;
        [Space]
        [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);
        [SerializeField] private Color coreColor = new Color(0f, 0f, 1f, 1f);
        [SerializeField] private Color cellColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] private Color detectionColor = new Color(1f, 0f, 0f, 1f);

        public NativeArray<Cell> Cells;
        public NativeArray<GridCore> Cores;
        public NativeArray<NeighborData> CellNeighbors;

        private NativeArray<int3> directions = new NativeArray<int3>(6, Allocator.Persistent);
        private int directionCount = 0;

        private RaycastHit directionHit;

        [SerializeField] private double miliseconds = 0;

        public List<NavigationSubLink> Links = new List<NavigationSubLink>();

        public BoxCollider DetectionBox { get; private set; }

        private void Awake()
        {
            TotalCells = (int)(cellAmount.x * amountOfCellsPerMainCell * cellAmount.y * amountOfCellsPerMainCell * cellAmount.z * amountOfCellsPerMainCell);
            TotalCores = (int)(cellAmount.x * cellAmount.y * cellAmount.z);
            TotalCellsPerCore = (int)(amountOfCellsPerMainCell * amountOfCellsPerMainCell * amountOfCellsPerMainCell);

            Cells = new NativeArray<Cell>(TotalCells, Allocator.Persistent);
            Cores = new NativeArray<GridCore>(TotalCores, Allocator.Persistent);
            CellNeighbors = new NativeArray<NeighborData>(TotalCells, Allocator.Persistent);
        }

        private void Start()
        {
            InitializeDirections();
            InitializeGrid();
            GetAllCellNeighbors();

            DetectionBox = GetComponent<BoxCollider>();

            Collider[] overlappedAgents = Physics.OverlapBox(transform.position, new Vector3((cellSize * amountOfCellsPerMainCell * cellAmount.x) / 2, (cellSize * amountOfCellsPerMainCell * cellAmount.y) / 2, (cellSize * amountOfCellsPerMainCell * cellAmount.z) / 2), Quaternion.identity, LayerMask.GetMask("FlyingAgent"));
            for (int i = 0; i < overlappedAgents.Length; i++)
            {
                overlappedAgents[i].GetComponent<FlyingAgent>().SetActiveVolume(this);
            }
        }

        private void InitializeDirections()
        {
            //Horizontal
            directions[0] = new int3(0, 0, 1);
            directions[1] = new int3(0, 0, -1);
            directions[2] = new int3(1, 0, 0);
            directions[3] = new int3(-1, 0, 0);

            //Vertical
            directions[4] = new int3(0, 1, 0);
            directions[5] = new int3(0, -1, 0);

            directionCount = directions.Count();
        }

        private void GetAllCellNeighbors()
        {
            for (int i = 0; i < TotalCells; i++)
            {
                GetNeighbours(Cells[i].CellPos, i);
            }
        }

        private void GetNeighbours(float3 position, int index)
        {
            NativeArray<int> neighbors = new NativeArray<int>(directionCount, Allocator.Temp);

            for (int i = 0; i < directionCount; i++)
            {
                if (!Physics.BoxCast(position, Vector3.one * detectionRadius, CalculationHelper.Int3ToVector3(directions[i]), out directionHit, transform.rotation,cellSize))
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

        public void InitializeGrid()
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
                        transform.position.x + ((x - (cellAmount.x - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.y + ((y - (cellAmount.y - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.z + ((z - (cellAmount.z - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell);

                        tempSubCells = new();

                        for (int a = 0; a < amountOfCellsPerMainCell; a++)
                        {
                            for (int b = 0; b < amountOfCellsPerMainCell; b++)
                            {
                                for (int c = 0; c < amountOfCellsPerMainCell; c++)
                                {
                                    float3 subcellCenter = new float3(
                                        mainCellCenter.x + (a - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.y + (b - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.z + (c - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize
                                    );

                                    Cell cell = new Cell(subcellCenter, index);

                                    Cells[index] = (cell);
                                    tempSubCells.Add(cell.Index);

                                    index++;
                                }
                            }
                        }

                        GridCore core = new GridCore(mainCellCenter, tempSubCells.ToArray());
                        Cores[coreIndex] = (core);
                        coreIndex++;
                    }
                }
            }
        }

        private void OnValidate()
        {
            GetComponent<BoxCollider>().size = new Vector3(cellAmount.x, cellAmount.y, cellAmount.z) * amountOfCellsPerMainCell * cellSize;
        }

        private void OnDisable()
        {
            directions.Dispose();
            Cores.Dispose();
        }

        private void OnTriggerEnter(Collider other)
        {
            FlyingAgent targetAgent = other.gameObject.GetComponent<FlyingAgent>();
        
            if (targetAgent != null)
            {
                targetAgent.SetActiveVolume(this);
            }
        }

        private void OnDrawGizmos()
        {
            if (visualMode == VisualMode.None)
                return;

            if (visualMode == VisualMode.VolumeOnly || visualMode == VisualMode.All)
            {
                Gizmos.color = volumeColor;
                Gizmos.DrawCube(transform.position, new Vector3(cellAmount.x, cellAmount.y, cellAmount.z) * amountOfCellsPerMainCell * cellSize);
            }

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

                        if (visualMode == VisualMode.CoresOnly || visualMode == VisualMode.All)
                        {
                            Gizmos.color = coreColor;
                            Gizmos.DrawWireCube(mainCellCenter, Vector3.one);
                        }

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

                                    if (visualMode == VisualMode.Detection || visualMode == VisualMode.All)
                                    {
                                        Gizmos.color = detectionColor;
                                        Gizmos.DrawWireCube(subcellCenter, Vector3.one * detectionRadius * 2);
                                    }
                                    if (visualMode == VisualMode.CellsOnly || visualMode == VisualMode.All)
                                    {
                                        Gizmos.color = cellColor;
                                        Gizmos.DrawWireCube(subcellCenter, Vector3.one * cellSize);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public enum VisualMode
    {
        None,
        All,
        Detection,
        Neighbors,
        VolumeOnly,
        CoresOnly,
        CellsOnly,
    }
}
