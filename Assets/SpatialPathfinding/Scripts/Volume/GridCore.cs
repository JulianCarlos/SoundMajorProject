using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

public struct GridCore
{
    public float3 CorePos;
    public UnsafeList<int> SubCells;
    
    public GridCore(float3 corePos, int[] subCells)
    {
        CorePos = corePos;
        SubCells = new UnsafeList<int>(subCells.Length, Allocator.Persistent);

        for (int i = 0; i < subCells.Length; i++)
        {
            SubCells.Add(subCells[i]);
        }
        //SubCells = subCells;
    }
}
