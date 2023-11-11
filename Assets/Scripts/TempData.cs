
public unsafe struct TempData
{
    public readonly int ParentIndex;
    public readonly float FCost;

    public TempData(int index, float cost)
    {
        ParentIndex = index;
        FCost = cost;
    }
}
