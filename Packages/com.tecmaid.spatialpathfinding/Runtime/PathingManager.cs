using Unity.Collections;
using UnityEngine;
using System.Diagnostics;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using Pathfinding.Helpers;
using System.Collections;

namespace Pathfinding
{
    [DefaultExecutionOrder(100)]
    public class PathingManager : MonoBehaviour
    {
        #region Static Accessors

        /// The Main Instance of this object
        /// </summary>
        /// <remarks>
        /// Note: Only one <see cref="PathingManager"/> can exist at a time
        /// </remarks>
        public static PathingManager Instance { get; private set; }

        /// <summary>
        /// Action for Adding Agents
        /// </summary>
        /// <remarks>
        /// This Action gets executed every time the agent starts moving
        /// </remarks>
        public static Action<FlyingAgent> OnAgentStartedPathing { get; private set; }

        /// <summary>
        /// Action for Removing Agents
        /// </summary>
        /// <remarks>
        /// This Action gets executed every time a agent has reached his destination
        /// </remarks>
        public static Action<FlyingAgent> OnAgentFinishedPathing { get; private set; }

        #endregion

        #region Serialized Field Accessors

        /// <summary>
        /// The layer name of the agent layer
        /// </summary>
        public string AgentLayerName => agentLayerName;

        /// <summary>
        /// The layer name of the agent layer
        /// </summary>
        public string VolumeLayerName => volumeLayerName;

        /// <summary>
        /// The active layers responsible for the neighbor calculation
        /// </summary>
        public LayerMask DetectableLayer => detectableLayer;

        /// <summary>
        /// The generation method for generating the grid and calculating neighbors
        /// </summary>
        public GridGenerationMethod GridGenerationMethod => gridGenerationMethod;

        #endregion

        #region Serialized Fields

        /// <summary>
        /// The layer name of the agent layer
        /// </summary>
        [SerializeField] private string agentLayerName;

        /// <summary>
        /// The layer name of the agent layer
        /// </summary>
        [SerializeField] private string volumeLayerName;

        /// <summary>
        /// The active layers responsible for the neighbor calculation
        /// </summary>
        [SerializeField] private LayerMask detectableLayer = ~0;

        /// <summary>
        /// The generation method for generating the grid and calculating neighbors
        /// </summary>
        [SerializeField] private GridGenerationMethod gridGenerationMethod = GridGenerationMethod.Simple;

        /// <summary>
        /// The update mode for updating the grid during runtime
        /// </summary>
        [SerializeField] private GridUpdateMode gridUpdateMode = GridUpdateMode.Static;

        /// <summary>
        /// The layer name input for generating agent layer
        /// </summary>
        [SerializeField] private string agentLayerInput = "FlyingAgent";

        /// <summary>
        /// The layer name input for generating volume layer
        /// </summary>
        [SerializeField] private string volumeLayerInput = "NavigationVolume";

        /// <summary>
        /// The current agents inside the movable list
        /// </summary>
        /// <remarks>
        /// Everytime the 
        /// <see cref="CalculateAStarPath(FlyingAgent)"/> is completed, the agent will be added
        /// </remarks>
        [SerializeField] private List<FlyingAgent> movableAgents = new List<FlyingAgent>();

        /// <summary>
        /// The current agents inside the calculable list
        /// </summary>
        /// <remarks>
        /// Everytime the 
        /// <see cref="OnAgentStartedPathing"/> is executed, the agent will be added
        /// </remarks>
        [SerializeField] private List<FlyingAgent> calculableAgents = new List<FlyingAgent>();

        /// <summary>
        /// The current volumes inside the Scene
        /// </summary>
        [SerializeField] private NavigationVolume[] totalVolumes;

        /// <summary>
        /// Modifier for modifying the finished path
        /// </summary>
        [SerializeField] private Modifiers modifiers = Modifiers.NONE;

        /// <summary>
        /// The frequency of how often the <see cref="CalculateAllAgentPaths"/> gets called
        /// </summary>
        [SerializeField] private float calculationTimeStep = 0.2f;

        /// <summary>
        /// The frequency of how often the grid neighbors get updated
        /// </summary>
        [SerializeField] private float updateGridTimeStep = 0.5f;

        /// <summary>
        /// The amount of time it takes to move the agents
        /// </summary>
        /// <remarks>
        /// Note: this is only for debugging purposes
        /// </remarks>
        [SerializeField] private double movableExecutionTime = 0;

        /// <summary>
        /// The amount of time it takes to calculating the agents
        /// </summary>
        /// <remarks>
        /// Note: this is only for debugging purposes
        /// </remarks>
        [SerializeField] private double calculateExecutionTime = 0;

        #endregion

        #region Private Fields

        /// <summary>
        /// Stopwatch for measuring time to move the agents
        /// </summary>
        /// <remarks>
        /// Note: this is only for debugging purposes
        /// </remarks>
        private readonly Stopwatch moveExecutionStopwatch = new Stopwatch();

        /// <summary>
        /// Stopwatch for measuring time to calculate the agents
        /// </summary>
        /// <remarks>
        /// Note: this is only for debugging purposes
        /// </remarks>
        private readonly Stopwatch calculateExecutionStopwatch = new Stopwatch();

        /// <summary>
        /// Local List to concat multiple waypoints together
        /// </summary>
        private NativeList<NavigationPathSegment> wayPoints = new NativeList<NavigationPathSegment>(Allocator.Persistent);

        /// <summary>
        /// Cached Job for faster processing speed
        /// </summary>
        /// <remarks>
        /// Note: This is not the final approach
        /// </remarks>
        private AStarJob targetJob;

        /// <summary>
        /// Cached Job for faster processing speed
        /// </summary>
        /// <remarks>
        /// Note: This is not the final approach
        /// </remarks>
        private AStarJob originJob;

        /// <summary>
        /// Handle for scheduling <see cref="AStarJob"/>
        /// </summary>
        private JobHandle aStarHandle;

        /// <summary>
        /// Cached Waitforseconds to reduce GC
        /// </summary>
        private WaitForSeconds calculationTimeStepWait;

        /// <summary>
        /// Cached Waitforseconds to reduce GC
        /// </summary>
        private WaitForSeconds updateGridTimeStepWait;

        /// <summary>
        /// The volume of the agent requesting the path
        /// </summary>
        private NavigationVolume originVolume;

        /// <summary>
        /// The targetvolume of the agent requesting the path
        /// </summary>
        private NavigationVolume targetVolume;

        #endregion

        #region Default Execution Methods

        private void Awake()
        {
            CreateInstance();

            OnAgentStartedPathing += AddAgentToCalculation;
            OnAgentFinishedPathing += RemoveAgentFromMovable;

            calculationTimeStepWait = new WaitForSeconds(calculationTimeStep);
            updateGridTimeStepWait = new WaitForSeconds(updateGridTimeStep);
        }

        private void Start()
        {
            totalVolumes = FindObjectsOfType<NavigationVolume>();

            StartCoroutine(nameof(CalculateAllAgentPaths));
            StartCoroutine(nameof(UpdateAllVolumeCells));
        }

        private void Update()
        {
            MoveAllAgents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setting the Layer from the input field
        /// </summary>
        public void SetLayers()
        {
            agentLayerName = agentLayerInput;
            volumeLayerName = volumeLayerInput;
        }

        /// <summary>
        /// Getting the layernames currently inside the input fields
        /// </summary>
        /// <returns> <see cref="agentLayerInput"/> | <see cref="volumeLayerInput"/> </returns>
        public string[] GetInputLayer()
        {
            return new string[2] { agentLayerInput, volumeLayerInput };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method for creating <see cref="PathingManager"/> Instance
        /// </summary>
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

        /// <summary>
        /// Adds a agent to the calculation list
        /// </summary>
        /// <remarks>
        /// Note: this method is subscribed to the <see cref="OnAgentStartedPathing"/> method
        /// </remarks>
        /// <param name="agent"><see cref="FlyingAgent"/> requesting path</param>
        private void AddAgentToCalculation(FlyingAgent agent)
        {
            calculableAgents.Add(agent);
            movableAgents.Add(agent);
        }

        /// <summary>
        /// Removes a agent from the movable list
        /// </summary>
        /// <remarks>
        /// Note: this method is subscribed to the <see cref="OnAgentFinishedPathing"/> method
        /// </remarks>
        /// <param name="agent"><see cref="FlyingAgent"/> reached path</param>
        private void RemoveAgentFromMovable(FlyingAgent agent)
        {
            calculableAgents.Remove(agent);
            movableAgents.Remove(agent);
        }

        /// <summary>
        /// Responsible for calculating path of each <see cref="FlyingAgent"/> inside the <see cref="calculableAgents"/> list
        /// </summary>
        private IEnumerator CalculateAllAgentPaths()
        {
            while (true)
            {
                yield return calculationTimeStepWait;

                calculateExecutionStopwatch.Start();

                for (int i = 0; i < calculableAgents.Count; i++)
                {
                    if (!calculableAgents[i].IsTraversing)
                    {
                        CalculateAStarPath(calculableAgents[i]);

                        if (gridUpdateMode == GridUpdateMode.Static)
                            calculableAgents.RemoveAt(i);
                    }
                }

                calculateExecutionStopwatch.Stop();
                calculateExecutionTime = calculateExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
                calculateExecutionStopwatch.Reset();
            }
        }

        /// <summary>
        /// Responsible for calculating path of each <see cref="NavigationVolume"/> inside the <see cref="totalVolumes"/> list
        /// </summary>
        private IEnumerator UpdateAllVolumeCells()
        {
            while (true)
            {
                yield return updateGridTimeStepWait;

                for (int i = 0; i < totalVolumes.Length; i++)
                {
                    totalVolumes[i].UpdateCells();
                }
            }
        }

        /// <summary>
        /// Responsible for moving all <see cref="FlyingAgent"/> inside the <see cref="movableAgents"/> list
        /// </summary>
        private void MoveAllAgents()
        {
            if (movableAgents.Count <= 0)
                return;

            moveExecutionStopwatch.Start();

            for (int i = 0; i < movableAgents.Count; i++)
            {
                movableAgents[i].Move();
            }

            moveExecutionStopwatch.Stop();
            movableExecutionTime = moveExecutionStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
            moveExecutionStopwatch.Reset();
        }

        /// <summary>
        /// Calculate Path for <see cref="FlyingAgent"/>
        /// </summary>
        /// <param name="agent">Target <see cref="FlyingAgent"/> requesting path</param>
        private void CalculateAStarPath(FlyingAgent agent)
        {
            originVolume = agent.ActiveVolume;
            targetVolume = agent.ActiveVolume;

            int tempLinkIndex = -1;
            float tempDistance = float.MaxValue;
            float distance = CalculationHelper.CalculateSquaredDistance(originVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);
            List<NavigationSubLink> links = agent.ActiveVolume.Links;

            CalculateClosestVolumes(agent, ref tempLinkIndex, ref tempDistance, ref distance, links);
            GenerateWaypoints(agent, tempLinkIndex, links);

            if (this.modifiers == Modifiers.PATHSMOOTHING)
            {
                wayPoints = SmoothPath(wayPoints, originVolume);
            }

            agent.SetPath(new NavigationPath(wayPoints));
            wayPoints.Clear();
        }

        /// <summary>
        /// Responsible for Smoothing out the finished waypoints list
        /// </summary>
        /// <remarks>
        /// Note: This method increases performance slightly
        /// </remarks>
        /// <param name="waypoints">The target waypoints which should get smoothed</param>
        /// <param name="detectionRadius">The detection radius to determine if neighbors are in line of sight</param>
        /// <returns></returns>
        private NativeList<NavigationPathSegment> SmoothPath(NativeList<NavigationPathSegment> waypoints, NavigationVolume volume)
        {
            int index;
            float smoothDetection;
            NativeList<float3> newWaypoints = new NativeList<float3>(Allocator.Persistent);
            NativeList<NavigationPathSegment> smoothedSegments = new NativeList<NavigationPathSegment>(Allocator.Persistent);

            for (int i = 0; i < waypoints.Length; i++)
            {
                index = 0;
                smoothDetection = 0;

                if (gridGenerationMethod == GridGenerationMethod.Simple)
                    smoothDetection = volume.CellSize;
                else if (gridGenerationMethod == GridGenerationMethod.Directional)
                    smoothDetection = volume.GetDetectionRadius();

                newWaypoints.Add(waypoints[i].Waypoints[index]);

                for (int j = 1; j < waypoints[i].Waypoints.Length - 1; j++)
                {
                    if (Physics.BoxCast(waypoints[i].Waypoints[index], smoothDetection * Vector3.one, waypoints[i].Waypoints[j] - waypoints[i].Waypoints[index], Quaternion.identity, math.distance(waypoints[i].Waypoints[j], waypoints[i].Waypoints[index])))
                    {
                        newWaypoints.Add(waypoints[i].Waypoints[j - 1]);
                        index = j - 1;
                    }
                }

                newWaypoints.Add(waypoints[i].Waypoints[^1]);

                smoothedSegments.Add(new NavigationPathSegment(newWaypoints));

                newWaypoints.Clear();
            }

            return smoothedSegments;
        }

        /// <summary>
        /// Calculates the closest volume to the target position of the requesting <see cref="FlyingAgent"/>
        /// </summary>
        /// <param name="agent">Target <see cref="FlyingAgent"/> requesting path</param>
        /// <param name="tempLinkIndex">The index of the current link</param>
        /// <param name="tempDistance">The current distance calculated</param>
        /// <param name="distance">The current shortest distance</param>
        /// <param name="links">All links connect to the target <see cref="NavigationVolume"/></param>
        private void CalculateClosestVolumes(FlyingAgent agent, ref int tempLinkIndex, ref float tempDistance, ref float distance, List<NavigationSubLink> links)
        {
            for (int i = 0; i < links.Count; i++)
            {
                tempDistance = CalculationHelper.CalculateSquaredDistance(links[i].LinkedVolume.DetectionBox.ClosestPoint(agent.TargetPos), agent.TargetPos);

                if (links[i].RootLink.CheckTraverseAccess(links[i]) && tempDistance < distance)
                {
                    distance = tempDistance;

                    targetVolume = links[i].LinkedVolume;
                    tempLinkIndex = i;
                }
            }
        }

        /// <summary>
        /// Responsible for executing <see cref="AStarJob"/> and generating a waypoints list
        /// </summary>
        /// <param name="agent">Target <see cref="FlyingAgent"/> requesting path</param>
        /// <param name="tempLinkIndex"> Current active link index</param>
        /// <param name="links">All links connect to the target <see cref="NavigationVolume"/></param>
        private void GenerateWaypoints(FlyingAgent agent, int tempLinkIndex, List<NavigationSubLink> links)
        {
            if (originVolume == targetVolume)
            {
                targetJob = JobFactory.GenerateAStarJob(originVolume, agent.InitialPos, agent.TargetPos);
                aStarHandle = targetJob.Schedule();
                aStarHandle.Complete();

                if (targetJob.WalkPoints.Length > 0)
                    wayPoints.Add(new NavigationPathSegment(targetJob.WalkPoints));

                targetJob.WalkPoints.Dispose();
                targetJob.TempData.Dispose();
                targetJob.OpenCells.Dispose();
            }
            else
            {
                targetJob = JobFactory.GenerateAStarJob(targetVolume, links[tempLinkIndex].NeighborLink.transform.position, agent.TargetPos);
                aStarHandle = targetJob.Schedule();
                aStarHandle.Complete();

                if (targetJob.WalkPoints.Length > 0)
                    wayPoints.Add(new NavigationPathSegment(targetJob.WalkPoints));

                targetJob.WalkPoints.Dispose();
                targetJob.TempData.Dispose();
                targetJob.OpenCells.Dispose();

                originVolume = agent.ActiveVolume;

                originJob = JobFactory.GenerateAStarJob(originVolume, agent.InitialPos, links[tempLinkIndex].transform.position);
                aStarHandle = originJob.Schedule();
                aStarHandle.Complete();
 
                if (originJob.WalkPoints.Length > 0)
                    wayPoints.Add(new NavigationPathSegment(originJob.WalkPoints));

                originJob.WalkPoints.Dispose();
                originJob.TempData.Dispose();
                originJob.OpenCells.Dispose();
            }
        }

        #endregion
    }
}


