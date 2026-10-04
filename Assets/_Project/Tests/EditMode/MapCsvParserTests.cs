using NUnit.Framework;
using Tactics.Editor;

namespace Tactics.Tests.EditMode
{
    public class MapCsvParserTests
    {
        private static readonly string[] KnownIds = { "P", "F", "W", "Wa" };

        [Test]
        public void Parse_FlipsRows_FirstSheetRowIsFarEdge()
        {
            // Sheet, top to bottom:   P0,F1   <- far edge, z = 1
            //                         W0,P2   <- near edge, z = 0
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("P0,F1\nW0,P2", "map.csv");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            MapLayout layout = result.Value;
            Assert.AreEqual(2, layout.Width);
            Assert.AreEqual(2, layout.Depth);

            // index = z * width + x
            CollectionAssert.AreEqual(new[] { "W", "P", "P", "F" }, layout.TerrainIds);
            CollectionAssert.AreEqual(new[] { 0, 2, 0, 1 }, layout.Heights);
        }

        [Test]
        public void Parse_TrimsTrailingBlankRowsAndColumns()
        {
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("P0,P0,,\nP0, P0 ,,\n,,,\n\n", "map.csv");

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Value.Width);
            Assert.AreEqual(2, result.Value.Depth);
        }

        [Test]
        public void Parse_EmptyCell_ReportsSheetCellAndGridCoordinate()
        {
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("P0,\nP0,P0", "map.csv");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("B1", result.Errors[0]);
            StringAssert.Contains("x=1, z=1", result.Errors[0]);
        }

        [Test]
        public void Parse_BlankRowInside_Fails()
        {
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("P0,P0\n,\nP0,P0", "map.csv");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("row 2", result.Errors[0]);
        }

        [Test]
        public void Parse_UnknownTerrain_ReportsCell()
        {
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("P0,X1", "map.csv", KnownIds);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("B1", result.Errors[0]);
            StringAssert.Contains("'X'", result.Errors[0]);
        }

        [Test]
        public void Parse_TerrainIdsAreCaseSensitive()
        {
            Assert.IsFalse(MapCsvParser.Parse("p0", "map.csv", KnownIds).Success);
        }

        [Test]
        public void Parse_ReportsEveryBadCell()
        {
            CsvParseResult<MapLayout> result = MapCsvParser.Parse("X0,P0\nP,1P", "map.csv", KnownIds);

            Assert.AreEqual(3, result.Errors.Count);
        }

        [Test]
        public void Parse_Empty_Fails()
        {
            Assert.IsFalse(MapCsvParser.Parse("\n,,\n", "map.csv").Success);
        }

        [TestCase("P0", "P", 0)]
        [TestCase("F12", "F", 12)]
        [TestCase("Wa3", "Wa", 3)]
        public void TryParseCell_Valid(string cell, string expectedId, int expectedHeight)
        {
            Assert.IsTrue(MapCsvParser.TryParseCell(cell, out string id, out int height));
            Assert.AreEqual(expectedId, id);
            Assert.AreEqual(expectedHeight, height);
        }

        [TestCase("P")]
        [TestCase("0")]
        [TestCase("1P")]
        [TestCase("P-1")]
        [TestCase("P1a")]
        [TestCase("P 1")]
        [TestCase("P99999999999")]
        public void TryParseCell_Invalid(string cell)
        {
            Assert.IsFalse(MapCsvParser.TryParseCell(cell, out _, out _));
        }

        [TestCase(0, 0, "A1")]
        [TestCase(25, 0, "Z1")]
        [TestCase(26, 4, "AA5")]
        [TestCase(27, 9, "AB10")]
        public void CellName_SpreadsheetStyle(int column, int row, string expected)
        {
            Assert.AreEqual(expected, MapCsvParser.CellName(column, row));
        }

        [TestCase("A1", 0, 0)]
        [TestCase("z1", 25, 0)]
        [TestCase("AA5", 26, 4)]
        [TestCase("ab10", 27, 9)]
        public void TryParseCellName_RoundTripsCellName(string cell, int column, int row)
        {
            Assert.IsTrue(MapCsvParser.TryParseCellName(cell, out int parsedColumn, out int parsedRow));
            Assert.AreEqual(column, parsedColumn);
            Assert.AreEqual(row, parsedRow);
            Assert.AreEqual(cell.ToUpperInvariant(), MapCsvParser.CellName(parsedColumn, parsedRow));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("A")]
        [TestCase("1")]
        [TestCase("A0")]
        [TestCase("1A")]
        [TestCase("A-1")]
        [TestCase("A 1")]
        [TestCase("É1")]
        [TestCase("ZZZZ1")]
        [TestCase("A99999999999")]
        public void TryParseCellName_Invalid(string cell)
        {
            Assert.IsFalse(MapCsvParser.TryParseCellName(cell, out _, out _));
        }

        [Test]
        public void SheetRowToZ_FirstRowIsFarEdge()
        {
            Assert.AreEqual(9, MapCsvParser.SheetRowToZ(0, 10));
            Assert.AreEqual(0, MapCsvParser.SheetRowToZ(9, 10));
        }
    }
}
