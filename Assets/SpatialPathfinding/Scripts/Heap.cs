using System;

public class Heap
{
    private int[] elements;
    private int count;

    public int Count => count;

    public Heap(int capacity)
    {
        elements = new int[capacity];
        count = 0;
    }

    public int Peek()
    {
        if (count > 0) return elements[0];
        throw new InvalidOperationException("Heap is empty");
    }

    public void Add(int element)
    {
        if (count == elements.Length) Resize();
        elements[count] = element;
        HeapifyUp(count++);
    }

    public int Pop()
    {
        if (count > 0)
        {
            int result = elements[0];
            elements[0] = elements[--count];
            HeapifyDown(0);
            return result;
        }
        throw new InvalidOperationException("Heap is empty");
    }

    private void Resize()
    {
        System.Array.Resize(ref elements, elements.Length << 1);
    }

    private void HeapifyUp(int index)
    {
        int element = elements[index];
        while (index > 0)
        {
            int parentIndex = (index - 1) >> 1;
            int parentElement = elements[parentIndex];
            if (element >= parentElement) break;
            elements[index] = parentElement;
            index = parentIndex;
        }
        elements[index] = element;
    }

    private void HeapifyDown(int index)
    {
        int element = elements[index];
        while (true)
        {
            int leftChildIndex = (index << 1) + 1;
            if (leftChildIndex >= count) break;
            int rightChildIndex = leftChildIndex + 1;
            int smallestIndex = (rightChildIndex < count && elements[rightChildIndex] < elements[leftChildIndex]) ? rightChildIndex : leftChildIndex;
            if (elements[smallestIndex] >= element) break;
            elements[index] = elements[smallestIndex];
            index = smallestIndex;
        }
        elements[index] = element;
    }
}