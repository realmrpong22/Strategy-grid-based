using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    public class MinHeapTests
    {
        [Test]
        public void Pop_ReturnsItemsInPriorityOrder()
        {
            var heap = new MinHeap<string>();
            int[] priorities = { 5, 1, 8, 3, 9, 2, 7, 4, 6, 0 };
            foreach (int p in priorities)
                heap.Push($"item{p}", p);

            var popped = new List<int>();
            while (heap.Count > 0)
            {
                string item = heap.Pop(out int priority);
                Assert.AreEqual($"item{priority}", item);
                popped.Add(priority);
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, popped);
        }

        [Test]
        public void Pop_DuplicatePriorities_AllReturned()
        {
            var heap = new MinHeap<int>();
            heap.Push(1, 2);
            heap.Push(2, 2);
            heap.Push(3, 1);

            Assert.AreEqual(3, heap.Pop(out int first));
            Assert.AreEqual(1, first);
            heap.Pop(out int second);
            heap.Pop(out int third);
            Assert.AreEqual(2, second);
            Assert.AreEqual(2, third);
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Pop_InterleavedWithPush_StaysOrdered()
        {
            var heap = new MinHeap<int>();
            heap.Push(10, 10);
            heap.Push(5, 5);
            Assert.AreEqual(5, heap.Pop(out _));
            heap.Push(1, 1);
            heap.Push(7, 7);

            Assert.AreEqual(1, heap.Pop(out _));
            Assert.AreEqual(7, heap.Pop(out _));
            Assert.AreEqual(10, heap.Pop(out _));
        }

        [Test]
        public void Pop_Empty_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new MinHeap<int>().Pop(out _));
        }
    }
}
