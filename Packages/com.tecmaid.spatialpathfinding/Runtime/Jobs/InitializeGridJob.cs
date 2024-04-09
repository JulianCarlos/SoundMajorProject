using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct InitializeGridJob : IJob
{
    [WriteOnly] public NativeArray<Cell> Cells;

    [ReadOnly] public int VolumeDepth;
    [ReadOnly] public int VolumeHeight;
    [ReadOnly] public int VolumeWidth;

    [ReadOnly] public float cellSize;
    [ReadOnly] public Vector3 position;

    public void Execute()
    {
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        int index = 0;

        for (int z = 0; z < VolumeDepth; z++)
        {
            for (int y = 0; y < VolumeHeight; y++)
            {
                for (int x = 0; x < VolumeWidth; x++)
                {
                    Vector3 mainCellCenter = new Vector3(
                    position.x + ((x - (VolumeWidth - 1f) / 2f) * cellSize),
                    position.y + ((y - (VolumeHeight - 1f) / 2f) * cellSize),
                    position.z + ((z - (VolumeDepth - 1f) / 2f) * cellSize));

                    Cell cell = new Cell(mainCellCenter, index, new int3(x, y, z));
                    Cells[index] = cell;

                    index++;
                }
            }
        }
    }
}
