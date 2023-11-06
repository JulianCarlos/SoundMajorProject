using System;

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
        int leftChildIndex = (index << 1) + 1;
        int rightChildIndex = (index << 1) + 2;
        int smallestIndex = index;

        if (leftChildIndex < Size && Elements[leftChildIndex] < Elements[smallestIndex])
        {
            smallestIndex = leftChildIndex;
        }

        if (rightChildIndex < Size && Elements[rightChildIndex] < Elements[smallestIndex])
        {
            smallestIndex = rightChildIndex;
        }

        if (smallestIndex != index)
        {
            Swap(index, smallestIndex);
            ReCalculateDown(smallestIndex);
        }
    }

    private void ReCalculateUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) >> 1;
            if (Elements[index] >= Elements[parentIndex])
            {
                break;
            }
            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void Swap(int firstIndex, int secondIndex)
    {
        int temp = Elements[firstIndex];
        Elements[firstIndex] = Elements[secondIndex];
        Elements[secondIndex] = temp;
    }
}