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
    /// <summary>
    /// Interaction logic for TrainingJacketWindow.xaml
    /// </summary>
    public partial class TrainingJacketWindow : Window
    {
        private PersonnelViewModel _personnel;
        private List<DD2760Form> _dd2760Forms = new List<DD2760Form>();
        private List<AAEScreeningForm> _aaeForms = new List<AAEScreeningForm>();
        private List<QualificationSession> _qualificationSessions = new List<QualificationSession>();

        public TrainingJacketWindow(PersonnelViewModel personnel)
        {
            InitializeComponent();
            _personnel = personnel;
            LoadPersonnelData();
        }

        private void LoadPersonnelData()
        {
            // Update header
            HeaderTextBlock.Text = $"Training Jacket - {_personnel.NameDisplay}";

            // Load biographical information
            NameTextBlock.Text = _personnel.NameDisplay;
            RateTextBlock.Text = _personnel.Rate;
            DODIdTextBlock.Text = _personnel.DODId ?? "Not specified";
            DutySectionsTextBlock.Text = _personnel.DutySectionsDisplay;

            // Load qualifications
            QualificationsDataGrid.ItemsSource = _personnel.Qualifications;
            
            // Load admin requirements
            LoadAdminRequirements();
            
            // Load DD2760 forms
            _ = LoadDD2760Forms(); // Fire and forget
            
            // Load AA&E forms
            _ = LoadAAEForms(); // Fire and forget
            
            // Load 3591/1 sessions
            _ = LoadQualificationSessions(); // Fire and forget
        }
        
        private void LoadAdminRequirements()
        {
            var adminRequirements = _personnel.AdminRequirements;
            
            // Form 2760
            if (adminRequirements?.Form2760Date.HasValue == true)
            {
                Form2760DateTextBlock.Text = adminRequirements.Form2760Date.Value.ToString("yyyy-MM-dd");
                Form2760DateTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                
                // Use the same logic as PersonnelViewModel
                var expirationDate = adminRequirements.Form2760Date.Value.AddYears(1);
                var daysUntilExpiration = (expirationDate - DateTime.Today).Days;
                
                if (daysUntilExpiration < 0)
                {
                    Form2760StatusTextBlock.Text = "EXPIRED";
                    Form2760StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (daysUntilExpiration <= 30)
                {
                    Form2760StatusTextBlock.Text = $"Expires in {daysUntilExpiration} days";
                    Form2760StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    Form2760StatusTextBlock.Text = "Valid";
                    Form2760StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                Form2760DateTextBlock.Text = "Not completed";
                Form2760DateTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                Form2760StatusTextBlock.Text = "Required";
                Form2760StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            // AA&E Screening (populated by LoadAAEForms)
            
            // Deadly Force Training
            if (adminRequirements?.DeadlyForceTrainingDate.HasValue == true)
            {
                DeadlyForceDateTextBlock.Text = adminRequirements.DeadlyForceTrainingDate.Value.ToString("yyyy-MM-dd");
                DeadlyForceDateTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                
                // Use the same logic as PersonnelViewModel (30 days warning)
                var expirationDate = adminRequirements.DeadlyForceTrainingDate.Value.AddMonths(3);
                var daysUntilExpiration = (expirationDate - DateTime.Today).Days;
                
                if (daysUntilExpiration < 0)
                {
                    DeadlyForceStatusTextBlock.Text = "EXPIRED";
                    DeadlyForceStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (daysUntilExpiration <= 30)
                {
                    DeadlyForceStatusTextBlock.Text = $"Expires in {daysUntilExpiration} days";
                    DeadlyForceStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    DeadlyForceStatusTextBlock.Text = "Valid";
                    DeadlyForceStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                DeadlyForceDateTextBlock.Text = "Not completed";
                DeadlyForceDateTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                DeadlyForceStatusTextBlock.Text = "Required";
                DeadlyForceStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void QualificationsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedQualification = QualificationsDataGrid.SelectedItem as Qualification;
            if (selectedQualification != null)
            {
                DisplayQualificationDetails(selectedQualification);
            }
            else
            {
                ClearQualificationDetails();
            }
        }

        private void DisplayQualificationDetails(Qualification qualification)
        {
            QualificationDetailsPanel.Children.Clear();

            // Basic qualification info
            AddDetailRow("Weapon:", qualification.Weapon);
            AddDetailRow("Category:", $"CAT {qualification.Category}");
            AddDetailRow("Date Qualified:", qualification.DateQualified.ToString("yyyy-MM-dd"));
            AddDetailRow("Qualified Underway:", qualification.Details?.QualifiedUnderway == true ? "Yes" : "No");

            // Status information
            if (qualification.Status != null)
            {
                AddDetailRow("Status:", qualification.Status.IsQualified ? "Qualified" : "Disqualified");
                AddDetailRow("Sustainment Due:", qualification.Status.SustainmentDue ? "Yes" : "No");
                AddDetailRow("Expires:", qualification.Status.ExpiresOn.ToString("yyyy-MM-dd"));
                
                // Enhanced sustainment information based on status
                if (qualification.Details?.SustainmentDate.HasValue == true)
                {
                    // Has sustained - show sustainment date
                    AddDetailRow("Sustainment Date:", qualification.Details.SustainmentDate.Value.ToString("dd MMM"));
                }
                else if (qualification.Status.SustainmentDue)
                {
                    // In sustainment window but hasn't sustained yet
                    var sustainmentEnd = qualification.DateQualified.AddDays(240);
                    var daysRemaining = (sustainmentEnd - DateTime.Today).Days;
                    AddDetailRow("Sustainment Window:", $"Eligible ({daysRemaining} days remaining)");
                }
                else if (qualification.Status.DaysUntilExpiration >= 0)
                {
                    // Not yet in sustainment window and not expired - show date range and days until
                    var sustainmentStart = qualification.DateQualified.AddDays(120);
                    var sustainmentEnd = qualification.DateQualified.AddDays(240);
                    AddDetailRow("Sustainment Date Ranges:", $"{sustainmentStart:dd MMM} - {sustainmentEnd:dd MMM}");
                    AddDetailRow("Days Until Sustainment Window:", qualification.Status.DaysUntilSustainment.ToString());
                }
                // If expired, don't show any sustainment window information
                
                // Show "Expired" instead of negative days
                var expirationText = qualification.Status.DaysUntilExpiration < 0 ? "Expired" : qualification.Status.DaysUntilExpiration.ToString();
                AddDetailRow("Days Until Expiration:", expirationText);
            }

            // Weapon-specific details
            if (qualification.Details != null)
            {
                AddDetailRow("", ""); // Spacer

                switch (qualification.Weapon)
                {
                    case "M9":
                        if (qualification.Category == 1)
                        {
                            AddDetailRow("HQC Score:", qualification.Details.HQCScore?.ToString() ?? "Not recorded");
                        }
                        else if (qualification.Category == 2)
                        {
                            AddDetailRow("NHQC Score:", qualification.Details.NHQCScore.ToString());
                            AddDetailRow("HLLC Score:", qualification.Details.HLLCScore.ToString());
                            AddDetailRow("HPWC Score:", qualification.Details.HPWCScore.ToString());
                            AddDetailRow("SAMI:", qualification.Details.Instructor ?? "Not recorded");
                            AddDetailRow("Remarks:", qualification.Details.Remarks ?? "None");
                        }
                        break;

                    case "M4/M16":
                        if (qualification.Category == 2)
                        {
                            AddDetailRow("RQC Score:", qualification.Details.RQCScore?.ToString() ?? "Not recorded");
                            AddDetailRow("RLC Score:", qualification.Details.RLCScore?.ToString() ?? "Not recorded");
                            AddDetailRow("SAMI:", qualification.Details.Instructor ?? "Not recorded");
                            AddDetailRow("Remarks:", qualification.Details.Remarks ?? "None");
                        }
                        break;

                    case "M500":
                        if (qualification.Category == 2)
                        {
                            AddDetailRow("SPWC Score:", qualification.Details.SPWCScore?.ToString() ?? "Not recorded");
                            AddDetailRow("SAMI:", qualification.Details.Instructor ?? "Not recorded");
                            AddDetailRow("Remarks:", qualification.Details.Remarks ?? "None");
                        }
                        break;

                    case "M240":
                    case "M2":
                        if (qualification.Category == 2)
                        {
                            AddDetailRow("COF Score:", qualification.Details.COFScore?.ToString() ?? "Not recorded");
                            AddDetailRow("CSWI:", qualification.Details.CSWI ?? "Not recorded");
                            AddDetailRow("Remarks:", qualification.Details.Remarks ?? "None");
                        }
                        break;
                }

                // Sustainment information
                if (qualification.Details.SustainmentDate.HasValue)
                {
                    AddDetailRow("", ""); // Spacer
                    AddDetailRow("Sustainment Date:", qualification.Details.SustainmentDate.Value.ToString("yyyy-MM-dd"));
                    AddDetailRow("Sustainment Score:", qualification.Details.SustainmentScore?.ToString() ?? "Not recorded");
                }
            }
        }

        private void AddDetailRow(string label, string? value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 10, 2),
                VerticalAlignment = VerticalAlignment.Top
            };

            var valueBlock = new TextBlock
            {
                Text = value ?? "",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Top
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueBlock, 1);

            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBlock);

            QualificationDetailsPanel.Children.Add(grid);
        }

        private void ClearQualificationDetails()
        {
            QualificationDetailsPanel.Children.Clear();
            var textBlock = new TextBlock
            {
                Text = "Select a qualification to view details.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            QualificationDetailsPanel.Children.Add(textBlock);
        }

        private async Task LoadQualificationSessions()
        {
            try
            {
                using var dbContext = new QualTrack.Data.Database.DatabaseContext();
                var qualRepo = new QualTrack.Data.Repositories.QualificationRepository();
                var sessionRepo = new QualTrack.Data.Repositories.QualificationSessionRepository();

                // Get all qualifications for this personnel
                var qualifications = await qualRepo.GetQualificationsForPersonnelAsync(dbContext, _personnel.Id);
                
                // Debug: Show qualification count and session IDs
                var debugInfo = $"Found {qualifications.Count} qualifications for {_personnel.NameDisplay}";
                var sessionIds = qualifications
                    .Where(q => q.QualificationSessionId.HasValue)
                    .Select(q => q.QualificationSessionId.Value)
                    .Distinct()
                    .ToList();
                
                debugInfo += $"\nSession IDs found: {string.Join(", ", sessionIds)}";
                
                if (sessionIds.Any())
                {
                    // Load sessions
                    var sessions = new List<QualTrack.Core.Models.QualificationSession>();
                    foreach (var sessionId in sessionIds)
                    {
                        var session = await sessionRepo.GetSessionByIdAsync(dbContext, sessionId);
                        if (session != null)
                        {
                            sessions.Add(session);
                        }
                    }

                    // Store sessions for printing
                    _qualificationSessions = sessions.OrderByDescending(s => s.DateOfFiring).ToList();

                    // Create display items with computed properties
                    var displayItems = _qualificationSessions.Select(s => new
                    {
                        Session = s,
                        DateOfFiringDisplay = s.DateOfFiringDisplay,
                        ShipStation = s.ShipStation,
                        WeaponsFired = s.WeaponsFired,
                        RangeNameLocation = s.RangeNameLocation,
                        DivisionActivity = s.DivisionActivity,
                        HasPdf = !string.IsNullOrWhiteSpace(s.PdfFilePath) && File.Exists(s.PdfFilePath)
                    }).ToList();

                    Dispatcher.Invoke(() =>
                    {
                        SessionsDataGrid.ItemsSource = displayItems;
                        PdfStatusTextBlock.Text = $"Found {sessions.Count} 3591/1 Form(s)";
                    });
                }
                else
                {
                    SessionsDataGrid.ItemsSource = null;
                    PdfStatusTextBlock.Text = $"No 3591/1 Forms found\n{debugInfo}";
                }
            }
            catch (Exception ex)
            {
                PdfStatusTextBlock.Text = $"Error loading sessions: {ex.Message}";
            }
        }

        private async Task LoadDD2760Forms()
        {
            try
            {
                using var dbContext = new DatabaseContext();
                var dd2760Repo = new DD2760FormRepository();

                _dd2760Forms = await dd2760Repo.GetAllByPersonnelIdAsync(dbContext, _personnel.Id);

                // Create display items with computed properties
                var displayItems = _dd2760Forms.Select(f => new
                {
                    Form = f,
                    DateCompletedDisplay = f.DateCompleted.ToString("yyyy-MM-dd"),
                    DateExpiresDisplay = f.DateExpires.ToString("yyyy-MM-dd"),
                    StatusDisplay = GetDD2760Status(f),
                    HasPdf = !string.IsNullOrWhiteSpace(f.PdfFilePath) && File.Exists(f.PdfFilePath)
                }).ToList();

                Dispatcher.Invoke(() =>
                {
                    DD2760DataGrid.ItemsSource = displayItems;
                    DD2760StatusTextBlock.Text = _dd2760Forms.Count == 0 
                        ? "No DD2760 Forms" 
                        : $"Found {_dd2760Forms.Count} DD2760 Form(s)";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    DD2760StatusTextBlock.Text = $"Error loading DD2760 forms: {ex.Message}";
                });
            }
        }

        private async Task LoadAAEForms()
        {
            try
            {
                using var dbContext = new DatabaseContext();
                var aaeRepo = new AAEScreeningFormRepository();

                _aaeForms = await aaeRepo.GetAllByPersonnelIdAsync(dbContext, _personnel.Id);

                // Create display items with computed properties
                var displayItems = _aaeForms.Select(f => new
                {
                    Form = f,
                    DateCompletedDisplay = f.DateCompleted.ToString("yyyy-MM-dd"),
                    DateExpiresDisplay = f.DateExpires.ToString("yyyy-MM-dd"),
                    StatusDisplay = GetAAEStatus(f),
                    OutcomeDisplay = GetAAEOutcome(f),
                    HasPdf = !string.IsNullOrWhiteSpace(f.PdfFilePath) && File.Exists(f.PdfFilePath)
                }).ToList();

                Dispatcher.Invoke(() =>
                {
                    AAEDataGrid.ItemsSource = displayItems;
                    AAEStatusTextBlock.Text = _aaeForms.Count == 0 
                        ? "No AA&E Forms" 
                        : $"Found {_aaeForms.Count} AA&E Form(s)";

                    if (_aaeForms.Count > 0)
                    {
                        var earliestDate = _aaeForms.Min(f => f.DateCompleted).Date;
                        AAEScreeningDateTextBlock.Text = earliestDate.ToString("yyyy-MM-dd");
                        AAEScreeningDateTextBlock.Foreground = System.Windows.Media.Brushes.Black;

                        var expirationDate = earliestDate.AddYears(1);
                        var daysUntilExpiration = (expirationDate - DateTime.Today).Days;
                        if (daysUntilExpiration < 0)
                        {
                            AAEScreeningStatusTextBlock.Text = "EXPIRED";
                            AAEScreeningStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else if (daysUntilExpiration <= 30)
                        {
                            AAEScreeningStatusTextBlock.Text = $"Expires in {daysUntilExpiration} days";
                            AAEScreeningStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                        }
                        else
                        {
                            AAEScreeningStatusTextBlock.Text = "Valid";
                            AAEScreeningStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                        }
                    }
                    else
                    {
                        AAEScreeningDateTextBlock.Text = "Not completed";
                        AAEScreeningDateTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                        AAEScreeningStatusTextBlock.Text = "Required";
                        AAEScreeningStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    AAEStatusTextBlock.Text = $"Error loading AA&E forms: {ex.Message}";
                });
            }
        }

        private string GetDD2760Status(DD2760Form form)
        {
            var daysUntilExpiration = (form.DateExpires - DateTime.Today).Days;
            if (daysUntilExpiration < 0)
                return "EXPIRED";
            else if (daysUntilExpiration <= 30)
                return $"Expires in {daysUntilExpiration} days";
            else
                return "Valid";
        }

        private string GetAAEStatus(AAEScreeningForm form)
        {
            var daysUntilExpiration = (form.DateExpires - DateTime.Today).Days;
            if (daysUntilExpiration < 0)
                return "EXPIRED";
            else if (daysUntilExpiration <= 30)
                return $"Expires in {daysUntilExpiration} days";
            else
                return "Valid";
        }

        private string GetAAEOutcome(AAEScreeningForm form)
        {
            if (form.Qualified)
                return "Qualified";
            else if (form.Unqualified)
                return "Unqualified";
            else if (form.ReviewLater)
                return "Review Later";
            else
                return "Not Set";
        }

        private void DD2760DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DD2760DataGrid.SelectedItem != null)
            {
                var selectedItem = DD2760DataGrid.SelectedItem;
                var formProperty = selectedItem.GetType().GetProperty("Form");
                if (formProperty != null)
                {
                    var form = formProperty.GetValue(selectedItem) as DD2760Form;
                    if (form != null && !string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        OpenPdf(form.PdfFilePath);
                    }
                }
            }
        }

        private void OpenDD2760Pdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                var selectedItem = button.Tag;
                var formProperty = selectedItem.GetType().GetProperty("Form");
                if (formProperty != null)
                {
                    var form = formProperty.GetValue(selectedItem) as DD2760Form;
                    if (form != null && !string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        OpenPdf(form.PdfFilePath);
                    }
                }
            }
        }

        private void AAEDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AAEDataGrid.SelectedItem != null)
            {
                var selectedItem = AAEDataGrid.SelectedItem;
                var formProperty = selectedItem.GetType().GetProperty("Form");
                if (formProperty != null)
                {
                    var form = formProperty.GetValue(selectedItem) as AAEScreeningForm;
                    if (form != null && !string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        OpenPdf(form.PdfFilePath);
                    }
                }
            }
        }

        private void OpenAAEPdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                var selectedItem = button.Tag;
                var formProperty = selectedItem.GetType().GetProperty("Form");
                if (formProperty != null)
                {
                    var form = formProperty.GetValue(selectedItem) as AAEScreeningForm;
                    if (form != null && !string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        OpenPdf(form.PdfFilePath);
                    }
                }
            }
        }

        private void SessionsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SessionsDataGrid.SelectedItem != null)
            {
                var selectedItem = SessionsDataGrid.SelectedItem;
                var sessionProperty = selectedItem.GetType().GetProperty("Session");
                if (sessionProperty != null)
                {
                    var session = sessionProperty.GetValue(selectedItem) as QualificationSession;
                    if (session != null && !string.IsNullOrWhiteSpace(session.PdfFilePath) && File.Exists(session.PdfFilePath))
                    {
                        OpenPdf(session.PdfFilePath);
                    }
                }
            }
        }

        private void Open3591Pdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QualificationSession session && 
                !string.IsNullOrWhiteSpace(session.PdfFilePath) && 
                File.Exists(session.PdfFilePath))
            {
                OpenPdf(session.PdfFilePath);
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

        private void PrintAllPdfs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pdfPaths = new List<string>();

                // Collect all DD2760 PDFs
                foreach (var form in _dd2760Forms)
                {
                    if (!string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        pdfPaths.Add(form.PdfFilePath);
                    }
                }

                // Collect all AA&E PDFs
                foreach (var form in _aaeForms)
                {
                    if (!string.IsNullOrWhiteSpace(form.PdfFilePath) && File.Exists(form.PdfFilePath))
                    {
                        pdfPaths.Add(form.PdfFilePath);
                    }
                }

                // Collect all 3591/1 PDFs
                foreach (var session in _qualificationSessions)
                {
                    if (!string.IsNullOrWhiteSpace(session.PdfFilePath) && File.Exists(session.PdfFilePath))
                    {
                        pdfPaths.Add(session.PdfFilePath);
                    }
                }

                if (pdfPaths.Count == 0)
                {
                    MessageBox.Show("No PDFs found to print.", "Print All PDFs", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Print all PDFs
                foreach (var pdfPath in pdfPaths)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = pdfPath,
                            Verb = "print",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error printing {Path.GetFileName(pdfPath)}: {ex.Message}", 
                            "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                MessageBox.Show($"Print command sent for {pdfPaths.Count} PDF(s).", 
                    "Print All PDFs", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing PDFs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 