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
        public int TotalCores { get; private set; }
        public int TotalCellsPerCore { get; private set; }

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
        [SerializeField] private Color coreColor = new Color(0f, 0f, 1f, 1f);
        [SerializeField] private Color cellColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] private Color detectionColor = new Color(1f, 0f, 0f, 1f);
        [Space]
        [SerializeField] private double miliseconds = 0;

        public NativeArray<Cell> Cells;
        public NativeArray<GridCore> Cores;
        public NativeArray<NeighborData> CellNeighbors;

        private NativeArray<int3> directions = new NativeArray<int3>(6, Allocator.Persistent);

        private RaycastHit directionHit;

        private int directionCount = 0;

        private int indexX = 0;
        private int indexY = 0;
        private int indexZ = 0;

        private int rowXLength = 0;
        private int rowYLength = 0;
        private int rowZLength = 0;

        private void Awake()
        {
            TotalCells = (int)(cellAmount.x * amountOfCellsPerMainCell * cellAmount.y * amountOfCellsPerMainCell * cellAmount.z * amountOfCellsPerMainCell);
            TotalCores = (int)(cellAmount.x * cellAmount.y * cellAmount.z);
            TotalCellsPerCore = (int)(amountOfCellsPerMainCell * amountOfCellsPerMainCell * amountOfCellsPerMainCell);

            Cells = new NativeArray<Cell>(TotalCells, Allocator.Persistent);
            Cores = new NativeArray<GridCore>(TotalCores, Allocator.Persistent);
            CellNeighbors = new NativeArray<NeighborData>(TotalCells, Allocator.Persistent);

            VolumeWidth = (int)(cellAmount.x * amountOfCellsPerMainCell);
            VolumeHeight = (int)(cellAmount.y * amountOfCellsPerMainCell);
            VolumeDepth = (int)(cellAmount.z * amountOfCellsPerMainCell);
        }

        private void Start()
        {
            Stopwatch calculateExecutionStopwatch = new Stopwatch();
            calculateExecutionStopwatch.Start();

            InitializeDirections();

            InitializeCoreGrid();

            GetAllCellNeighbors();

            DetectionBox = GetComponent<BoxCollider>();

            Collider[] overlappedAgents = Physics.OverlapBox(transform.position, new Vector3((cellSize * amountOfCellsPerMainCell * cellAmount.x) / 2, (cellSize * amountOfCellsPerMainCell * cellAmount.y) / 2, (cellSize * amountOfCellsPerMainCell * cellAmount.z) / 2), Quaternion.identity, LayerMask.GetMask("FlyingAgent"));
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

            //int index = FindNearestCell(new float3(0f, 0f, 0f));
            //Debug.Log(index);
            //Debug.DrawLine(Cells[index].CellPos, (Vector3)Cells[index].CellPos + Vector3.up * 5f, Color.cyan, 155f);

            //Debug.Log(CalculationHelper.FlattenIndex(new int3(3, 5, 11), VolumeWidth, VolumeHeight, VolumeDepth));
        }

        private void GetNeighbours(int index)
        {
            float3 position = Cells[index].CellPos;
            NativeArray<int> neighbors = new NativeArray<int>(directionCount, Allocator.Temp);

            for (int i = 0; i < directionCount; i++)
            {
                if (!Physics.BoxCast(position, Vector3.one * detectionRadius, CalculationHelper.Int3ToFloat3(directions[i]), out directionHit, transform.rotation,cellSize))
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
            float distanceX = float.MaxValue;
            float distanceY = float.MaxValue;
            float distanceZ = float.MaxValue;

            float tempDistance = 0;

            for (int x = 0; x < VolumeWidth; x++)
            {
                int index = CalculationHelper.FlattenIndex(new int3(x, 0, 0), VolumeWidth, VolumeHeight, VolumeDepth);
                tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);
                
                if (tempDistance < distanceX)
                {
                    distanceX = tempDistance;
                    indexX = x;
                }
            }
            for (int y = 0; y < VolumeHeight; y++)
            {
                int index = CalculationHelper.FlattenIndex(new int3(0, y, 0), VolumeWidth, VolumeHeight, VolumeDepth);
                tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);
                
                if (tempDistance < distanceY)
                {
                    distanceY = tempDistance;
                    indexY = y;
                }
            }
            for (int z = 0; z < VolumeDepth; z++)
            {
                int index = CalculationHelper.FlattenIndex(new int3(0, 0, z), VolumeWidth, VolumeHeight, VolumeDepth);
                tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[index].CellPos, position);
                
                if (tempDistance < distanceZ)
                {
                    distanceZ = tempDistance;
                    indexZ = z;
                }
            }

            return CalculationHelper.FlattenIndex(new int3(indexX, indexY, indexZ), VolumeWidth, VolumeHeight, VolumeDepth);
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
                                Gizmos.DrawWireCube(mainCellCenter, Vector3.one * detectionRadius);
                            }

                            if (visualMode == VisualMode.CellsOnly || visualMode == VisualMode.All)
                            {
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
