using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct InitializeGridJob : IJobParallelFor
{
    public float3 TransformPosition;
    public int3 CellAmount;
    public float CellSize;
    public NativeArray<Cell> Cells;

    public void Execute(int index)
    {
        int3 cellPos = new int3(index % CellAmount.x, (index / CellAmount.x) % CellAmount.y, index / (CellAmount.x * CellAmount.y));

        float3 cellCenter = new float3(
            TransformPosition.x + (cellPos.x - (CellAmount.x - 1) / 2) * CellSize,
            TransformPosition.y + (cellPos.y - (CellAmount.y - 1) / 2) * CellSize,
            TransformPosition.z + (cellPos.z - (CellAmount.z - 1) / 2) * CellSize
        );

        Cell cell = new Cell(cellCenter, index);
        Cells[index] = cell;
    }
}