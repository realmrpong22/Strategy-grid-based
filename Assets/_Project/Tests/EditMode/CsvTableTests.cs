using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tactics.Core;
using Tactics.Editor;

namespace Tactics.Tests.EditMode
{
    public class CsvTableTests
    {
        private List<string> _errors;

        [SetUp]
        public void SetUp() => _errors = new List<string>();

        private CsvTable Parse(string text, params string[] required) => CsvTable.Parse(text, "t.csv", required, _errors);

        [Test]
        public void Parse_Empty_ReturnsNullWithError()
        {
            Assert.IsNull(Parse(""));
            StringAssert.Contains("empty", _errors[0]);
        }

        [Test]
        public void Parse_MissingColumns_ReportsEach()
        {
            Assert.IsNull(Parse("a\n1", "a", "b", "c"));
            Assert.AreEqual(2, _errors.Count);
            StringAssert.Contains("'b'", _errors[0]);
            StringAssert.Contains("'c'", _errors[1]);
        }

        [Test]
        public void Rows_SkipsBlankRows_ReportsSheetRowNumbers()
        {
            CsvTable table = Parse("a\n1\n,\n\n2", "a");

            List<CsvTableRow> rows = table.Rows.ToList();
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("t.csv row 2", rows[0].Where);
            Assert.AreEqual("t.csv row 5", rows[1].Where);
        }

        [Test]
        public void FieldError_CitesRowColumnAndCell()
        {
            CsvTableRow row = Parse("name,count\nx,lots", "name", "count").Rows.Single();

            Assert.AreEqual(0, row.Int("count"));
            Assert.AreEqual(1, _errors.Count);
            StringAssert.StartsWith("t.csv row 2, count (B2):", _errors[0]);
        }

        [Test]
        public void Text_MissingTrailingField_IsEmpty()
        {
            CsvTableRow row = Parse("a,b\n1", "a", "b").Rows.Single();

            Assert.AreEqual(string.Empty, row.Text("b"));
        }

        [TestCase("Enemy", Faction.Enemy)]
        [TestCase("player", Faction.Player)]
        [TestCase(" PLAYER ", Faction.Player)]
        public void Enum_ByNameCaseInsensitive(string value, Faction expected)
        {
            CsvTableRow row = Parse($"faction\n{value}", "faction").Rows.Single();

            Assert.AreEqual(expected, row.Enum<Faction>("faction"));
            CollectionAssert.IsEmpty(_errors);
        }

        [TestCase("1")]
        [TestCase("-1")]
        [TestCase("Neutral")]
        [TestCase("")]
        public void Enum_NumbersAndUnknownNames_Rejected(string value)
        {
            // Second column keeps the row non-blank when the value is empty (blank rows are skipped).
            CsvTableRow row = Parse($"id,faction\nx,{value}", "id", "faction").Rows.Single();

            row.Enum<Faction>("faction");
            Assert.AreEqual(1, _errors.Count);
            StringAssert.Contains("Player, Enemy", _errors[0]);
        }
    }
}
