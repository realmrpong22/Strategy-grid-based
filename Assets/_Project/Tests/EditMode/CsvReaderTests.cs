using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Editor;

namespace Tactics.Tests.EditMode
{
    public class CsvReaderTests
    {
        [Test]
        public void Parse_SimpleRows()
        {
            List<string[]> rows = CsvReader.Parse("a,b\nc,d");

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
        }

        [Test]
        public void Parse_QuotedFieldWithCommaAndEscapedQuote()
        {
            List<string[]> rows = CsvReader.Parse("\"Fort, ruined\",\"say \"\"hi\"\"\"");

            CollectionAssert.AreEqual(new[] { "Fort, ruined", "say \"hi\"" }, rows[0]);
        }

        [Test]
        public void Parse_CrLfAndTrailingNewline_NoExtraRow()
        {
            List<string[]> rows = CsvReader.Parse("a,b\r\nc,d\r\n");

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
        }

        [Test]
        public void Parse_StripsBom()
        {
            List<string[]> rows = CsvReader.Parse("﻿id,name");

            Assert.AreEqual("id", rows[0][0]);
        }

        [Test]
        public void Parse_KeepsEmptyFields()
        {
            List<string[]> rows = CsvReader.Parse("a,,c,");

            CollectionAssert.AreEqual(new[] { "a", "", "c", "" }, rows[0]);
        }

        [Test]
        public void Parse_EmptyText_NoRows()
        {
            Assert.AreEqual(0, CsvReader.Parse("").Count);
        }

        [Test]
        public void IsBlank_WhitespaceOnly()
        {
            Assert.IsTrue(CsvReader.IsBlank(new[] { "", " ", "\t" }));
            Assert.IsFalse(CsvReader.IsBlank(new[] { "", "x" }));
        }
    }
}
