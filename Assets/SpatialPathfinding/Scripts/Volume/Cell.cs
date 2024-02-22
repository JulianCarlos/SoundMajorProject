using Unity.Burst;
using Unity.Mathematics;

public struct Cell
{
    public readonly int Index;
    public readonly float3 CellPos;

    public Cell(float3 cellPos, int index)
    {
        this.Index = index;
        this.CellPos = cellPos;
    }
}
