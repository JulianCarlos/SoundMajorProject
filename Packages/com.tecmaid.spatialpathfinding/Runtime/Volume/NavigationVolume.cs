using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Helpers;
using System.Diagnostics;
using Unity.Jobs;
using Debug = UnityEngine.Debug;
using UnityEditor;

namespace Pathfinding
{
    [DefaultExecutionOrder(150)]
    [RequireComponent(typeof(BoxCollider))]
    public class NavigationVolume : MonoBehaviour
    {
        public int TotalCells { get; private set; }

        public int VolumeWidth { get; private set; }
        public int VolumeHeight { get; private set; }
        public int VolumeDepth { get; private set; }

        public BoxCollider DetectionBox { get; private set; }
        public List<NavigationSubLink> Links = new List<NavigationSubLink>();

        [SerializeField] private uint cellSize = 2;
        [SerializeField] private uint amountOfCellsPerMainCell = 5;
        [SerializeField] private uint3 cellAmount = new uint3(3, 3, 3);
        [Space]
        [SerializeField] private float detectionRadius = 2f;
        [Space]
        [SerializeField] private VisualMode visualMode = VisualMode.VolumeOnly;
        [Space]
        [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);
        [SerializeField] private Color cellColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] private Color detectionColor = new Color(1f, 0f, 0f, 1f);
        [Space]
        [SerializeField] private double miliseconds = 0;

        public NativeArray<Cell> Cells;
        public NativeArray<GridCore> Cores;
        public NativeArray<NeighborData> CellNeighbors;

        private NativeArray<int3> directions = new NativeArray<int3>(6, Allocator.Persistent);

        private RaycastHit directionHit;

        private int directionCount = 6;

        private void Awake()
        {
            VolumeWidth = (int)(cellAmount.x * amountOfCellsPerMainCell);
            VolumeHeight = (int)(cellAmount.y * amountOfCellsPerMainCell);
            VolumeDepth = (int)(cellAmount.z * amountOfCellsPerMainCell);

            TotalCells = VolumeWidth * VolumeHeight * VolumeDepth;

            Cells = new NativeArray<Cell>(TotalCells, Allocator.Persistent);
            CellNeighbors = new NativeArray<NeighborData>(TotalCells, Allocator.Persistent);
        }

        private void Start()
        {
            Stopwatch calculateExecutionStopwatch = new Stopwatch();
            calculateExecutionStopwatch.Start();

            InitializeDirections();

            InitializeCoreGrid();

            GetAllCellNeighbors();

            DetectionBox = GetComponent<BoxCollider>();

            Collider[] overlappedAgents = Physics.OverlapBox(transform.position, new Vector3((cellSize * VolumeWidth) / 2, (cellSize * VolumeHeight) / 2, (cellSize * VolumeDepth) / 2), Quaternion.identity, LayerMask.GetMask("FlyingAgent"));
            for (int i = 0; i < overlappedAgents.Length; i++)
            {
                overlappedAgents[i].GetComponent<FlyingAgent>().SetActiveVolume(this);
            }

            calculateExecutionStopwatch.Stop();
            UnityEngine.Debug.Log(calculateExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency));
        }

        public float GetDetectionRadius()
        {
            return detectionRadius;
        }

        private void InitializeDirections()
        {
            //Horizontal
            directions[0] = new int3( 0,  0,  1);
            directions[1] = new int3( 0,  0, -1);
            directions[2] = new int3( 1,  0,  0);
            directions[3] = new int3(-1,  0,  0);

            //Vertical
            directions[4] = new int3( 0,  1,  0);
            directions[5] = new int3( 0, -1,  0);

            directionCount = directions.Count();
        }

        private void GetAllCellNeighbors()
        {
            for (int i = 0; i < TotalCells; i++)
            {
                GetNeighbours(i);
            }
        }

        private void GetNeighbours(int index)
        {
            float3 position = Cells[index].CellPos;
            NativeArray<int> neighbors = new NativeArray<int>(directionCount, Allocator.Temp);

            for (int i = 0; i < directionCount; i++)
            {
                if (CalculationHelper.CheckIfIndexValid(Cells[index].Index3D + directions[i], VolumeWidth, VolumeHeight, VolumeDepth) &&
                    !Physics.BoxCast(position, Vector3.one * detectionRadius, CalculationHelper.Int3ToFloat3(directions[i]), out directionHit, transform.rotation, cellSize))
                {
                    int targetCellIndex = CalculationHelper.FlattenIndex(Cells[index].Index3D + directions[i], VolumeWidth, VolumeHeight);
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

        public void InitializeCoreGrid()
        {
            int index = 0;

            for (int z = 0; z < cellAmount.z * amountOfCellsPerMainCell; z++)
            {
                for (int y = 0; y < cellAmount.y * amountOfCellsPerMainCell; y++)
                {
                    for (int x = 0; x < cellAmount.x * amountOfCellsPerMainCell; x++)
                    {
                        Vector3 mainCellCenter = new Vector3(
                        transform.position.x + ((x - (cellAmount.x * amountOfCellsPerMainCell - 1f) / 2f) * cellSize),
                        transform.position.y + ((y - (cellAmount.y * amountOfCellsPerMainCell - 1f) / 2f) * cellSize),
                        transform.position.z + ((z - (cellAmount.z * amountOfCellsPerMainCell - 1f) / 2f) * cellSize));
                        
                        Cell cell = new Cell(mainCellCenter, index, new int3(x, y, z));
                        Cells[index] = cell;

                        index++;
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

            if (visualMode == VisualMode.All || visualMode == VisualMode.VolumeOnly)
            {
                Gizmos.color = volumeColor;
                Gizmos.DrawCube(transform.position, (new Vector3(cellAmount.x, cellAmount.y, cellAmount.z) * amountOfCellsPerMainCell) * cellSize);
            }

            if (visualMode == VisualMode.All || visualMode == VisualMode.Detection || visualMode == VisualMode.CellsOnly)
            {
                Gizmos.color = Color.red;

                int index = 0;

                for (int z = 0; z < cellAmount.z * amountOfCellsPerMainCell; z++)
                {
                    for (int y = 0; y < cellAmount.y * amountOfCellsPerMainCell; y++)
                    {
                        for (int x = 0; x < cellAmount.x * amountOfCellsPerMainCell; x++)
                        {
                            Vector3 mainCellCenter = new Vector3(
                            transform.position.x + ((x - (cellAmount.x * amountOfCellsPerMainCell - 1f) / 2f) * cellSize),
                            transform.position.y + ((y - (cellAmount.y * amountOfCellsPerMainCell - 1f) / 2f) * cellSize),
                            transform.position.z + ((z - (cellAmount.z * amountOfCellsPerMainCell - 1f) / 2f) * cellSize));

                            if (visualMode == VisualMode.Detection || visualMode == VisualMode.All)
                            {
                                Gizmos.color = detectionColor;
                                Gizmos.DrawWireCube(mainCellCenter, Vector3.one * detectionRadius);
                            }

                            if (visualMode == VisualMode.CellsOnly || visualMode == VisualMode.All)
                            {
                                Gizmos.color = cellColor;
                                Gizmos.DrawWireCube(mainCellCenter, Vector3.one * cellSize);
                            }
                            //Handles.Label(mainCellCenter, $"{new int3(x, y, z)} \n {index}");

                            index++;
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
        VolumeOnly,
        CellsOnly,
    }
}
