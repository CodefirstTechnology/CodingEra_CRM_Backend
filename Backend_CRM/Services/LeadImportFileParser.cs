using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CRM.DTO;
using CsvHelper;
using CsvHelper.Configuration;

namespace CRM.Services
{
    public sealed class LeadImportFileParser : ILeadImportFileParser
    {
        public Task<IReadOnlyList<LeadImportRowDto>> ParseAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            var matrix = extension switch
            {
                ".xlsx" => ParseXlsxMatrix(stream),
                ".csv" => ParseCsvMatrix(stream),
                _ => throw new InvalidOperationException("Only .xlsx and .csv files are supported."),
            };

            return Task.FromResult(LeadImportMatrixParser.Parse(matrix));
        }

        private static List<string[]> ParseXlsxMatrix(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
            {
                return new List<string[]>();
            }

            var rowCount = usedRange.RowCount();
            var colCount = usedRange.ColumnCount();
            var firstRow = usedRange.FirstRow().RowNumber();
            var firstCol = usedRange.FirstColumn().ColumnNumber();
            var matrix = new List<string[]>();

            for (var r = 0; r < rowCount; r++)
            {
                var cells = new string[colCount];
                for (var c = 0; c < colCount; c++)
                {
                    cells[c] = NormalizeCell(worksheet.Cell(firstRow + r, firstCol + c).GetFormattedString());
                }

                if (RowHasContent(cells))
                {
                    matrix.Add(cells);
                }
            }

            return matrix;
        }

        private static List<string[]> ParseCsvMatrix(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                BadDataFound = null,
                MissingFieldFound = null,
            };

            using var csv = new CsvReader(reader, config);
            var matrix = new List<string[]>();

            while (csv.Read())
            {
                var record = csv.Parser.Record ?? Array.Empty<string>();
                var cells = record.Select(NormalizeCell).ToArray();
                if (RowHasContent(cells))
                {
                    matrix.Add(cells);
                }
            }

            return matrix;
        }

        private static string NormalizeCell(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static bool RowHasContent(IReadOnlyList<string> cells) =>
            cells.Any(c => c.Length > 0);
    }

    /// <summary>Shared matrix → DTO mapping for Excel and CSV uploads.</summary>
    internal static class LeadImportMatrixParser
    {
        public static IReadOnlyList<LeadImportRowDto> Parse(IReadOnlyList<string[]> matrix)
        {
            if (matrix.Count == 0)
            {
                return Array.Empty<LeadImportRowDto>();
            }

            var columns = ResolveColumns(matrix[0]);
            var rows = new List<LeadImportRowDto>();

            for (var i = 1; i < matrix.Count; i++)
            {
                var rawRow = matrix[i];
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasContent = false;

                for (var c = 0; c < columns.Count; c++)
                {
                    var text = c < rawRow.Length ? NormalizeCell(rawRow[c]) : string.Empty;
                    if (text.Length > 0)
                    {
                        hasContent = true;
                    }

                    values[columns[c]] = text;
                }

                if (!hasContent)
                {
                    continue;
                }

                rows.Add(MapRow(values, columns, i + 1));
            }

            return rows;
        }

        private static LeadImportRowDto MapRow(
            IReadOnlyDictionary<string, string> values,
            IReadOnlyList<string> columns,
            int rowNumber)
        {
            return new LeadImportRowDto
            {
                RowNumber = rowNumber,
                Salutation = Optional(Pick(values, columns, "salutation")),
                FirstName = Optional(Pick(values, columns, "first name", "firstname", "first_name")),
                LastName = Optional(Pick(values, columns, "last name", "lastname", "last_name")),
                Mobile = Optional(Pick(values, columns, "mobile", "phone", "mobile number")),
                Email = Optional(Pick(values, columns, "email", "e-mail")),
                Organization = Optional(Pick(values, columns, "organization", "organisation", "company")),
                Industry = Optional(Pick(values, columns, "industry")),
                NoOfEmployees = Optional(Pick(values, columns, "no of employees", "employees", "employee count", "no_of_employees")),
                AnnualRevenue = Optional(Pick(values, columns, "annual revenue", "revenue", "annual_revenue")),
                Website = Optional(Pick(values, columns, "website", "web site", "url")),
                Territory = Optional(Pick(values, columns, "territory")),
                Status = Optional(Pick(values, columns, "status", "lead status")),
                LeadOwner = Optional(Pick(values, columns, "lead owner", "owner", "assigned to")),
                RequestType = Optional(Pick(values, columns, "request type", "request_type")),
                Requirement = Optional(Pick(values, columns, "requirement", "requirements")),
                AdditionalDetails = Optional(Pick(values, columns, "additional details", "notes", "additional_details")),
            };
        }

        private static List<string> ResolveColumns(string[] headerRow)
        {
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var columns = new List<string>(headerRow.Length);

            for (var index = 0; index < headerRow.Length; index++)
            {
                var label = NormalizeCell(headerRow[index]);
                var baseName = label.Length > 0 ? label : $"Column {index + 1}";
                var count = used.GetValueOrDefault(baseName.ToLowerInvariant());
                used[baseName.ToLowerInvariant()] = count + 1;
                columns.Add(count == 0 ? baseName : $"{baseName} ({count + 1})");
            }

            return columns;
        }

        private static string Pick(
            IReadOnlyDictionary<string, string> values,
            IReadOnlyList<string> columns,
            params string[] aliases)
        {
            var aliasSet = new HashSet<string>(
                aliases.Select(NormalizeHeaderKey),
                StringComparer.OrdinalIgnoreCase);

            foreach (var col in columns)
            {
                if (aliasSet.Contains(NormalizeHeaderKey(col)))
                {
                    return values.TryGetValue(col, out var val) ? val : string.Empty;
                }
            }

            return string.Empty;
        }

        private static string NormalizeHeaderKey(string value) =>
            Regex.Replace(value.Trim(), @"\s+", " ").ToLowerInvariant();

        private static string? Optional(string value) =>
            value.Length > 0 ? value : null;

        private static string NormalizeCell(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
