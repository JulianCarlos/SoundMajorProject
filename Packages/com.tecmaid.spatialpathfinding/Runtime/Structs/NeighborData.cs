
using Unity.Collections;
using UnityEngine;

public unsafe struct NeighborData
{
    public int NeighborsCount;
    public fixed int Neighbors[6];

    public NeighborData(NativeArray<int> size)
    {
        NeighborsCount = 6;

        for (int i = 0; i < NeighborsCount; i++)
        {
            Neighbors[i] = size[i];
        }
    }
}