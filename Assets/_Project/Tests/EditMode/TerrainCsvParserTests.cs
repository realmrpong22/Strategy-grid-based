using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Editor;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class TerrainCsvParserTests
    {
        private const string Header = "id,name,moveCost,passable,flyable,defense,avoid,color\n";

        private static CsvParseResult<IReadOnlyList<TerrainRow>> Parse(string text) =>
            TerrainCsvParser.Parse(text, "terrain.csv");

        [Test]
        public void Parse_ValidRows()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,TRUE,0,0,#8DB360\nW,Water,1,FALSE,TRUE,0,0,#3D7CC9");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Value.Count);

            TerrainRow plain = result.Value[0];
            Assert.AreEqual("P", plain.Id);
            Assert.AreEqual("Plain", plain.DisplayName);
            Assert.AreEqual(1, plain.Definition.MoveCost);
            Assert.IsTrue(plain.Definition.IsPassable);
            Assert.IsTrue(plain.Definition.IsFlyable);

            TerrainRow water = result.Value[1];
            Assert.IsFalse(water.Definition.IsPassable);
            Assert.IsTrue(water.Definition.IsFlyable);
        }

        [Test]
        public void Parse_ColumnOrderAndCaseDoNotMatter_ExtraColumnsIgnored()
        {
            var result = Parse("COLOR,notes,avoid,defense,Flyable,passable,moveCost,name,ID\n#FFFFFF,whatever,20,1,false,true,2,Forest,F");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual("F", result.Value[0].Id);
            Assert.AreEqual(20, result.Value[0].Definition.AvoidBonus);
            Assert.IsFalse(result.Value[0].Definition.IsFlyable);
        }

        [Test]
        public void Parse_MissingColumn_ReportsName()
        {
            var result = Parse("id,name,moveCost,passable,flyable,defense,color\nP,Plain,1,TRUE,TRUE,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("'avoid'", result.Errors[0]);
        }

        [Test]
        public void Parse_MissingFlyableColumn_ReportsName()
        {
            var result = Parse("id,name,moveCost,passable,defense,avoid,color\nP,Plain,1,TRUE,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("'flyable'", result.Errors[0]);
        }

        [Test]
        public void Parse_BadNumber_ReportsRowAndColumn()
        {
            var result = Parse(Header + "P,Plain,one,TRUE,TRUE,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2", result.Errors[0]);
            StringAssert.Contains("moveCost", result.Errors[0]);
        }

        [TestCase("TRUE", true)]
        [TestCase("false", false)]
        [TestCase("1", true)]
        [TestCase("0", false)]
        public void Parse_BoolVariants(string value, bool expected)
        {
            var result = Parse(Header + $"P,Plain,1,{value},{value},0,0,#FFFFFF");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(expected, result.Value[0].Definition.IsPassable);
            Assert.AreEqual(expected, result.Value[0].Definition.IsFlyable);
        }

        [Test]
        public void Parse_BadBool_Fails()
        {
            Assert.IsFalse(Parse(Header + "P,Plain,1,maybe,TRUE,0,0,#FFFFFF").Success);
        }

        [Test]
        public void Parse_BadFlyable_ReportsColumn()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,sometimes,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("flyable", result.Errors[0]);
        }

        [Test]
        public void Parse_ColorWithoutHash_Accepted()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,TRUE,0,0,FF0000");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(Color.red, result.Value[0].Color);
        }

        [Test]
        public void Parse_BadColor_Fails()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,TRUE,0,0,#GG0000");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("color", result.Errors[0]);
        }

        [Test]
        public void Parse_DuplicateIdDifferentCase_Fails()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,TRUE,0,0,#FFFFFF\np,Plain2,1,TRUE,TRUE,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("duplicate", result.Errors[0]);
        }

        [TestCase("F2")]
        [TestCase("")]
        [TestCase("Hi ll")]
        public void Parse_InvalidId_Fails(string id)
        {
            Assert.IsFalse(Parse(Header + $"{id},Name,1,TRUE,TRUE,0,0,#FFFFFF").Success);
        }

        [Test]
        public void Parse_PassableWithZeroCost_ReportsDefinitionError()
        {
            var result = Parse(Header + "P,Plain,0,TRUE,FALSE,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2", result.Errors[0]);
        }

        [Test]
        public void Parse_FlyableWithZeroCost_ReportsDefinitionError()
        {
            var result = Parse(Header + "W,Water,0,FALSE,TRUE,0,0,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2", result.Errors[0]);
        }

        [Test]
        public void Parse_BlankRowsSkipped()
        {
            var result = Parse(Header + "P,Plain,1,TRUE,TRUE,0,0,#FFFFFF\n,,,,,,,\n\nF,Forest,2,TRUE,TRUE,1,20,#00FF00\n");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Value.Count);
        }

        [Test]
        public void Parse_HeaderOnly_Fails()
        {
            Assert.IsFalse(Parse(Header).Success);
        }
    }
}
