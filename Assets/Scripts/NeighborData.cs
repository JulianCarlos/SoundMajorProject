
public unsafe struct NeighborData
{
    public int NeighborsCount;
    public int[] Neighbors;

    public NeighborData(int[] size)
    {
        Neighbors = size;
        NeighborsCount = size.Length;
    }
}