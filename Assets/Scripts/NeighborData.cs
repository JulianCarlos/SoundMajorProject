
public unsafe struct NeighborData
{
    public readonly int NeighborsCount;
    public readonly int[] Neighbors;

    public NeighborData(int[] size)
    {
        Neighbors = size;
        NeighborsCount = size.Length;
    }
}