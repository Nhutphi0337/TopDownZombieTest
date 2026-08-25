using System.Collections.Generic;

public class FlowFieldPriorityQueue
{
    private readonly List<Entry> elements =
        new List<Entry>();

    public int Count => elements.Count;

    private struct Entry
    {
        public FlowFieldCell Cell;
        public int Priority;

        public Entry(
            FlowFieldCell cell,
            int priority)
        {
            Cell = cell;
            Priority = priority;
        }
    }

    public void Enqueue(
        FlowFieldCell cell,
        int priority)
    {
        Entry entry =
            new Entry(cell, priority);

        elements.Add(entry);

        int index =
            elements.Count - 1;

        while (index > 0)
        {
            int parent =
                (index - 1) / 2;

            if (elements[parent].Priority <=
                priority)
            {
                break;
            }

            elements[index] =
                elements[parent];

            index = parent;
        }

        elements[index] = entry;
    }

    public FlowFieldCell Dequeue(
        out int priority)
    {
        Entry root =
            elements[0];

        priority =
            root.Priority;

        int lastIndex =
            elements.Count - 1;

        Entry last =
            elements[lastIndex];

        elements.RemoveAt(lastIndex);

        if (elements.Count > 0)
        {
            int index = 0;

            while (true)
            {
                int left =
                    index * 2 + 1;

                if (left >= elements.Count)
                    break;

                int right =
                    left + 1;

                int smallest =
                    left;

                if (right < elements.Count &&
                    elements[right].Priority <
                    elements[left].Priority)
                {
                    smallest = right;
                }

                if (elements[smallest].Priority >=
                    last.Priority)
                {
                    break;
                }

                elements[index] =
                    elements[smallest];

                index = smallest;
            }

            elements[index] =
                last;
        }

        return root.Cell;
    }
}