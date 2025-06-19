using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using QualTrack.Core.Models;
using QualTrack.Core.Services;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;
using QualTrack.Data.Services;
using System.IO;
using System.Linq;

namespace QualTrack.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseContext _dbContext;
        private readonly PersonnelRepository _personnelRepository;
        private readonly QualificationRepository _qualificationRepository;
        private readonly QualificationService _qualificationService;
        private readonly AuditService _auditService;
        private readonly ObservableCollection<PersonnelViewModel> _personnelViewModels;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _dbContext = new DatabaseContext();
            _personnelRepository = new PersonnelRepository(_dbContext);
            _qualificationRepository = new QualificationRepository(_dbContext);
            _qualificationService = new QualificationService();
            _auditService = new AuditService(_dbContext);
            
            // Initialize collections
            _personnelViewModels = new ObservableCollection<PersonnelViewModel>();
            PersonnelDataGrid.ItemsSource = _personnelViewModels;
            
            // Load initial data
            LoadData();
            LoadWeaponComboBox();
            PopulateDutySectionLists();
        }

        private async void LoadData()
        {
            try
            {
                var personnel = await _personnelRepository.GetPersonnelWithQualificationsAsync();
                
                _personnelViewModels.Clear();
                foreach (var person in personnel)
                {
                    _personnelViewModels.Add(new PersonnelViewModel(person));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWeaponComboBox()
        {
            var weapons = _qualificationService.GetAllowedWeapons();
            WeaponComboBox.ItemsSource = weapons;
            if (weapons.Any())
            {
                WeaponComboBox.SelectedIndex = 0;
            }
        }

        private void PopulateDutySectionLists()
        {
            DutySections6ListBox.Items.Clear();
            for (int i = 1; i <= 6; i++)
                DutySections6ListBox.Items.Add(i.ToString());
            DutySections3ListBox.Items.Clear();
            for (int i = 1; i <= 3; i++)
                DutySections3ListBox.Items.Add(i.ToString());
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyStatusFilter();
        }

        private void ApplyStatusFilter()
        {
            var selectedItem = StatusFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var filterText = selectedItem.Content.ToString();
            
            // Reload all data and apply filter
            LoadData();
            
            if (filterText != "All")
            {
                var filteredViewModels = _personnelViewModels.Where(pvm =>
                {
                    return filterText switch
                    {
                        "Qualified" => pvm.StatusDisplay.StartsWith("Qualified"),
                        "Sustainment Due" => pvm.StatusDisplay.StartsWith("Sustainment Due"),
                        "Disqualified" => pvm.StatusDisplay.StartsWith("Disqualified"),
                        _ => true
                    };
                }).ToList();

                _personnelViewModels.Clear();
                foreach (var pvm in filteredViewModels)
                {
                    _personnelViewModels.Add(pvm);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCatIIDetailsPanelVisibility();
        }

        private void WeaponComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCatIIDetailsPanelVisibility();
        }

        private void UpdateCatIIDetailsPanelVisibility()
        {
            var weapon = WeaponComboBox.SelectedItem?.ToString();
            var categoryText = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            CatII_M9_DetailsPanel.Visibility = (weapon == "M9" && categoryText == "CAT II") ? Visibility.Visible : Visibility.Collapsed;
            CatII_M4M16_DetailsPanel.Visibility = (weapon == "M4/M16" && categoryText == "CAT II") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void QuickAddAnotherQualificationButton_Click(object sender, RoutedEventArgs e)
        {
            ClearQualificationFieldsOnly();
        }

        private void ClearQualificationFieldsOnly()
        {
            DateQualifiedPicker.SelectedDate = null;
            if (WeaponComboBox.Items.Count > 0)
            {
                WeaponComboBox.SelectedIndex = 0;
            }
            if (CategoryComboBox.Items.Count > 0)
            {
                CategoryComboBox.SelectedIndex = 0;
            }
            NHQCScoreTextBox.Text = string.Empty;
            HLLCScoreTextBox.Text = string.Empty;
            HPWCScoreTextBox.Text = string.Empty;
            InstructorTextBox.Text = string.Empty;
            RemarksTextBox.Text = string.Empty;
            CatII_M9_DetailsPanel.Visibility = Visibility.Collapsed;
            RQCScoreTextBox.Text = string.Empty;
            RLCScoreTextBox.Text = string.Empty;
            RifleInstructorTextBox.Text = string.Empty;
            RifleRemarksTextBox.Text = string.Empty;
            CatII_M4M16_DetailsPanel.Visibility = Visibility.Collapsed;
        }

        private async void AddQualificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(NameTextBox.Text) || 
                    string.IsNullOrWhiteSpace(RateTextBox.Text) ||
                    WeaponComboBox.SelectedItem == null ||
                    CategoryComboBox.SelectedItem == null ||
                    DateQualifiedPicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(DODIdTextBox.Text) ||
                    DutySections6ListBox.SelectedItems.Count == 0 && DutySections3ListBox.SelectedItems.Count == 0)
                {
                    StatusTextBlock.Text = "Please fill in all required fields and select at least one duty section.";
                    return;
                }

                var weapon = WeaponComboBox.SelectedItem.ToString()!;
                var categoryText = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()!;
                int category = categoryText switch
                {
                    "CAT I" => 1,
                    "CAT II" => 2,
                    "CAT III" => 3,
                    "CAT IV" => 4,
                    _ => throw new Exception("Invalid category")
                };
                var dateQualified = DateQualifiedPicker.SelectedDate.Value;
                var dodId = DODIdTextBox.Text;
                var dutySections6 = DutySections6ListBox.SelectedItems.Cast<string>().Select(s => ("6", s)).ToList();
                var dutySections3 = DutySections3ListBox.SelectedItems.Cast<string>().Select(s => ("3", s)).ToList();
                var allDutySections = dutySections6.Concat(dutySections3).ToList();

                // Validate weapon
                if (!_qualificationService.IsWeaponAllowed(weapon))
                {
                    StatusTextBlock.Text = "Invalid weapon selected.";
                    return;
                }

                // Check for existing personnel by DOD ID or Name+Rate
                var personnelByDodId = await _personnelRepository.GetAllPersonnelAsync();
                var existingPersonnel = personnelByDodId.FirstOrDefault(p => p.DODId == dodId || (p.Name == NameTextBox.Text && p.Rate == RateTextBox.Text));
                Personnel personnel;
                if (existingPersonnel != null)
                {
                    // Prompt user to merge
                    var result = MessageBox.Show($"A sailor with this DOD ID or Name/Rate already exists. Do you want to merge and add a new qualification to this sailor?", "Merge Personnel", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        personnel = existingPersonnel;
                        // Optionally update DOD ID, duty sections, etc.
                        personnel.DODId = dodId;
                        personnel.DutySections = allDutySections;
                        await _personnelRepository.UpdatePersonnelAsync(personnel);
                    }
                    else
                    {
                        StatusTextBlock.Text = "Operation cancelled by user.";
                        return;
                    }
                }
                else
                {
                    // Create new personnel
                    personnel = new Personnel(NameTextBox.Text, RateTextBox.Text)
                    {
                        DODId = dodId,
                        DutySections = allDutySections
                    };
                    personnel.Id = await _personnelRepository.AddPersonnelAsync(personnel);
                }

                // Check if qualification already exists
                if (await _qualificationRepository.QualificationExistsAsync(personnel.Id, weapon))
                {
                    StatusTextBlock.Text = $"Qualification for {weapon} already exists for this personnel.";
                    return;
                }

                // Add qualification
                var qualification = new Qualification(weapon, category, dateQualified)
                {
                    PersonnelId = personnel.Id
                };

                // If CAT II and M9, collect new details
                if (weapon == "M9" && categoryText == "CAT II")
                {
                    if (!int.TryParse(NHQCScoreTextBox.Text, out int nhqcScore) ||
                        !int.TryParse(HLLCScoreTextBox.Text, out int hllcScore) ||
                        !int.TryParse(HPWCScoreTextBox.Text, out int hpwcScore))
                    {
                        StatusTextBlock.Text = "Please enter valid numeric scores for NHQC, HLLC, and HPWC.";
                        return;
                    }
                    qualification.Details = new QualificationDetails
                    {
                        NHQCScore = nhqcScore,
                        HLLCScore = hllcScore,
                        HPWCScore = hpwcScore,
                        Instructor = InstructorTextBox.Text,
                        Remarks = RemarksTextBox.Text
                    };
                }
                // If CAT II and M4/M16, collect rifle details
                else if (weapon == "M4/M16" && categoryText == "CAT II")
                {
                    if (!int.TryParse(RQCScoreTextBox.Text, out int rqcScore) ||
                        !int.TryParse(RLCScoreTextBox.Text, out int rlcScore))
                    {
                        StatusTextBlock.Text = "Please enter valid numeric scores for RQC and RLC.";
                        return;
                    }
                    qualification.Details = new QualificationDetails
                    {
                        RQCScore = rqcScore,
                        RLCScore = rlcScore,
                        Instructor = RifleInstructorTextBox.Text,
                        Remarks = RifleRemarksTextBox.Text
                    };
                }

                await _qualificationRepository.AddQualificationAsync(qualification);

                // Log the action
                await _auditService.LogQualificationActionAsync("Add Qualification", personnel.Name, weapon, category);

                // Clear only qualification fields for quick add
                ClearQualificationFieldsOnly();
                LoadData();
                StatusTextBlock.Text = "Qualification added successfully!";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error adding qualification: {ex.Message}";
            }
        }

        private void ClearQualificationForm()
        {
            NameTextBox.Text = string.Empty;
            RateTextBox.Text = string.Empty;
            DODIdTextBox.Text = string.Empty;
            DutySections6ListBox.Items.Clear();
            DutySections3ListBox.Items.Clear();
            PopulateDutySectionLists();
            ClearQualificationFieldsOnly();
        }

        private void AddPersonnel_Click(object sender, RoutedEventArgs e)
        {
            // For now, just show a message - could be expanded to a separate window
            MessageBox.Show("Use the 'Add Qualification' tab to add personnel. Personnel will be created automatically when adding qualifications.", 
                "Add Personnel", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewPersonnel_Click(object sender, RoutedEventArgs e)
        {
            // Switch to dashboard tab
            MainTabControl.SelectedIndex = 0;
        }

        private void AddQualification_Click(object sender, RoutedEventArgs e)
        {
            // Switch to add qualification tab
            MainTabControl.SelectedIndex = 1;
        }

        private void ViewQualifications_Click(object sender, RoutedEventArgs e)
        {
            // Switch to dashboard tab
            MainTabControl.SelectedIndex = 0;
        }

        private async void ExportQualifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"QualTrack_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var personnel = await _personnelRepository.GetPersonnelWithQualificationsAsync();
                    
                    using var writer = new StreamWriter(saveFileDialog.FileName);
                    
                    // Write header
                    await writer.WriteLineAsync("Name,Rate,Duty Sections,Weapon,Category,Date Qualified,Status,Expires On,Days Until Expiration");
                    
                    // Write data
                    foreach (var person in personnel)
                    {
                        foreach (var qualification in person.Qualifications)
                        {
                            var dutySections = string.Join(";", person.DutySections.Select(ds => $"{ds.Item1} - {ds.Item2}"));
                            var status = qualification.Status?.IsQualified == true ? 
                                (qualification.Status.SustainmentDue ? "Sustainment Due" : "Qualified") : 
                                "Disqualified";
                            
                            await writer.WriteLineAsync(
                                $"\"{person.Name}\",\"{person.Rate}\",\"{dutySections}\",\"{qualification.Weapon}\"," +
                                $"{qualification.Category},\"{qualification.DateQualified:yyyy-MM-dd}\",\"{status}\"," +
                                $"\"{qualification.Status?.ExpiresOn:yyyy-MM-dd}\",{qualification.Status?.DaysUntilExpiration}");
                        }
                    }
                    
                    MessageBox.Show($"Data exported successfully to {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Log the export action
                    var totalRecords = personnel.Sum(p => p.Qualifications.Count);
                    await _auditService.LogExportActionAsync(saveFileDialog.FileName, totalRecords);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void LoadTestData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await TestData.PopulateTestDataAsync();
                LoadData();
                MessageBox.Show("Test data loaded successfully!", "Test Data", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading test data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _dbContext?.Dispose();
            base.OnClosed(e);
        }
    }
} 