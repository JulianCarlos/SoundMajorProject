using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Codice.Client.Common.WebApi.WebApiEndpoints;

public static class JobFactory
{
    private static NavigationVolume targetVolume;

    public static AStarJob GenerateAStarJob(NavigationVolume volume, float3 initialPos, float3 targetPos, NativeList<float3> wayPoints)
    {
        targetVolume = volume;

        AStarJob job = new AStarJob()
        {
            TotalCells = targetVolume.TotalCells,
            TotalCellsPerCore = targetVolume.TotalCellsPerCore,
            TotalCores = targetVolume.TotalCores,

            Cores = targetVolume.Cores,
            Cells = targetVolume.Cells,
            CellNeighbors = targetVolume.CellNeighbors,

            InitialPos = initialPos,
            TargetPos = targetPos,

            TempData = new NativeArray<TempData>(targetVolume.TotalCells, Allocator.TempJob),
            OpenCells = new NativeArray<int>(targetVolume.TotalCells, Allocator.TempJob),
            WalkPoints = wayPoints,
        };

        return job;
    }
}
