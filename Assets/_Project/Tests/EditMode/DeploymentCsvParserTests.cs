using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Core;
using Tactics.Editor;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class DeploymentCsvParserTests
    {
        private const string Header = "faction,class,cell\n";

        private static readonly Dictionary<string, UnitClassDefinition> Classes = new Dictionary<string, UnitClassDefinition>
        {
            [TestGrids.Soldier.Id] = TestGrids.Soldier,
            [TestGrids.Flier.Id] = TestGrids.Flier,
        };

        // Map sheet cells: A1 P, B1 P, C1 W (wall), D1 L (lake, fliers only). One row, so z = 0.
        private static readonly GridMap Map = TestGrids.Row("P", "P", "W", "L");

        private static CsvParseResult<IReadOnlyList<DeploymentRow>> Parse(string text, GridMap map = null) =>
            DeploymentCsvParser.Parse(text, "map_01.csv", Classes, map ?? Map);

        [Test]
        public void Parse_ValidRows()
        {
            var result = Parse(Header + "Player,Soldier,A1\nEnemy,Flier,D1");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Value.Count);
            Assert.AreEqual(Faction.Player, result.Value[0].Faction);
            Assert.AreEqual("Soldier", result.Value[0].ClassId);
            Assert.AreEqual(new Vector2Int(0, 0), result.Value[0].Position);
            Assert.AreEqual(Faction.Enemy, result.Value[1].Faction);
            Assert.AreEqual(new Vector2Int(3, 0), result.Value[1].Position);
        }

        [Test]
        public void Parse_Cell_UsesMapSheetOrientation()
        {
            // Map sheet row 1 is the far edge: on a 3-row map, A1 is z = 2 and B3 is (1, 0).
            GridMap map = TestGrids.Flat(2, 3);

            var result = Parse(Header + "Player,Soldier,A1\nPlayer,Soldier,b3", map);

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(new Vector2Int(0, 2), result.Value[0].Position);
            Assert.AreEqual(new Vector2Int(1, 0), result.Value[1].Position);
        }

        [Test]
        public void Parse_UnknownFaction_CitesCell()
        {
            var result = Parse(Header + "Neutral,Soldier,A1");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2, faction (A2)", result.Errors[0]);
        }

        [Test]
        public void Parse_UnknownClass_CitesCell()
        {
            var result = Parse(Header + "Player,Wizard,A1");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2, class (B2)", result.Errors[0]);
            StringAssert.Contains("'Wizard'", result.Errors[0]);
        }

        [Test]
        public void Parse_ClassWrongCase_SuggestsCorrectId()
        {
            var result = Parse(Header + "Player,soldier,A1");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("did you mean 'Soldier'", result.Errors[0]);
        }

        [TestCase("")]
        [TestCase("1A")]
        [TestCase("A0")]
        [TestCase("A")]
        [TestCase("4,0")]
        public void Parse_BadCell_CitesCell(string cell)
        {
            var result = Parse(Header + $"Player,Soldier,\"{cell}\"");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2, cell (C2)", result.Errors[0]);
        }

        [TestCase("E1")]
        [TestCase("A2")]
        public void Parse_CellOutsideMap_ReportsMapExtent(string cell)
        {
            var result = Parse(Header + $"Player,Soldier,{cell}");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("outside", result.Errors[0]);
            StringAssert.Contains("A1 to D1", result.Errors[0]);
        }

        [Test]
        public void Parse_SameTile_ReportsSecondRowAndMapCell()
        {
            var result = Parse(Header + "Player,Soldier,B1\nEnemy,Soldier,b1");

            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("row 3", result.Errors[0]);
            StringAssert.Contains("map cell B1", result.Errors[0]);
            StringAssert.Contains("occupied", result.Errors[0]);
        }

        [Test]
        public void Parse_TerrainClassCannotEnter_Fails()
        {
            var result = Parse(Header + "Player,Soldier,D1\nEnemy,Flier,C1");

            Assert.AreEqual(2, result.Errors.Count);
            StringAssert.Contains("can't stand", result.Errors[0]);
            StringAssert.Contains("row 3", result.Errors[1]);
        }

        [Test]
        public void Parse_FlierOnFlyableOnlyTerrain_Succeeds()
        {
            Assert.IsTrue(Parse(Header + "Enemy,Flier,D1").Success);
        }

        [Test]
        public void Parse_ColumnOrderDoesNotMatter()
        {
            var result = Parse("cell,class,faction,notes\nB1,Soldier,Player,leader");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(new Vector2Int(1, 0), result.Value[0].Position);
        }

        [Test]
        public void Parse_NoUnits_Fails()
        {
            Assert.IsFalse(Parse(Header).Success);
        }
    }
}
