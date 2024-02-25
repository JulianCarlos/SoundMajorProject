using UnityEngine;
using System;

public class Heap
{
    int[] items;
    int[] heapIndices; // Store heap indices for each item
    int currentItemCount;

    public Heap(int maxHeapSize)
    {
        items = new int[maxHeapSize];
        heapIndices = new int[maxHeapSize];
    }

    public void Add(int item)
    {
        heapIndices[item] = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public int RemoveFirst()
    {
        int firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        heapIndices[items[0]] = 0;
        SortDown(items[0]);
        return firstItem;
    }

    public void UpdateItem(int item)
    {
        SortUp(item);
    }

    public int Count
    {
        get
        {
            return currentItemCount;
        }
    }

    public bool Contains(int item)
    {
        return heapIndices[item] < currentItemCount;
    }

    void SortDown(int item)
    {
        while (true)
        {
            int childIndexLeft = item * 2 + 1;
            int childIndexRight = item * 2 + 2;
            int swapIndex = 0;

            if (childIndexLeft < currentItemCount)
            {
                swapIndex = childIndexLeft;

                if (childIndexRight < currentItemCount)
                {
                    if (items[childIndexLeft] < items[childIndexRight])
                    {
                        swapIndex = childIndexRight;
                    }
                }

                if (items[item] < items[swapIndex])
                {
                    Swap(item, swapIndex);
                }
                else
                {
                    return;
                }

            }
            else
            {
                return;
            }

        }
    }

    void SortUp(int item)
    {
        int parentIndex = (item - 1) / 2;

        while (true)
        {
            int parentItem = items[parentIndex];
            if (item > parentItem)
            {
                Swap(item, parentItem);
            }
            else
            {
                break;
            }

            parentIndex = (item - 1) / 2;
        }
    }

    void Swap(int itemA, int itemB)
    {
        int temp = items[itemA];
        items[itemA] = items[itemB];
        items[itemB] = temp;

        // Update heap indices
        heapIndices[items[itemA]] = itemA;
        heapIndices[items[itemB]] = itemB;
    }
}