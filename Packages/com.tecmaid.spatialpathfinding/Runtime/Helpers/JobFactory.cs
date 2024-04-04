using Unity.Collections;
using Unity.Mathematics;

namespace Pathfinding.Helpers
{
    public static class JobFactory
    {
        private static NavigationVolume targetVolume;

        public static AStarJob GenerateAStarJob(NavigationVolume volume, float3 initialPos, float3 targetPos, NativeList<float3> wayPoints)
        {
            targetVolume = volume;

            AStarJob job = new AStarJob()
            {
                TotalCells = targetVolume.TotalCells,

                VolumeWidth = targetVolume.VolumeWidth,
                VolumeHeight = targetVolume.VolumeHeight,
                VolumeDepth = targetVolume.VolumeDepth,

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
}

