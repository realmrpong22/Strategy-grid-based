using System;
using System.Collections.Generic;

namespace Tactics.Core
{
    /// <summary>
    /// Binary min-heap keyed by an int priority. Exists because Unity's .NET profile has no
    /// System.Collections.Generic.PriorityQueue (that arrived in .NET 6); named MinHeap so it won't
    /// clash if Unity's runtime gains it. No decrease-key: Dijkstra pushes duplicates and skips stale ones.
    /// </summary>
    internal sealed class MinHeap<T>
    {
        private readonly List<(T Item, int Priority)> _nodes = new List<(T, int)>();

        public int Count => _nodes.Count;

        public void Push(T item, int priority)
        {
            _nodes.Add((item, priority));
            SiftUp(_nodes.Count - 1);
        }

        public T Pop(out int priority)
        {
            if (_nodes.Count == 0)
                throw new InvalidOperationException("Heap is empty.");

            (T item, int itemPriority) = _nodes[0];
            int last = _nodes.Count - 1;
            _nodes[0] = _nodes[last];
            _nodes.RemoveAt(last);
            if (_nodes.Count > 0)
                SiftDown(0);

            priority = itemPriority;
            return item;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_nodes[parent].Priority <= _nodes[index].Priority)
                    return;
                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                int smallest = index;

                if (left < _nodes.Count && _nodes[left].Priority < _nodes[smallest].Priority)
                    smallest = left;
                if (right < _nodes.Count && _nodes[right].Priority < _nodes[smallest].Priority)
                    smallest = right;
                if (smallest == index)
                    return;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b) => (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
    }
}
