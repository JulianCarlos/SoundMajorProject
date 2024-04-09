using Pathfinding.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GetNeighborsSimpleJob : IJob
{
    [ReadOnly] public int TotalCells;
    [ReadOnly] public int VolumeWidth;
    [ReadOnly] public int VolumeHeight;
    [ReadOnly] public int VolumeDepth;

    [ReadOnly] public NativeArray<Cell> Cells;
    [ReadOnly] public NativeArray<bool> ObscuredCells;
    [ReadOnly] public NativeArray<int3> Directions;

    [WriteOnly] public NativeArray<NeighborData> CellNeighbors;
    
    public NativeArray<int> TempNeighbors;

    public void Execute()
    {
        for (int i = 0; i < TotalCells; i++)
        {
            GetNeighborsSimple(i);
        }
    }

    private void GetNeighborsSimple([AssumeRange(0, int.MaxValue)] int index)
    {
        int3 localIndex3D;
        int flattenIndex;

        for (int j = 0; j < 6; j++)
        {
            localIndex3D = Cells[index].Index3D + Directions[j];
            flattenIndex = CalculationHelper.FlattenIndex(localIndex3D, VolumeWidth, VolumeHeight);

            if (CalculationHelper.CheckIfIndexValid(localIndex3D, VolumeWidth, VolumeHeight, VolumeDepth) &&
                ObscuredCells[flattenIndex] == false)
            {
                TempNeighbors[j] = flattenIndex;
            }
            else
            {
                TempNeighbors[j] = -1;
            }
        }

        CellNeighbors[index] = new NeighborData(TempNeighbors);
    }
}
