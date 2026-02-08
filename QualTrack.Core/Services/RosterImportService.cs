using System;
using System.Collections.Generic;
using System.Linq;

namespace QualTrack.Core.Services
{
    public class RosterImportFieldMap
    {
        public string? DODId { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? Rate { get; set; }
        public string? Rank { get; set; }
        public string? DutySectionType { get; set; }
        public string? DutySectionNumber { get; set; }
        public string? DutySection { get; set; }
    }

    public class RosterImportRecord
    {
        public string DODId { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string DutySectionType { get; set; } = string.Empty;
        public string DutySectionNumber { get; set; } = string.Empty;
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public string DutySectionDisplay =>
            string.IsNullOrWhiteSpace(DutySectionNumber) || string.IsNullOrWhiteSpace(DutySectionType)
                ? string.Empty
                : $"{DutySectionNumber}/{DutySectionType}";
    }

    public class RosterImportService
    {
        public List<RosterImportRecord> NormalizeRows(
            List<Dictionary<string, string>> rows,
            RosterImportFieldMap map)
        {
            var results = new List<RosterImportRecord>();
            foreach (var row in rows)
            {
                var record = new RosterImportRecord
                {
                    DODId = GetValue(row, map.DODId),
                    LastName = GetValue(row, map.LastName),
                    FirstName = GetValue(row, map.FirstName),
                    Rate = GetValue(row, map.Rate),
                    Rank = GetValue(row, map.Rank)
                };

                (record.DutySectionNumber, record.DutySectionType) = ResolveDutySection(row, map);

                ValidateRecord(record);
                results.Add(record);
            }

            return results;
        }

        private static string GetValue(Dictionary<string, string> row, string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
        }

        private static (string sectionNumber, string sectionType) ResolveDutySection(
            Dictionary<string, string> row,
            RosterImportFieldMap map)
        {
            var type = GetValue(row, map.DutySectionType);
            var number = GetValue(row, map.DutySectionNumber);
            if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(number))
            {
                return (number, type);
            }

            var combined = GetValue(row, map.DutySection);
            if (!string.IsNullOrWhiteSpace(combined))
            {
                var tokens = combined.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 2)
                {
                    return (tokens[0], tokens[1]);
                }
            }

            return (string.Empty, string.Empty);
        }

        private static void ValidateRecord(RosterImportRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.LastName))
            {
                record.Errors.Add("Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(record.FirstName))
            {
                record.Errors.Add("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(record.Rate))
            {
                record.Errors.Add("Rate is required.");
            }

            if (string.IsNullOrWhiteSpace(record.Rank))
            {
                record.Errors.Add("Rank is required.");
            }
        }
    }
}
