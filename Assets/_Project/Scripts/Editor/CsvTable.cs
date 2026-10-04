using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Tactics.Editor
{
    /// <summary>
    /// A CSV whose first row is a header. Columns are found by name (case-insensitive), so the sheet's
    /// column order doesn't matter and extra columns (notes, etc.) are ignored. Field readers append
    /// errors that cite the sheet row, column name and cell, e.g. "unit_classes.csv row 3, move (D3)".
    /// </summary>
    public sealed class CsvTable
    {
        private readonly Dictionary<string, int> _columns;
        private readonly List<string[]> _rows;
        private readonly List<string> _errors;

        public string SourceName { get; }

        private CsvTable(string sourceName, Dictionary<string, int> columns, List<string[]> rows, List<string> errors)
        {
            SourceName = sourceName;
            _columns = columns;
            _rows = rows;
            _errors = errors;
        }

        /// <summary>
        /// Reads the header and checks required columns. Returns null (with errors added) if the file is
        /// empty or a required column is missing. Later field errors go to the same list.
        /// </summary>
        public static CsvTable Parse(string text, string sourceName, IEnumerable<string> requiredColumns, List<string> errors)
        {
            List<string[]> rows = CsvReader.Parse(text);
            if (rows.Count == 0)
            {
                errors.Add($"{sourceName}: file is empty.");
                return null;
            }

            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows[0].Length; i++)
            {
                string header = rows[0][i].Trim();
                if (header.Length > 0 && !columns.ContainsKey(header))
                    columns.Add(header, i);
            }

            int errorsBefore = errors.Count;
            foreach (string required in requiredColumns)
            {
                if (!columns.ContainsKey(required))
                    errors.Add($"{sourceName}: missing column '{required}'.");
            }

            return errors.Count > errorsBefore ? null : new CsvTable(sourceName, columns, rows, errors);
        }

        /// <summary>Data rows (after the header), skipping blank ones.</summary>
        public IEnumerable<CsvTableRow> Rows
        {
            get
            {
                for (int r = 1; r < _rows.Count; r++)
                {
                    if (!CsvReader.IsBlank(_rows[r]))
                        yield return new CsvTableRow(this, _rows[r], r);
                }
            }
        }

        internal int ColumnIndex(string column) => _columns[column];

        internal void AddError(string message) => _errors.Add(message);
    }

    /// <summary>One data row of a CsvTable. Readers return a fallback value and record an error on bad input.</summary>
    public sealed class CsvTableRow
    {
        private readonly CsvTable _table;
        private readonly string[] _fields;
        private readonly int _rowIndex;

        /// <summary>"file.csv row N" with N as the spreadsheet shows it (header is row 1).</summary>
        public string Where => $"{_table.SourceName} row {_rowIndex + 1}";

        internal CsvTableRow(CsvTable table, string[] fields, int rowIndex)
        {
            _table = table;
            _fields = fields;
            _rowIndex = rowIndex;
        }

        public string Text(string column)
        {
            int index = _table.ColumnIndex(column);
            return index < _fields.Length ? _fields[index].Trim() : string.Empty;
        }

        public int Int(string column)
        {
            string value = Text(column);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            Error(column, $"'{value}' is not a whole number.");
            return 0;
        }

        /// <summary>TRUE/FALSE (any case) or 1/0, matching what spreadsheets export.</summary>
        public bool Bool(string column)
        {
            string value = Text(column);
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0")
                return false;
            Error(column, $"'{value}' is not TRUE/FALSE.");
            return false;
        }

        /// <summary>Hex colour, '#' optional.</summary>
        public Color Color(string column)
        {
            string value = Text(column);
            string html = value.StartsWith("#", StringComparison.Ordinal) ? value : "#" + value;
            if (value.Length > 0 && ColorUtility.TryParseHtmlString(html, out Color result))
                return result;
            Error(column, $"'{value}' is not a hex colour like #8DB360.");
            return UnityEngine.Color.magenta;
        }

        /// <summary>Enum member by name, case-insensitive. Numbers are rejected so a typo like "1" can't pass.</summary>
        public T Enum<T>(string column) where T : struct, System.Enum
        {
            string value = Text(column);
            if (value.Length > 0 && !char.IsDigit(value[0]) && value[0] != '-'
                && System.Enum.TryParse(value, true, out T result) && System.Enum.IsDefined(typeof(T), result))
                return result;
            Error(column, $"'{value}' is not one of: {string.Join(", ", System.Enum.GetNames(typeof(T)))}.");
            return default;
        }

        /// <summary>Spreadsheet cell of this row's field, e.g. "C4".</summary>
        public string Cell(string column) => MapCsvParser.CellName(_table.ColumnIndex(column), _rowIndex);

        public void Error(string message) => _table.AddError($"{Where}: {message}");

        public void Error(string column, string message) => _table.AddError($"{Where}, {column} ({Cell(column)}): {message}");
    }
}
