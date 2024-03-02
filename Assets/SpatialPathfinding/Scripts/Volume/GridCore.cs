using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct GridCore
{
    public float3 CorePos;
    public NativeArray<int> SubCells;

    public GridCore(float3 corePos, int[] subCells)
    {
        CorePos = corePos;
        SubCells = new NativeArray<int>(subCells.Length, Allocator.Persistent);
        SubCells.CopyFrom(subCells);
        //SubCells = subCells;
    }
}
