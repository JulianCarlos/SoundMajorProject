using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct GridCore
{
    public float3 CorePos;
    public int[] SubCells;

    public GridCore(float3 corePos, int[] subCells)
    {
        CorePos = corePos;
        SubCells = subCells;
    }
}
