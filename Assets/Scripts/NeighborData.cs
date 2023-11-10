
using UnityEngine;

public unsafe struct NeighborData
{
    public readonly int NeighborsCount;
    public fixed int Neighbors[6];

    public NeighborData(int[] size)
    {
        NeighborsCount = 6;

        for (int i = 0; i < NeighborsCount; i++)
        {
            Neighbors[i] = size[i];
        }
    }
}