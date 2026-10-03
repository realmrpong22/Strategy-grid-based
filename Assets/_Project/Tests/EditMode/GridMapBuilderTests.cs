using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class GridMapBuilderTests
    {
        [Test]
        public void Build_ValidLayout_Succeeds()
        {
            GridBuildResult result = GridMapBuilder.Build(
                3, 2,
                new[] { "P", "P", "F", "W", "P", "P" },
                new[] { 0, 0, 1, 2, 0, 0 },
                TestGrids.Terrains);

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(3, result.Map.Width);
            Assert.AreEqual(2, result.Map.Depth);
            CollectionAssert.IsEmpty(result.Errors);
        }

        [Test]
        public void Build_UsesRowMajorIndex_ZeroZFirst()
        {
            // index = z * width + x
            GridBuildResult result = GridMapBuilder.Build(
                3, 2,
                new[] { "P", "P", "F", "W", "P", "P" },
                new[] { 0, 0, 1, 2, 0, 0 },
                TestGrids.Terrains);

            Tile forest = result.Map.GetTile(new Vector2Int(2, 0));
            Tile wall = result.Map.GetTile(new Vector2Int(0, 1));

            Assert.AreSame(TestGrids.Forest, forest.Terrain);
            Assert.AreEqual(1, forest.Height);
            Assert.AreSame(TestGrids.Wall, wall.Terrain);
            Assert.AreEqual(2, wall.Height);
            Assert.AreEqual(new Vector2Int(0, 1), wall.Position);
        }

        [TestCase(0, 3)]
        [TestCase(3, 0)]
        [TestCase(-1, 2)]
        public void Build_NonPositiveSize_Fails(int width, int depth)
        {
            GridBuildResult result = GridMapBuilder.Build(
                width, depth, new string[0], new int[0], TestGrids.Terrains);

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Map);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [Test]
        public void Build_WrongArrayLengths_ReportsBoth()
        {
            GridBuildResult result = GridMapBuilder.Build(
                2, 2, new[] { "P", "P", "P" }, new[] { 0 }, TestGrids.Terrains);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(2, result.Errors.Count);
        }

        [Test]
        public void Build_UnknownTerrain_ReportsCoordinate()
        {
            GridBuildResult result = GridMapBuilder.Build(
                2, 1, new[] { "P", "X" }, new[] { 0, 0 }, TestGrids.Terrains);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("(1, 0)", result.Errors[0]);
            StringAssert.Contains("'X'", result.Errors[0]);
        }

        [Test]
        public void Build_NegativeHeight_Fails()
        {
            GridBuildResult result = GridMapBuilder.Build(
                1, 1, new[] { "P" }, new[] { -1 }, TestGrids.Terrains);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("negative", result.Errors[0]);
        }

        [Test]
        public void Build_MultipleBadCells_CollectsAllErrors()
        {
            GridBuildResult result = GridMapBuilder.Build(
                2, 2,
                new[] { "X", "P", "P", "Y" },
                new[] { 0, -1, 0, 0 },
                TestGrids.Terrains);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, result.Errors.Count);
        }

        [Test]
        public void Build_DuplicateTerrainIds_Fails()
        {
            var plainCopy = new TerrainDefinition("P", 3, true, 0, 0);

            GridBuildResult result = GridMapBuilder.Build(
                1, 1, new[] { "P" }, new[] { 0 }, new[] { TestGrids.Plain, plainCopy });

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Duplicate terrain id 'P'", result.Errors[0]);
        }

        [Test]
        public void Build_NullTerrainEntry_Fails()
        {
            GridBuildResult result = GridMapBuilder.Build(
                1, 1, new[] { "P" }, new[] { 0 }, new[] { TestGrids.Plain, null });

            Assert.IsFalse(result.Success);
            StringAssert.Contains("null entry", result.Errors[0]);
        }

        [Test]
        public void Build_NullInputs_FailsWithoutThrowing()
        {
            GridBuildResult result = GridMapBuilder.Build(1, 1, null, null, null);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, result.Errors.Count);
        }
    }
}
