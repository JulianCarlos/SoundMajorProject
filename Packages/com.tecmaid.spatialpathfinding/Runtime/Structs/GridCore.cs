using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public struct GridCore
{
    public float3 CorePos;
    public UnsafeList<int> SubCells;
    
    public GridCore(float3 corePos, NativeArray<int> subCells)
    {
        CorePos = corePos;
        SubCells = new UnsafeList<int>(subCells.Length, Allocator.Persistent);

        for (int i = 0; i < subCells.Length; i++)
        {
            SubCells.Add(subCells[i]);
        }
    }
}
