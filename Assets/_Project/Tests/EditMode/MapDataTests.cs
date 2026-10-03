using NUnit.Framework;
using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class MapDataTests
    {
        private TerrainTypeData _plain;
        private TerrainTypeData _forest;
        private MapData _map;

        [SetUp]
        public void SetUp()
        {
            _plain = ScriptableObject.CreateInstance<TerrainTypeData>();
            _plain.SetValues("P", "Plain", 1, true, 0, 0, Color.green);
            _forest = ScriptableObject.CreateInstance<TerrainTypeData>();
            _forest.SetValues("F", "Forest", 2, true, 1, 20, Color.black);
            _map = ScriptableObject.CreateInstance<MapData>();
        }

        [TearDown]
        public void TearDown()
        {
            // CreateInstance objects aren't garbage collected; destroy them so they don't leak between tests.
            Object.DestroyImmediate(_plain);
            Object.DestroyImmediate(_forest);
            Object.DestroyImmediate(_map);
        }

        [Test]
        public void BuildGrid_ValidLayout_ProducesMatchingGrid()
        {
            _map.SetLayout(2, 1, new[] { "P", "F" }, new[] { 0, 3 }, new[] { _plain, _forest });

            GridBuildResult result = _map.BuildGrid();

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Tile forest = result.Map.GetTile(new Vector2Int(1, 0));
            Assert.AreEqual("F", forest.Terrain.Id);
            Assert.AreEqual(2, forest.Terrain.MoveCost);
            Assert.AreEqual(3, forest.Height);
        }

        [Test]
        public void BuildGrid_MissingTerrainReference_ReportsUnknownId()
        {
            _map.SetLayout(2, 1, new[] { "P", "F" }, new[] { 0, 0 }, new[] { _plain, null });

            GridBuildResult result = _map.BuildGrid();

            Assert.IsFalse(result.Success);
            StringAssert.Contains("'F'", result.Errors[0]);
        }

        [Test]
        public void TryGetTerrainType_FindsById()
        {
            _map.SetLayout(1, 1, new[] { "P" }, new[] { 0 }, new[] { _plain, _forest });

            Assert.IsTrue(_map.TryGetTerrainType("F", out TerrainTypeData found));
            Assert.AreSame(_forest, found);
            Assert.IsFalse(_map.TryGetTerrainType("X", out _));
        }
    }
}
