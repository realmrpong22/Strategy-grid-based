using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class GridMapTests
    {
        private GridMap _map;
        private List<Tile> _buffer;

        [SetUp]
        public void SetUp()
        {
            _map = TestGrids.Flat(4, 3);
            _buffer = new List<Tile>();
        }

        [TestCase(0, 0, true)]
        [TestCase(3, 2, true)]
        [TestCase(4, 0, false)]
        [TestCase(0, 3, false)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        public void InBounds_Edges(int x, int z, bool expected)
        {
            Assert.AreEqual(expected, _map.InBounds(new Vector2Int(x, z)));
        }

        [Test]
        public void TryGetTile_OutOfBounds_ReturnsFalseAndNull()
        {
            bool found = _map.TryGetTile(new Vector2Int(10, 10), out Tile tile);

            Assert.IsFalse(found);
            Assert.IsNull(tile);
        }

        [Test]
        public void GetTile_OutOfBounds_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _map.GetTile(new Vector2Int(-1, 0)));
        }

        [Test]
        public void GetTile_PositionMatchesLookup()
        {
            var position = new Vector2Int(2, 1);
            Assert.AreEqual(position, _map.GetTile(position).Position);
        }

        [Test]
        public void GetNeighbors_Interior_ReturnsFour()
        {
            _map.GetNeighbors(new Vector2Int(1, 1), _buffer);

            CollectionAssert.AreEquivalent(
                new[] { new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(1, 0), new Vector2Int(0, 1) },
                _buffer.Select(t => t.Position));
        }

        [Test]
        public void GetNeighbors_Corner_ReturnsTwo()
        {
            _map.GetNeighbors(new Vector2Int(0, 0), _buffer);
            Assert.AreEqual(2, _buffer.Count);
        }

        [Test]
        public void GetNeighbors_Edge_ReturnsThree()
        {
            _map.GetNeighbors(new Vector2Int(3, 1), _buffer);
            Assert.AreEqual(3, _buffer.Count);
        }

        [Test]
        public void GetNeighbors_ClearsBufferFirst()
        {
            _map.GetNeighbors(new Vector2Int(1, 1), _buffer);
            _map.GetNeighbors(new Vector2Int(0, 0), _buffer);
            Assert.AreEqual(2, _buffer.Count);
        }

        [Test]
        public void AllTiles_VisitsEveryTileOnce_ZMajorOrder()
        {
            List<Tile> tiles = _map.AllTiles().ToList();

            Assert.AreEqual(12, tiles.Count);
            Assert.AreEqual(12, tiles.Select(t => t.Position).Distinct().Count());
            Assert.AreEqual(new Vector2Int(0, 0), tiles[0].Position);
            Assert.AreEqual(new Vector2Int(3, 0), tiles[3].Position);
            Assert.AreEqual(new Vector2Int(0, 1), tiles[4].Position);
        }
    }
}
