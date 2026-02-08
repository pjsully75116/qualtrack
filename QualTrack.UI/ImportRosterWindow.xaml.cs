using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExcelDataReader;
using Microsoft.VisualBasic.FileIO;
using QualTrack.Core.Models;
using QualTrack.Core.Services;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;

namespace QualTrack.UI
{
    public partial class ImportRosterWindow : Window
    {
        private const string NoneOption = "(None)";
        private readonly RosterImportService _importService = new RosterImportService();
        private List<Dictionary<string, string>> _rawRows = new List<Dictionary<string, string>>();
        private List<RosterPreviewRow> _previewRows = new List<RosterPreviewRow>();
        private List<string> _headers = new List<string>();

        private sealed class RosterPreviewRow
        {
            public bool Include { get; set; }
            public string DODId { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string Rate { get; set; } = string.Empty;
            public string Rank { get; set; } = string.Empty;
            public string DutySection { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Errors { get; set; } = string.Empty;
            public bool IsDuplicate { get; set; }
            public RosterImportRecord Record { get; set; } = new RosterImportRecord();
        }

        public ImportRosterWindow()
        {
            InitializeComponent();
            DuplicateHandlingComboBox.SelectedIndex = 0;
        }

        private void BrowseRoster_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Roster Files (*.csv;*.xlsx)|*.csv;*.xlsx|CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Select Roster File"
            };

            if (dialog.ShowDialog() == true)
            {
                RosterFilePathTextBox.Text = dialog.FileName;
                LoadRosterFile(dialog.FileName);
            }
        }

        private void LoadRosterFile(string filePath)
        {
            try
            {
                _rawRows.Clear();
                _headers.Clear();
                StatusTextBlock.Text = string.Empty;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension == ".csv")
                {
                    LoadCsv(filePath);
                }
                else if (extension == ".xlsx")
                {
                    LoadXlsx(filePath);
                }
                else
                {
                    StatusTextBlock.Text = "Unsupported file type. Please select CSV or XLSX.";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                PopulateMappingCombos();
                AutoMapHeaders();
                RefreshPreview();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading roster file: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void LoadCsv(string filePath)
        {
            using var parser = new TextFieldParser(filePath, Encoding.UTF8)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true
            };
            parser.SetDelimiters(",");
            if (parser.EndOfData)
            {
                return;
            }

            _headers = parser.ReadFields()?.Select(h => h?.Trim() ?? string.Empty).ToList()
                ?? new List<string>();

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _headers.Count; i++)
                {
                    var value = i < fields.Length ? fields[i] : string.Empty;
                    row[_headers[i]] = value?.Trim() ?? string.Empty;
                }
                _rawRows.Add(row);
            }
        }

        private void LoadXlsx(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (dataSet.Tables.Count == 0)
            {
                return;
            }

            var table = dataSet.Tables[0];
            _headers = table.Columns.Cast<System.Data.DataColumn>()
                .Select(c => c.ColumnName.Trim())
                .ToList();

            foreach (System.Data.DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in _headers)
                {
                    dict[header] = row[header]?.ToString()?.Trim() ?? string.Empty;
                }
                _rawRows.Add(dict);
            }
        }

        private void PopulateMappingCombos()
        {
            var items = new List<string> { NoneOption };
            items.AddRange(_headers);

            MapDodIdComboBox.ItemsSource = items;
            MapLastNameComboBox.ItemsSource = items;
            MapFirstNameComboBox.ItemsSource = items;
            MapRateComboBox.ItemsSource = items;
            MapRankComboBox.ItemsSource = items;
            MapDutyTypeComboBox.ItemsSource = items;
            MapDutyNumberComboBox.ItemsSource = items;
            MapDutyCombinedComboBox.ItemsSource = items;

            MapDodIdComboBox.SelectedIndex = 0;
            MapLastNameComboBox.SelectedIndex = 0;
            MapFirstNameComboBox.SelectedIndex = 0;
            MapRateComboBox.SelectedIndex = 0;
            MapRankComboBox.SelectedIndex = 0;
            MapDutyTypeComboBox.SelectedIndex = 0;
            MapDutyNumberComboBox.SelectedIndex = 0;
            MapDutyCombinedComboBox.SelectedIndex = 0;
        }

        private void AutoMapHeaders()
        {
            SetIfMatch(MapDodIdComboBox, new[] { "DODID", "DOD_ID", "DOD ID", "EDIPI" });
            SetIfMatch(MapLastNameComboBox, new[] { "LASTNAME", "LAST NAME", "SURNAME", "LNAME" });
            SetIfMatch(MapFirstNameComboBox, new[] { "FIRSTNAME", "FIRST NAME", "GIVENNAME", "FNAME" });
            SetIfMatch(MapRateComboBox, new[] { "RATE", "RATING" });
            SetIfMatch(MapRankComboBox, new[] { "RANK", "GRADE", "PAYGRADE" });
            SetIfMatch(MapDutyTypeComboBox, new[] { "DUTYSECTIONTYPE", "SECTIONTYPE", "DUTY TYPE" });
            SetIfMatch(MapDutyNumberComboBox, new[] { "DUTYSECTIONNUMBER", "SECTIONNUMBER", "DUTY NUMBER" });
            SetIfMatch(MapDutyCombinedComboBox, new[] { "DUTYSECTION", "SECTION", "DUTY SECTION" });
        }

        private void SetIfMatch(ComboBox comboBox, IEnumerable<string> candidates)
        {
            foreach (var header in _headers)
            {
                var normalized = NormalizeHeader(header);
                if (candidates.Any(c => NormalizeHeader(c) == normalized))
                {
                    comboBox.SelectedItem = header;
                    return;
                }
            }
        }

        private static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return string.Empty;
            }

            return new string(header.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            RefreshPreview();
        }

        private void DuplicateHandlingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (_rawRows.Count == 0)
            {
                return;
            }

            var map = BuildFieldMap();
            var records = _importService.NormalizeRows(_rawRows, map);
            _previewRows = BuildPreviewRows(records);
            PreviewDataGrid.ItemsSource = _previewRows;
        }

        private RosterImportFieldMap BuildFieldMap()
        {
            return new RosterImportFieldMap
            {
                DODId = GetSelectedHeader(MapDodIdComboBox),
                LastName = GetSelectedHeader(MapLastNameComboBox),
                FirstName = GetSelectedHeader(MapFirstNameComboBox),
                Rate = GetSelectedHeader(MapRateComboBox),
                Rank = GetSelectedHeader(MapRankComboBox),
                DutySectionType = GetSelectedHeader(MapDutyTypeComboBox),
                DutySectionNumber = GetSelectedHeader(MapDutyNumberComboBox),
                DutySection = GetSelectedHeader(MapDutyCombinedComboBox)
            };
        }

        private static string? GetSelectedHeader(ComboBox comboBox)
        {
            var value = comboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(value) || value == NoneOption)
            {
                return null;
            }

            return value;
        }

        private List<RosterPreviewRow> BuildPreviewRows(List<RosterImportRecord> records)
        {
            using var dbContext = new DatabaseContext();
            dbContext.InitializeDatabase();
            var personnelRepo = new PersonnelRepository();
            var allPersonnel = personnelRepo.GetAllPersonnelAsync(dbContext).GetAwaiter().GetResult();

            var byDodId = allPersonnel
                .Where(p => !string.IsNullOrWhiteSpace(p.DODId))
                .ToDictionary(p => p.DODId.Trim(), StringComparer.OrdinalIgnoreCase);
            var byNameRate = allPersonnel.ToDictionary(
                p => $"{p.LastName}|{p.FirstName}|{p.Rate}".ToUpperInvariant(),
                p => p);

            var duplicateOption = GetDuplicateHandling();

            return records.Select(record =>
            {
                var isDuplicate = false;
                if (!string.IsNullOrWhiteSpace(record.DODId) && byDodId.ContainsKey(record.DODId))
                {
                    isDuplicate = true;
                }
                else
                {
                    var key = $"{record.LastName}|{record.FirstName}|{record.Rate}".ToUpperInvariant();
                    isDuplicate = byNameRate.ContainsKey(key);
                }

                var action = isDuplicate ? "Update" : "Add";
                if (isDuplicate && duplicateOption == DuplicateHandlingOption.Skip)
                {
                    action = "Skip";
                }
                if (isDuplicate && duplicateOption == DuplicateHandlingOption.Prompt)
                {
                    action = "Review";
                }

                var include = action == "Add" || duplicateOption == DuplicateHandlingOption.Update;
                if (action == "Review")
                {
                    include = false;
                }

                return new RosterPreviewRow
                {
                    Include = include,
                    DODId = record.DODId,
                    LastName = record.LastName,
                    FirstName = record.FirstName,
                    Rate = record.Rate,
                    Rank = record.Rank,
                    DutySection = record.DutySectionDisplay,
                    Action = action,
                    Errors = record.IsValid ? string.Empty : string.Join(" ", record.Errors),
                    IsDuplicate = isDuplicate,
                    Record = record
                };
            }).ToList();
        }

        private enum DuplicateHandlingOption
        {
            Prompt,
            Skip,
            Update
        }

        private DuplicateHandlingOption GetDuplicateHandling()
        {
            var selected = (DuplicateHandlingComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            return selected switch
            {
                "Skip Existing" => DuplicateHandlingOption.Skip,
                "Update Existing" => DuplicateHandlingOption.Update,
                _ => DuplicateHandlingOption.Prompt
            };
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (_previewRows.Count == 0)
            {
                StatusTextBlock.Text = "Nothing to import.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var duplicateOption = GetDuplicateHandling();
            var toImport = _previewRows
                .Where(r => r.Include && r.Record.IsValid)
                .ToList();

            if (toImport.Count == 0)
            {
                StatusTextBlock.Text = "No rows selected for import.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                var personnelRepo = new PersonnelRepository();

                var added = 0;
                var updated = 0;
                var skipped = 0;

                foreach (var row in toImport)
                {
                    var record = row.Record;
                    var existing = await FindPersonnelAsync(dbContext, personnelRepo, record);
                    if (existing == null)
                    {
                        var person = new Personnel
                        {
                            DODId = record.DODId,
                            LastName = record.LastName,
                            FirstName = record.FirstName,
                            Rate = record.Rate,
                            Rank = record.Rank
                        };

                        if (!string.IsNullOrWhiteSpace(record.DutySectionNumber) &&
                            !string.IsNullOrWhiteSpace(record.DutySectionType))
                        {
                            person.DutySections.Add((record.DutySectionType, record.DutySectionNumber));
                        }

                        await personnelRepo.AddPersonnelAsync(dbContext, person);
                        added++;
                        continue;
                    }

                    if (duplicateOption == DuplicateHandlingOption.Skip)
                    {
                        skipped++;
                        continue;
                    }

                    existing.DODId = string.IsNullOrWhiteSpace(record.DODId) ? existing.DODId : record.DODId;
                    existing.LastName = record.LastName;
                    existing.FirstName = record.FirstName;
                    existing.Rate = record.Rate;
                    existing.Rank = record.Rank;

                    if (!string.IsNullOrWhiteSpace(record.DutySectionNumber) &&
                        !string.IsNullOrWhiteSpace(record.DutySectionType))
                    {
                        existing.DutySections = new List<(string, string)>
                        {
                            (record.DutySectionType, record.DutySectionNumber)
                        };
                        await personnelRepo.UpdatePersonnelAsync(dbContext, existing);
                    }
                    else
                    {
                        await personnelRepo.UpdatePersonnelFieldsAsync(dbContext, existing);
                    }

                    updated++;
                }

                StatusTextBlock.Text = $"Import complete. Added: {added}, Updated: {updated}, Skipped: {skipped}.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Import failed: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private static async Task<Personnel?> FindPersonnelAsync(
            DatabaseContext dbContext,
            PersonnelRepository repo,
            RosterImportRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.DODId))
            {
                var byDodId = await repo.GetPersonnelByDODIdAsync(dbContext, record.DODId);
                if (byDodId != null)
                {
                    return byDodId;
                }
            }

            if (!string.IsNullOrWhiteSpace(record.LastName) &&
                !string.IsNullOrWhiteSpace(record.FirstName) &&
                !string.IsNullOrWhiteSpace(record.Rate))
            {
                return await repo.GetPersonnelByNameAndRateAsync(dbContext, record.LastName, record.FirstName, record.Rate);
            }

            return null;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
