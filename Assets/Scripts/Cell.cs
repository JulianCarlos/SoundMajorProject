using Unity.Burst;
using Unity.Mathematics;

public struct Cell
{
    public int Index;
    public int ParentIndex;
    public float FCost;

    public float3 CellPos;

    public Cell(float3 cellPos, int index)
    {
        this.Index = index;
        this.ParentIndex = -1;
        this.CellPos = cellPos;
        FCost = -1;
    }

    public Cell(float3 cellPos, int index, int parentIndex, float FCost)
    {
        this.CellPos = cellPos;
        this.Index = index;
        this.ParentIndex = parentIndex;
        this.FCost = FCost;
    }
}
