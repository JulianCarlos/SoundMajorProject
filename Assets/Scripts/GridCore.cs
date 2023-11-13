using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct GridCore
{
    public float3 CorePos;
    public Cell[] SubCells;

    public GridCore(float3 corePos, Cell[] subCells)
    {
        CorePos = corePos;
        SubCells = subCells;
    }
}
