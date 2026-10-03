using System.Collections.Generic;
using System.Text;

namespace Tactics.Editor
{
    public sealed class CsvParseResult<T> where T : class
    {
        /// <summary>Null when parsing failed.</summary>
        public T Value { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;

        public CsvParseResult(T value, IReadOnlyList<string> errors)
        {
            Value = value;
            Errors = errors;
        }
    }

    /// <summary>
    /// Minimal RFC 4180 reader: comma separated, double-quoted fields may contain commas, quotes ("")
    /// and newlines. Handles CRLF/LF and a UTF-8 BOM, which spreadsheet exports often include.
    /// </summary>
    public static class CsvReader
    {
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            int i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
            for (; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c != '"')
                        field.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                        inQuotes = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        fields.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                            i++;
                        EndRow();
                        break;
                    case '\n':
                        EndRow();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            // Last line without a trailing newline.
            if (field.Length > 0 || fields.Count > 0)
                EndRow();

            return rows;

            void EndRow()
            {
                fields.Add(field.ToString());
                field.Clear();
                rows.Add(fields.ToArray());
                fields.Clear();
            }
        }

        public static bool IsBlank(string[] row)
        {
            foreach (string field in row)
            {
                if (!string.IsNullOrWhiteSpace(field))
                    return false;
            }
            return true;
        }
    }
}
