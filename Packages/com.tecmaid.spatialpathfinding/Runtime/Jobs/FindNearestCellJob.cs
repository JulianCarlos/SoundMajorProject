using Pathfinding.Helpers;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct FindNearestCellJob : IJob
{
    public int TotalCells;
    public int TotalCores;
    public int TotalCellsPerCore;

    public float3 TargetPosition;
    public NativeArray<int> TargetIndex;

    public NativeArray<Cell> Cells;
    public NativeArray<GridCore> Cores;

    public void Execute()
    {
        TargetIndex[0] = FindNearestCell(TargetPosition);
    }

    private int FindNearestCell(float3 position)
    {
        int closestCore = 0;
        float tempDistance;
        float distance = float.MaxValue;

        for (int i = 0; i < TotalCores; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cores[i].CorePos, position);

            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCore = i;
            }
        }

        distance = float.MaxValue;
        int closestCell = 0;

        NativeArray<int> subCells = new NativeArray<int>(Cores[closestCore].SubCells.Length, Allocator.Temp);

        for (int i = 0; i < Cores[closestCore].SubCells.Length; i++)
        {
            subCells[i] = Cores[closestCore].SubCells[i];
        }

        for (int i = 0; i < TotalCellsPerCore; i++)
        {
            tempDistance = CalculationHelper.CalculateSquaredDistance(Cells[subCells[i]].CellPos, position);

            if (tempDistance < distance)
            {
                distance = tempDistance;
                closestCell = subCells[i];
            }
        }

        subCells.Dispose();

        return closestCell;
    }
}
