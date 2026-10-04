using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Editor;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class UnitClassCsvParserTests
    {
        private const string Header = "id,name,maxHp,move,jump,flies,color\n";

        private static CsvParseResult<IReadOnlyList<UnitClassRow>> Parse(string text) =>
            UnitClassCsvParser.Parse(text, "unit_classes.csv");

        [Test]
        public void Parse_ValidRows()
        {
            var result = Parse(Header + "Soldier,Soldier,20,5,1,FALSE,#B8B8B8\nPegasusKnight,Pegasus Knight,18,7,0,TRUE,FF0000");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Value.Count);

            UnitClassRow soldier = result.Value[0];
            Assert.AreEqual("Soldier", soldier.Id);
            Assert.AreEqual(20, soldier.Definition.MaxHp);
            Assert.AreEqual(5, soldier.Definition.Move);
            Assert.AreEqual(1, soldier.Definition.Jump);
            Assert.IsFalse(soldier.Definition.Flies);

            UnitClassRow pegasus = result.Value[1];
            Assert.AreEqual("Pegasus Knight", pegasus.DisplayName);
            Assert.IsTrue(pegasus.Definition.Flies);
            Assert.AreEqual(Color.red, pegasus.Color);
        }

        [Test]
        public void Parse_ColumnOrderDoesNotMatter_ExtraColumnsIgnored()
        {
            var result = Parse("notes,FLIES,color,jump,move,maxHp,name,id\nfast,false,#FFFFFF,1,7,22,Cavalier,Cavalier");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(7, result.Value[0].Definition.Move);
        }

        [Test]
        public void Parse_MissingColumn_ReportsName()
        {
            var result = Parse("id,name,maxHp,move,flies,color\nSoldier,Soldier,20,5,FALSE,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("'jump'", result.Errors[0]);
        }

        [Test]
        public void Parse_BadNumber_CitesCell()
        {
            var result = Parse(Header + "Soldier,Soldier,20,five,1,FALSE,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2, move (D2)", result.Errors[0]);
        }

        [Test]
        public void Parse_InvalidStat_ReportsDefinitionError()
        {
            var result = Parse(Header + "Soldier,Soldier,0,5,1,FALSE,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2", result.Errors[0]);
            StringAssert.Contains("max HP", result.Errors[0]);
        }

        [Test]
        public void Parse_DuplicateIdDifferentCase_Fails()
        {
            var result = Parse(Header + "Soldier,A,20,5,1,FALSE,#FFFFFF\nSOLDIER,B,20,5,1,FALSE,#FFFFFF");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("duplicate", result.Errors[0]);
            StringAssert.Contains("row 3", result.Errors[0]);
        }

        [TestCase("Soldier")]
        [TestCase("Knight2")]
        [TestCase("Pegasus_Knight")]
        public void IsValidClassId_Accepts(string id)
        {
            Assert.IsTrue(UnitClassCsvParser.IsValidClassId(id));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("2Knight")]
        [TestCase("_Knight")]
        [TestCase("Pegasus Knight")]
        [TestCase("Knight-A")]
        public void IsValidClassId_Rejects(string id)
        {
            Assert.IsFalse(UnitClassCsvParser.IsValidClassId(id));
        }

        [Test]
        public void Parse_InvalidId_Fails()
        {
            Assert.IsFalse(Parse(Header + "Pegasus Knight,P,18,7,0,TRUE,#FFFFFF").Success);
        }

        [Test]
        public void Parse_MultipleBadFields_AllReported()
        {
            var result = Parse(Header + "Soldier,Soldier,x,y,z,maybe,nope");

            Assert.AreEqual(5, result.Errors.Count);
        }

        [Test]
        public void Parse_HeaderOnly_Fails()
        {
            Assert.IsFalse(Parse(Header).Success);
        }
    }
}
