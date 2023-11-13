using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct MoveToTargetJob : IJob
{
    public NativeArray<Cell> cells;
    public Heap openCells;
    public int endPoint;
    public float3 targetPos;

    public void Execute()
    {

    }
}
