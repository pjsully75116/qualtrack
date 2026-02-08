using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QualTrack.Core.Models;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;

namespace QualTrack.UI
{
    public partial class InstructorJacketWindow : Window
    {
        private readonly int _personnelId;
        private Personnel? _personnel;

        private sealed class DesignationRow
        {
            public string Role { get; set; } = string.Empty;
            public string DateDisplay { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string? PdfPath { get; set; }
            public bool HasPdf => !string.IsNullOrWhiteSpace(PdfPath) && File.Exists(PdfPath);
        }

        private sealed class QualificationRow
        {
            public string Role { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string DateDisplay { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string? PdfPath { get; set; }
            public bool HasPdf => !string.IsNullOrWhiteSpace(PdfPath) && File.Exists(PdfPath);
        }

        public InstructorJacketWindow(int personnelId)
        {
            InitializeComponent();
            _personnelId = personnelId;
            _ = LoadInstructorData();
        }

        private async Task LoadInstructorData()
        {
            try
            {
                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                var personnelRepo = new PersonnelRepository();
                var credentialRepo = new InstructorCredentialRepository();

                _personnel = await personnelRepo.GetPersonnelByIdAsync(dbContext, _personnelId);
                if (_personnel == null)
                {
                    MessageBox.Show("Unable to load instructor data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                var roles = new List<string>();
                if (_personnel.IsSami) roles.Add("SAMI");
                if (_personnel.IsCswi) roles.Add("CSWI");

                HeaderTextBlock.Text = $"{string.Join("/", roles)} Jacket - {_personnel.LastName}, {_personnel.FirstName}";
                NameTextBlock.Text = $"{_personnel.LastName}, {_personnel.FirstName}";
                RateTextBlock.Text = _personnel.Rate;
                DODIdTextBlock.Text = _personnel.DODId;
                RolesTextBlock.Text = roles.Count > 0 ? string.Join(", ", roles) : "None";

                var designationRows = new List<DesignationRow>();
                foreach (var role in roles)
                {
                    var designation = await credentialRepo.GetDesignationAsync(dbContext, _personnelId, role);
                    if (designation != null)
                    {
                        designationRows.Add(new DesignationRow
                        {
                            Role = role,
                            DateDisplay = designation.DesignationDate.ToString("yyyy-MM-dd"),
                            FileName = designation.PdfFileName ?? string.Empty,
                            PdfPath = designation.PdfFilePath
                        });
                    }
                    else
                    {
                        designationRows.Add(new DesignationRow
                        {
                            Role = role,
                            DateDisplay = "Not on file",
                            FileName = string.Empty,
                            PdfPath = null
                        });
                    }
                }

                DesignationDataGrid.ItemsSource = designationRows;

                var qualificationRows = new List<QualificationRow>();
                foreach (var role in roles)
                {
                    var qualifications = await credentialRepo.GetQualificationsAsync(dbContext, _personnelId, role);
                    qualificationRows.AddRange(qualifications.Select(q => new QualificationRow
                    {
                        Role = role,
                        Type = q.QualificationType,
                        DateDisplay = q.QualificationDate.ToString("yyyy-MM-dd"),
                        FileName = q.PdfFileName ?? string.Empty,
                        PdfPath = q.PdfFilePath
                    }));
                }

                InstructorQualificationsDataGrid.ItemsSource = qualificationRows
                    .OrderByDescending(q => q.DateDisplay)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading instructor data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDesignationPdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DesignationRow row && row.HasPdf && row.PdfPath != null)
            {
                OpenPdf(row.PdfPath);
            }
        }

        private void OpenQualificationPdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QualificationRow row && row.HasPdf && row.PdfPath != null)
            {
                OpenPdf(row.PdfPath);
            }
        }

        private void OpenPdf(string pdfPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
