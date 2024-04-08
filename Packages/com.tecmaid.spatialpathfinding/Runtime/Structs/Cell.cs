using Unity.Burst;
using Unity.Mathematics;

public readonly struct Cell
{
    public readonly int Index;
    public readonly int3 Index3D;
    public readonly float3 CellPos;

    public Cell(float3 cellPos, int index, int3 index3D)
    {
        this.Index = index;
        this.Index3D = index3D;
        this.CellPos = cellPos;
    }
}
