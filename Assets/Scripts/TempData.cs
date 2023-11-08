public unsafe struct TempData
{
    public int ParentIndex;
    public float FCost;

    public TempData(int index, float cost)
    {
        ParentIndex = index;
        FCost = cost;
    }
}
