using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Helpers;
using System.Diagnostics;
using Unity.Jobs;
using Unity.Profiling;

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

        public uint CellSize => cellSize;

        public NativeArray<Cell> Cells;
        public NativeArray<NeighborData> CellNeighbors;

        public BoxCollider DetectionBox { get; private set; }
        public List<NavigationSubLink> Links = new List<NavigationSubLink>();

        [SerializeField] private uint3 gridSize = new uint3(25, 8, 25);
        [SerializeField] private uint cellSize = 3;

        [SerializeField] private float detectionRadius = 2f;

        [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);
        [SerializeField] private Color cellColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] private Color detectionColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private VisualMode visualMode = VisualMode.VolumeOnly;

        [SerializeField] private double generationTime = 0;

        private NativeArray<bool> obscuredCells;
        private NativeArray<int3> directions = new NativeArray<int3>(6, Allocator.Persistent);
        private NativeArray<int> tempNeighbors = new NativeArray<int>(6, Allocator.Persistent);
        private LayerMask detectionMask;
        private RaycastHit directionHit;

        private short directionCount;

        private void Awake()
        {
            VolumeWidth = (int) gridSize.x;
            VolumeHeight = (int) gridSize.y;
            VolumeDepth = (int) gridSize.z;

            TotalCells = VolumeWidth * VolumeHeight * VolumeDepth;

            Cells = new NativeArray<Cell>(TotalCells, Allocator.Persistent);
            CellNeighbors = new NativeArray<NeighborData>(TotalCells, Allocator.Persistent);
            obscuredCells = new NativeArray<bool>(TotalCells, Allocator.Persistent);

            detectionMask = PathingManager.Instance.DetectableLayer;
            DetectionBox = GetComponent<BoxCollider>();
            
            CollectAgents();
        }

        private void Start()
        {
            Stopwatch calculateExecutionStopwatch = new Stopwatch();
            calculateExecutionStopwatch.Start();

            InitializeDirections();
            InitializeGrid();

            if (PathingManager.Instance.GridGenerationMethod == GridGenerationMethod.Simple)
            {
                CheckObscuredCells();
                GetAllCellNeighborsSimple();
            }
            else if (PathingManager.Instance.GridGenerationMethod == GridGenerationMethod.Directional)
            {
                GetAllCellNeighborsDirectional();
            }

            calculateExecutionStopwatch.Stop();
            UnityEngine.Debug.Log(calculateExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency));
        }

        public float GetDetectionRadius()
        {
            return detectionRadius;
        }

        public void CollectAgents()
        {
            int layerIndex = LayerMask.GetMask(PathingManager.Instance.AgentLayerName);

            Collider[] overlappedAgents = Physics.OverlapBox(transform.position, cellSize * 0.5f * new Vector3(VolumeWidth,VolumeHeight,VolumeDepth) , Quaternion.identity, layerIndex);
            for (int i = 0; i < overlappedAgents.Length; i++)
            {
                overlappedAgents[i].GetComponent<FlyingAgent>().SetActiveVolume(this);
            }
        }

        private void CheckObscuredCells()
        {
            DetectionBox.enabled = false;

            for (int i = 0; i < TotalCells; i++)
            {
                obscuredCells[i] = Physics.CheckBox(Cells[i].CellPos, 0.5f * cellSize * Vector3.one, Quaternion.identity, detectionMask);
            }

            DetectionBox.enabled = true;
        }

        private void GetAllCellNeighborsSimple()
        {
            GetNeighborsSimpleJob job = new GetNeighborsSimpleJob()
            {
                Cells = this.Cells,
                ObscuredCells = this.obscuredCells,
                Directions = this.directions,

                CellNeighbors = this.CellNeighbors,

                VolumeWidth = this.VolumeWidth,
                VolumeHeight = this.VolumeHeight,
                VolumeDepth = this.VolumeDepth,

                TotalCells = this.TotalCells,

                TempNeighbors = this.tempNeighbors,
            };

            JobHandle handle = job.Schedule();

            handle.Complete();
        }

        private void GetAllCellNeighborsDirectional()
        {
            for (int i = 0; i < TotalCells; i++)
            {
                GetNeighboursDirectional(i, Cells[i].CellPos);
            }
        }

        private void GetNeighboursDirectional(int index, float3 position)
        {
            for (int i = 0; i < directionCount; i++)
            {
                if (CalculationHelper.CheckIfIndexValid(Cells[index].Index3D + directions[i], VolumeWidth, VolumeHeight, VolumeDepth) &&
                    !Physics.BoxCast(position, detectionRadius * Vector3.one, CalculationHelper.Int3ToFloat3(directions[i]), out directionHit, transform.rotation, cellSize, detectionMask))
                {
                    tempNeighbors[i] = CalculationHelper.FlattenIndex(Cells[index].Index3D + directions[i], VolumeWidth, VolumeHeight);
                } 
                else
                {
                    tempNeighbors[i] = -1;
                }
            }

            CellNeighbors[index] = new NeighborData(tempNeighbors);
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

            directionCount = (short)directions.Count();
        }

        private void InitializeGrid()
        {
            InitializeGridJob job = new InitializeGridJob()
            {
                Cells = this.Cells,
                VolumeDepth = this.VolumeDepth,
                VolumeHeight = this.VolumeHeight, 
                VolumeWidth = this.VolumeWidth,
                cellSize = this.cellSize,
                position = transform.position,
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            Cells = job.Cells;
        }

        private void OnValidate()
        {
            GetComponent<BoxCollider>().size = cellSize * new Vector3(gridSize.x, gridSize.y, gridSize.z);
        }

        private void OnDisable()
        {
            Cells.Dispose();
            directions.Dispose();
            CellNeighbors.Dispose();
            tempNeighbors.Dispose();
        }

        private void OnTriggerEnter(Collider other)
        {
            FlyingAgent targetAgent = other.gameObject.GetComponent<FlyingAgent>();
        
            if (targetAgent != null)
            {
                targetAgent.SetActiveVolume(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            FlyingAgent targetAgent = other.gameObject.GetComponent<FlyingAgent>();

            if (targetAgent != null)
            {
                targetAgent.SetActiveVolume(null);
            }
        }

        private void OnDrawGizmos()
        {
            if (visualMode == VisualMode.None)
                return;

            if (visualMode == VisualMode.All || visualMode == VisualMode.VolumeOnly)
            {
                Gizmos.color = volumeColor;
                Gizmos.DrawCube(transform.position, new Vector3(gridSize.x, gridSize.y, gridSize.z) * cellSize);
            }

            if (visualMode == VisualMode.All || visualMode == VisualMode.Detection || visualMode == VisualMode.CellsOnly)
            {
                Gizmos.color = Color.red;

                int index = 0;

                for (int z = 0; z < gridSize.z; z++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        for (int x = 0; x < gridSize.x; x++)
                        {
                            Vector3 mainCellCenter = new Vector3(
                            transform.position.x + ((x - (gridSize.x - 1f) / 2f) * cellSize),
                            transform.position.y + ((y - (gridSize.y - 1f) / 2f) * cellSize),
                            transform.position.z + ((z - (gridSize.z - 1f) / 2f) * cellSize));

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
