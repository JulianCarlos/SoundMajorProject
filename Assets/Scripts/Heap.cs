public class Heap
{
    public int[] Elements;
    public int Size;

    public Heap(int size)
    {
        Elements = new int[size];
        Size = 0;
    }

    public int Peek()
    {
        return Elements[0];
    }

    public void Add(int element)
    {
        Elements[Size] = element;
        ReCalculateUp(Size);
        Size++;
    }

    public int Pop()
    {
        int result = Elements[0];
        Elements[0] = Elements[Size - 1];
        Size--;
        ReCalculateDown(0);

        return result;
    }

    private void ReCalculateDown(int index)
    {
        int leftChildIndex, rightChildIndex, smallestIndex;
        int element = Elements[index];

        while (true)
        {
            leftChildIndex = (index << 1) + 1;
            rightChildIndex = leftChildIndex + 1;
            smallestIndex = index;

            if (leftChildIndex < Size && Elements[leftChildIndex] < Elements[smallestIndex])
                smallestIndex = leftChildIndex;
            if (rightChildIndex < Size && Elements[rightChildIndex] < Elements[smallestIndex])
                smallestIndex = rightChildIndex;

            if (smallestIndex == index)
                break;

            Elements[index] = Elements[smallestIndex];
            index = smallestIndex;
        }

        Elements[index] = element;
    }

    private void ReCalculateUp(int index)
    {
        int element = Elements[index];

        while (index > 0)
        {
            int parentIndex = (index - 1) >> 1;
            if (element >= Elements[parentIndex])
                break;

            Elements[index] = Elements[parentIndex];
            index = parentIndex;
        }

        Elements[index] = element;
    }
}