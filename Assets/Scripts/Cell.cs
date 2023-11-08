using Unity.Burst;
using Unity.Mathematics;

public struct Cell
{
    public int Index;

    public float3 CellPos;

    public Cell(float3 cellPos, int index)
    {
        this.Index = index;
        this.CellPos = cellPos;
    }
}
