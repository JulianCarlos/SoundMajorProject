using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(150)]
    [RequireComponent(typeof(BoxCollider))]
    public class NavigationVolume : MonoBehaviour
    {
        [SerializeField] private uint cellSize = 1;
        [SerializeField] private uint amountOfCellsPerMainCell = 5;
        [SerializeField] private uint3 cellAmount = new uint3(3, 3, 3);
        [Space]
        [SerializeField] private VisualMode visualMode = VisualMode.None;
        [Space]
        [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);
        [SerializeField] private Color coreColor = new Color(0f, 0f, 1f, 1f);
        [SerializeField] private Color cellColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] private Color detectionColor = new Color(1f, 0f, 0f, 1f);

        public float DetectionRadius = 1f;

        public GridCore[] cores;
        public NativeList<Cell> cells;
        public NativeArray<NeighborData> cellNeighbors;

        public int totalCells;
        public int totalCores;
        public int totalCellsPerCore;

        private void Awake()
        {
            totalCells = ((int)((cellAmount.x * amountOfCellsPerMainCell) * (cellAmount.y * amountOfCellsPerMainCell) * (cellAmount.z * amountOfCellsPerMainCell)));

            cells = new NativeList<Cell>(totalCells, Allocator.Persistent);
            cores = new GridCore[cellAmount.x * cellAmount.y * cellAmount.z];

            cellNeighbors = new NativeArray<NeighborData>(totalCells, Allocator.Persistent);
        }

        private void Start()
        {
            InitializeGrid();

            totalCores = cores.Count();
            totalCellsPerCore = (int)(amountOfCellsPerMainCell * amountOfCellsPerMainCell * amountOfCellsPerMainCell);

            GetAllCellNeighbors();
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
            NativeArray<int> neighbors = new NativeArray<int>(PathingManager.Instance.Directions.Count(), Allocator.Temp);

            for (int i = 0; i < neighbors.Count(); i++)
            {
                if (!Physics.SphereCast(position, DetectionRadius, CalculationHelper.Int3ToVector3(PathingManager.Instance.Directions[i]), out RaycastHit hit, cellSize))
                {
                    int targetCellIndex = FindNearestCell(position + (PathingManager.Instance.Directions[i] * (int)cellSize));

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

        private void OnValidate()
        {
            GetComponent<BoxCollider>().size = new Vector3(cellAmount.x, cellAmount.y, cellAmount.z) * amountOfCellsPerMainCell * cellSize;
        }

        private void OnTriggerEnter(Collider other)
        {
            FlyingAgent targetAgent = other.gameObject.GetComponent<FlyingAgent>();

            if (targetAgent != null)
            {
                targetAgent.AddActiveVolume(this);
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
                                        Gizmos.DrawWireCube(subcellCenter, Vector3.one * DetectionRadius * 2);
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
