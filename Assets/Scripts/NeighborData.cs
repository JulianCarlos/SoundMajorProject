
using UnityEngine;

public unsafe struct NeighborData
{
    public readonly int NeighborsCount;
    public fixed int Neighbors[6]; // Use fixed size array

    public NeighborData(int[] size)
    {
        NeighborsCount = Mathf.Min(size.Length, 6); // Ensure not exceeding the fixed array size

        // Copy values from input array to the fixed array
        for (int i = 0; i < NeighborsCount; i++)
        {
            Neighbors[i] = size[i];
        }
    }
}