using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[BurstCompile]
public struct FindNearestCellJob : IJob
{
    public float3 PlayerPos;

    public NativeArray<Cell> Cells;
    public NativeArray<int> ClosestCell;

    public void Execute()
    {
        int closestCell = -1;
        float distance = float.MaxValue;

        for (int i = 0; i < Cells.Length; i++)
        {
            float tempDistance = math.distance(Cells[i].CellPos, PlayerPos);

            if (tempDistance < distance)
            {
                closestCell = i;
                distance = tempDistance;
            }
        }
        ClosestCell[0] = closestCell;
    }
}
