using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using QualTrack.Core.Models;
using QualTrack.Core.Services;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;
using QualTrack.Data.Services;
using QualTrack.UI.Services;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using QualTrack.UI;
using QualTrack.UI.Models;
using System.Text;


namespace QualTrack.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly QualificationService _qualificationService;
        private readonly ObservableCollection<PersonnelViewModel> _personnelViewModels;
        private string _selectedDutySectionType = "All";
        private string _selectedDutySectionNumber = "All";
        private string _selectedWeapon = "All";
        private string _selectedStatus = "All";
        private DashboardColumnSettings _dashboardSettings = new DashboardColumnSettings();
        private readonly IRbacService _rbacService = new RbacService();
        private readonly ICurrentUserContext _currentUserContext = new LocalUserContext();
        
        // Performance optimization: Cache all personnel data
        private List<PersonnelViewModel> _allPersonnelCache = new List<PersonnelViewModel>();
        private bool _isFiltering = false;
        
        // Performance monitoring
        private readonly System.Diagnostics.Stopwatch _loadStopwatch = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch _filterStopwatch = new System.Diagnostics.Stopwatch();
        private int _totalLoadTime = 0;
        private int _totalFilterTime = 0;
        private int _loadCount = 0;
        private int _filterCount = 0;
        private readonly List<string> _performanceLog = new List<string>();
        private readonly object _logLock = new object();

        // --- Document upload fields ---
        private string _upload3591FilePath = string.Empty;
        private readonly DocumentService _documentService = new DocumentService();
        private readonly IDocumentRepository _documentRepository = new DocumentRepository(new DatabaseContext());
        private readonly PdfGenerationService _pdfGenerationService = new PdfGenerationService();



        private ObservableCollection<SailorQualification> deSailorQualifications = new ObservableCollection<SailorQualification>();
        private ObservableCollection<CrewServedWeaponEntry> cswEntries = new ObservableCollection<CrewServedWeaponEntry>();

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _qualificationService = new QualificationService();
            
            // Initialize collections
            _personnelViewModels = new ObservableCollection<PersonnelViewModel>();
            PersonnelDataGrid.ItemsSource = _personnelViewModels;
            
            // Load initial data after UI is loaded
            Loaded += MainWindow_Loaded;

            UpdateRoleMenuChecks();
            ApplyRbacToUi();
        }

        private void RoleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string roleTag
                && Enum.TryParse(roleTag, out RbacRole role))
            {
                _currentUserContext.Role = role;
                UpdateRoleMenuChecks();
                ApplyRbacToUi();
            }
        }

        private void UpdateRoleMenuChecks()
        {
            foreach (var roleMenuItem in GetRoleMenuItems())
            {
                roleMenuItem.IsChecked = roleMenuItem.Tag?.ToString() == _currentUserContext.Role.ToString();
            }
        }

        private IEnumerable<MenuItem> GetRoleMenuItems()
        {
            if (RoleMenu != null)
            {
                foreach (var child in RoleMenu.Items.OfType<MenuItem>())
                {
                    yield return child;
                }
            }
        }

        private void ApplyRbacToUi()
        {
            DashboardTab.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ViewDashboard);
            AddAdminTab.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageAdminForms);
            DigitalEntryTab.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageQualifications);
            CrewServedTab.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageCrewServed);
            SupportTab.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ViewDashboard);

            DashboardSetupButton.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ConfigureDashboard);
            ExportQualificationsMenuItem.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageSystem);
            LoadTestDataMenuItem.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageSystem);
            AddPersonnelMenuItem.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManagePersonnel);
            AddQualificationMenuItem.IsEnabled = _rbacService.HasPermission(_currentUserContext.Role, RbacPermission.ManageQualifications);
        }

        private bool RequirePermission(RbacPermission permission, string action)
        {
            if (_rbacService.HasPermission(_currentUserContext.Role, permission))
            {
                return true;
            }

            MessageBox.Show($"Access denied for action: {action}. Current role: {_currentUserContext.Role}.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.Source, MainTabControl))
            {
                return;
            }

            if (MainTabControl.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Header?.ToString() == "3591/2 Digital Entry")
                {
                    await InitializeCrewServedWeaponTab();
                }
            }
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Populate test data if DB is empty
                await EnsureTestData();
                
                // Load initial data
                await LoadData();
                ApplyColumnVisibility();
                
                // Initialize performance display first
                UpdatePerformanceDisplay();
                
                // Start performance monitoring timer after UI is ready
                StartPerformanceMonitoring();
                await InitializeDigitalEntryTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPerformanceMonitoring()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Log every 5 minutes
            };
            timer.Tick += (s, e) => LogPerformanceStats();
            timer.Start();
            
            // Also update display every 30 seconds
            var displayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            displayTimer.Tick += (s, e) => UpdatePerformanceDisplay();
            displayTimer.Start();
        }

        private void LogPerformanceStats()
        {
            var cacheStats = _qualificationService.GetCacheStats();
            var avgLoadTime = _loadCount > 0 ? _totalLoadTime / _loadCount : 0;
            var avgFilterTime = _filterCount > 0 ? _totalFilterTime / _filterCount : 0;
            
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Load: {avgLoadTime}ms avg ({_loadCount} calls), " +
                          $"Filter: {avgFilterTime}ms avg ({_filterCount} calls), " +
                          $"Cache: {cacheStats.CacheSize} entries, " +
                          $"Expired: {cacheStats.ExpiredEntries}";
            
            lock (_logLock)
            {
                _performanceLog.Add(logEntry);
                // Keep only last 50 entries
                if (_performanceLog.Count > 50)
                {
                    _performanceLog.RemoveAt(0);
                }
            }
            
            System.Diagnostics.Debug.WriteLine(logEntry);
        }

        private void UpdatePerformanceDisplay()
        {
            try
            {
                // Check if UI elements are initialized
                if (AvgLoadTimeText == null || LoadCountText == null || CacheSizeText == null)
                {
                    return; // UI not ready yet
                }

                var stats = GetPerformanceStats();
                var cacheStats = _qualificationService.GetCacheStats();
                
                // Update UI on UI thread
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        AvgLoadTimeText.Text = $"{stats.AvgLoadTime} ms";
                        LoadCountText.Text = stats.LoadCount.ToString();
                        AvgFilterTimeText.Text = $"{stats.AvgFilterTime} ms";
                        FilterCountText.Text = stats.FilterCount.ToString();
                        
                        CacheSizeText.Text = $"{cacheStats.CacheSize} entries";
                        ExpiredEntriesText.Text = cacheStats.ExpiredEntries.ToString();
                        
                        // Calculate cache hit rate (simplified)
                        var totalCacheRequests = stats.LoadCount + stats.FilterCount;
                        var hitRate = totalCacheRequests > 0 ? (cacheStats.CacheSize * 100.0 / totalCacheRequests) : 0;
                        CacheHitRateText.Text = $"{Math.Min(100, Math.Max(0, hitRate)):F1}%";
                        
                        // Update data counts
                        TotalPersonnelText.Text = _allPersonnelCache.Count.ToString();
                        var totalQuals = _allPersonnelCache.Sum(p => p.Qualifications.Count);
                        TotalQualificationsText.Text = totalQuals.ToString();
                        

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating UI elements: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating performance display: {ex.Message}");
            }
        }

        private async Task EnsureTestData()
        {
            using var dbContext = new DatabaseContext();
            dbContext.InitializeDatabase();
            
            var personnelRepo = new PersonnelRepository();
            var personnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
            if (personnel.Count == 0)
            {
                await TestData.PopulateTestDataAsync();
            }
        }

        private async Task LoadData()
        {
            try
            {
                _loadStopwatch.Restart();
                
                // Only reload from database if not filtering
                if (!_isFiltering)
                {
                    using var dbContext = new DatabaseContext();
                    dbContext.InitializeDatabase();
                    
                    var personnelRepo = new PersonnelRepository();
                    var personnel = await personnelRepo.GetPersonnelWithQualificationsAsync(dbContext);
                    var dd2760Repo = new DD2760FormRepository();
                    var aaeRepo = new AAEScreeningFormRepository();
                    
                    _allPersonnelCache.Clear();
                    foreach (var person in personnel)
                    {
                        var viewModel = new PersonnelViewModel(person);
                        
                        // Load DD2760 form for this personnel
                        var dd2760Form = await dd2760Repo.GetByPersonnelIdAsync(dbContext, person.Id);
                        if (dd2760Form != null)
                        {
                            viewModel.DD2760Form = dd2760Form;
                            viewModel.DD2760ExpirationDate = dd2760Form.DateExpires;
                        }
                        
                        // Load all AA&E forms for this personnel
                        var aaeForms = await aaeRepo.GetAllByPersonnelIdAsync(dbContext, person.Id);
                        if (aaeForms != null && aaeForms.Any())
                        {
                            viewModel.AAEScreeningForms = aaeForms;
                            
                            // Find the earliest DateCompleted from all AA&E forms
                            var earliestDate = aaeForms.Min(f => f.DateCompleted);
                            viewModel.AAEScreeningEarliestDate = earliestDate;
                            
                            // Calculate expiration (1 year from earliest date)
                            viewModel.AAEScreeningExpirationDate = earliestDate.AddYears(1);
                        }
                        
                        _allPersonnelCache.Add(viewModel);
                    }
                }
                
                ApplyFilters();
                ApplyColumnVisibility();
                PersonnelDataGrid.Items.Refresh();
                
                _loadStopwatch.Stop();
                _totalLoadTime += (int)_loadStopwatch.ElapsedMilliseconds;
                _loadCount++;
                
                // Log performance for large datasets
                if (_allPersonnelCache.Count > 100)
                {
                    var logEntry = $"[{DateTime.Now:HH:mm:ss}] Loaded {_allPersonnelCache.Count} personnel in {_loadStopwatch.ElapsedMilliseconds}ms";
                    lock (_logLock)
                    {
                        _performanceLog.Add(logEntry);
                        if (_performanceLog.Count > 50) _performanceLog.RemoveAt(0);
                    }
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedStatus = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            ApplyFilters();
        }

        private void DutySectionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDutySectionType = (DutySectionTypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            
            // Update section number options
            DutySectionNumberFilterComboBox.Items.Clear();
            var allItem = new ComboBoxItem { Content = "All" };
            DutySectionNumberFilterComboBox.Items.Add(allItem);
            if (_selectedDutySectionType == "3 Section")
            {
                for (int i = 1; i <= 3; i++)
                    DutySectionNumberFilterComboBox.Items.Add(new ComboBoxItem { Content = i.ToString() });
            }
            else if (_selectedDutySectionType == "6 Section")
            {
                for (int i = 1; i <= 6; i++)
                    DutySectionNumberFilterComboBox.Items.Add(new ComboBoxItem { Content = i.ToString() });
            }
            DutySectionNumberFilterComboBox.SelectedIndex = 0;
            _selectedDutySectionNumber = "All";
            ApplyFilters();
        }

        private void DutySectionNumberFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDutySectionNumber = (DutySectionNumberFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            ApplyFilters();
        }

        private void WeaponFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedWeapon = (WeaponFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filterStopwatch.Restart();
            _isFiltering = true;
            
            // Apply all filters to cached data
            var filtered = _allPersonnelCache.AsEnumerable();
            
            // Status filter
            if (_selectedStatus != "All")
            {
                filtered = filtered.Where(pvm =>
                {
                    switch (_selectedStatus)
                    {
                        case "Qualified":
                            return pvm.Qualifications.Any(q => q.Status?.IsQualified == true && !q.Status.SustainmentDue);
                        case "Sustainment Due":
                            return pvm.Qualifications.Any(q => q.Status?.SustainmentDue == true);
                        case "Lapsed":
                            return !string.IsNullOrWhiteSpace(pvm.LapsedQualificationsDisplay);
                        default:
                            return true;
                    }
                });
            }
            
            // Duty section type filter
            if (_selectedDutySectionType != "All")
            {
                filtered = filtered.Where(pvm => pvm.DutySections.Any(ds =>
                    (_selectedDutySectionType == "3 Section" && ds.Type == "3") ||
                    (_selectedDutySectionType == "6 Section" && ds.Type == "6")));
            }
            
            // Duty section number filter
            if (_selectedDutySectionNumber != "All")
            {
                filtered = filtered.Where(pvm => pvm.DutySections.Any(ds => ds.Section == _selectedDutySectionNumber));
            }
            
            // Weapon filter
            if (_selectedWeapon != "All")
            {
                if (_selectedWeapon == ".50")
                {
                    filtered = filtered.Where(pvm => pvm.Qualifications.Any(q =>
                        (q.Weapon == "M2" || q.Weapon == "M2A1") && q.Status?.IsQualified == true));
                }
                else
                {
                    filtered = filtered.Where(pvm => pvm.Qualifications.Any(q => q.Weapon == _selectedWeapon && q.Status?.IsQualified == true));
                }
            }
            
            // Update ObservableCollection efficiently
            var filteredList = filtered.ToList();
            _personnelViewModels.Clear();
            foreach (var pvm in filteredList)
            {
                _personnelViewModels.Add(pvm);
            }
            
            _isFiltering = false;
            _filterStopwatch.Stop();
            _totalFilterTime += (int)_filterStopwatch.ElapsedMilliseconds;
            _filterCount++;
            
            // Log performance for large datasets
            if (_allPersonnelCache.Count > 100)
            {
                var logEntry = $"[{DateTime.Now:HH:mm:ss}] Filtered {_allPersonnelCache.Count} -> {filteredList.Count} personnel in {_filterStopwatch.ElapsedMilliseconds}ms";
                lock (_logLock)
                {
                    _performanceLog.Add(logEntry);
                    if (_performanceLog.Count > 50) _performanceLog.RemoveAt(0);
                }
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
        }

        private void DashboardSetup_Click(object sender, RoutedEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ConfigureDashboard, "Configure Dashboard"))
            {
                return;
            }

            var setupWindow = new DashboardSetupWindow(_dashboardSettings);
            setupWindow.Owner = this;
            
            if (setupWindow.ShowDialog() == true)
            {
                _dashboardSettings = setupWindow.ColumnSettings;
                ApplyColumnVisibility();
            }
        }

        private void PersonnelDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ViewTrainingJacket, "View Training Jacket"))
            {
                return;
            }

            var selectedPersonnel = PersonnelDataGrid.SelectedItem as PersonnelViewModel;
            if (selectedPersonnel != null)
            {
                var trainingJacketWindow = new TrainingJacketWindow(selectedPersonnel);
                trainingJacketWindow.Show(); // Non-modal window
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Reset all filters
            StatusFilterComboBox.SelectedIndex = 0;
            DutySectionTypeFilterComboBox.SelectedIndex = 0;
            DutySectionNumberFilterComboBox.SelectedIndex = 0;
            WeaponFilterComboBox.SelectedIndex = 0;
            _selectedDutySectionType = "All";
            _selectedDutySectionNumber = "All";
            _selectedWeapon = "All";
            _selectedStatus = "All";
            
            // Reload data from database
            await LoadData();
        }





























        private void AddPersonnel_Click(object sender, RoutedEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ManagePersonnel, "Add Personnel"))
            {
                return;
            }

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
            if (!RequirePermission(RbacPermission.ManageQualifications, "Add Qualification"))
            {
                return;
            }

            // Switch to add qualification tab
            MainTabControl.SelectedIndex = 1;
        }

        private void ViewQualifications_Click(object sender, RoutedEventArgs e)
        {
            // Switch to dashboard tab
            MainTabControl.SelectedIndex = 0;
        }

        // Admin form event handlers
        private async void Submit2760Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(AdminLastNameTextBox.Text) || 
                    string.IsNullOrWhiteSpace(AdminFirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AdminDODIdTextBox.Text))
                {
                    AdminStatusTextBlock.Text = "Please fill in all required fields (Last Name, First Name, DOD ID)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate DD2760 form - Date Signed is required
                if (!DD2760DateSignedPicker.SelectedDate.HasValue)
                {
                    AdminStatusTextBlock.Text = "Please select a Date Signed for the 2760 form";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate that exactly one domestic violence response is selected
                int checkedCount = 0;
                if (DD2760YesCheckBox.IsChecked == true) checkedCount++;
                if (DD2760NoCheckBox.IsChecked == true) checkedCount++;
                if (DD2760UnknownCheckBox.IsChecked == true) checkedCount++;

                if (checkedCount == 0)
                {
                    AdminStatusTextBlock.Text = "Please select one response for the domestic violence question (Yes, No, or Unknown)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                if (checkedCount > 1)
                {
                    AdminStatusTextBlock.Text = "Please select only ONE response for the domestic violence question (Yes, No, or Unknown)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                
                var personnelRepo = new PersonnelRepository();
                var additionalRequirementsRepo = new AdditionalRequirementsRepository(dbContext);
                var dd2760Repo = new DD2760FormRepository();

                // Check if personnel exists
                var existingPersonnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, AdminDODIdTextBox.Text.Trim());
                Personnel? personnelForPdf;
                int personnelId;

                if (existingPersonnel == null)
                {
                    // Create new personnel
                    var newPersonnel = new Personnel
                    {
                        LastName = AdminLastNameTextBox.Text.Trim(),
                        FirstName = AdminFirstNameTextBox.Text.Trim(),
                        DODId = AdminDODIdTextBox.Text.Trim(),
                        Rate = AdminRateComboBox.SelectedItem?.ToString() ?? "ET",
                        Rank = AdminRankComboBox.SelectedItem?.ToString() ?? "E-1"
                    };

                    personnelId = await personnelRepo.AddPersonnelAsync(dbContext, newPersonnel);
                    newPersonnel.Id = personnelId;
                    personnelForPdf = newPersonnel;
                }
                else
                {
                    personnelId = existingPersonnel.Id;
                    personnelForPdf = existingPersonnel;
                }

                // Get or create admin requirements
                var adminRequirements = new AdditionalRequirements
                {
                    PersonnelId = personnelId,
                    Form2760Date = DD2760DateSignedPicker.SelectedDate.Value
                };

                // Save admin requirements
                var success = await additionalRequirementsRepo.SaveAsync(personnelId, adminRequirements);

                // Save DD2760 form
                string? domesticViolenceResponse = null;
                string? domesticViolenceInitials = null;
                
                if (DD2760YesCheckBox.IsChecked == true)
                {
                    domesticViolenceResponse = "yes";
                    domesticViolenceInitials = DD2760YesInitialsTextBox.Text.Trim();
                }
                else if (DD2760NoCheckBox.IsChecked == true)
                {
                    domesticViolenceResponse = "no";
                    domesticViolenceInitials = DD2760NoInitialsTextBox.Text.Trim();
                }
                else if (DD2760UnknownCheckBox.IsChecked == true)
                {
                    domesticViolenceResponse = "dontknow";
                    domesticViolenceInitials = DD2760UnknownInitialsTextBox.Text.Trim();
                }
                
                var dd2760Form = new DD2760Form
                {
                    PersonnelId = personnelId,
                    DateCompleted = DD2760DateSignedPicker.SelectedDate.Value,
                    DateExpires = DD2760DateSignedPicker.SelectedDate.Value.AddYears(1),
                    DateCreated = DateTime.Now,
                    DomesticViolenceResponse = domesticViolenceResponse,
                    DomesticViolenceInitials = domesticViolenceInitials,
                    DomesticViolenceDate = DD2760DomesticViolenceDatePicker.SelectedDate,
                    CourtJurisdiction = string.IsNullOrWhiteSpace(DD2760CourtJurisdictionTextBox.Text) ? null : DD2760CourtJurisdictionTextBox.Text.Trim(),
                    DocketCaseNumber = string.IsNullOrWhiteSpace(DD2760DocketCaseNumberTextBox.Text) ? null : DD2760DocketCaseNumberTextBox.Text.Trim(),
                    StatuteCharge = string.IsNullOrWhiteSpace(DD2760StatuteChargeTextBox.Text) ? null : DD2760StatuteChargeTextBox.Text.Trim(),
                    DateSentenced = DD2760DateSentencedPicker.SelectedDate,
                    CertifierSSN = string.IsNullOrWhiteSpace(DD2760SSNTextBox.Text) ? null : DD2760SSNTextBox.Text.Trim(),
                    IsCertified = DD2760CertificationCheckBox.IsChecked == true,
                    CertifierSignatureDate = DD2760DateSignedPicker.SelectedDate
                };
                
                var dd2760FormId = await dd2760Repo.AddAsync(dbContext, dd2760Form);
                
                // Update the form with the generated ID
                dd2760Form.Id = dd2760FormId;

                // Generate PDF
                try
                {
                    var pdfService = new DD2760PdfGenerationService();
                    var pdfPath = pdfService.GenerateDD2760Pdf(dd2760Form, personnelForPdf);

                    var signerName = $"{AdminLastNameTextBox.Text}, {AdminFirstNameTextBox.Text}".Trim(' ', ',');
                    var signedPath = await TrySignPdfWithCacAsync(pdfPath, "DD2760 Admin Form", signerName);
                    if (!string.IsNullOrWhiteSpace(signedPath))
                    {
                        pdfPath = signedPath;
                    }
                    
                    // Update the form with PDF path
                    dd2760Form.PdfFilePath = pdfPath;
                    dd2760Form.PdfFileName = Path.GetFileName(pdfPath);
                    await dd2760Repo.UpdateAsync(dbContext, dd2760Form);
                    
                    // Display the PDF
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception pdfEx)
                {
                    AdminStatusTextBlock.Text = $"Form saved but PDF generation failed: {pdfEx.Message}";
                    AdminStatusTextBlock.Foreground = Brushes.Orange;
                }

                if (success)
                {
                    if (string.IsNullOrEmpty(AdminStatusTextBlock.Text) || AdminStatusTextBlock.Text.Contains("PDF generation failed"))
                    {
                        AdminStatusTextBlock.Text = "2760 form submitted and PDF generated successfully!";
                        AdminStatusTextBlock.Foreground = Brushes.Green;
                    }
                    
                    // Refresh dashboard data
                    await LoadData();
                    ApplyFilters();
                }
                else
                {
                    AdminStatusTextBlock.Text = "Failed to save 2760 form.";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                AdminStatusTextBlock.Text = $"Error: {ex.Message}";
                AdminStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        // AA&E Question checkbox handlers - ensure only one is selected per question
        private void AAEQuestion1_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion1YCheckBox)
            {
                AAEQuestion1NCheckBox.IsChecked = false;
                AAEQuestion1NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion1NCheckBox)
            {
                AAEQuestion1YCheckBox.IsChecked = false;
                AAEQuestion1NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion1NACheckBox)
            {
                AAEQuestion1YCheckBox.IsChecked = false;
                AAEQuestion1NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion2_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion2YCheckBox)
            {
                AAEQuestion2NCheckBox.IsChecked = false;
                AAEQuestion2NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion2NCheckBox)
            {
                AAEQuestion2YCheckBox.IsChecked = false;
                AAEQuestion2NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion2NACheckBox)
            {
                AAEQuestion2YCheckBox.IsChecked = false;
                AAEQuestion2NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion3_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion3YCheckBox)
            {
                AAEQuestion3NCheckBox.IsChecked = false;
                AAEQuestion3NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion3NCheckBox)
            {
                AAEQuestion3YCheckBox.IsChecked = false;
                AAEQuestion3NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion3NACheckBox)
            {
                AAEQuestion3YCheckBox.IsChecked = false;
                AAEQuestion3NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion4_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion4YCheckBox)
            {
                AAEQuestion4NCheckBox.IsChecked = false;
                AAEQuestion4NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion4NCheckBox)
            {
                AAEQuestion4YCheckBox.IsChecked = false;
                AAEQuestion4NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion4NACheckBox)
            {
                AAEQuestion4YCheckBox.IsChecked = false;
                AAEQuestion4NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion5_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion5YCheckBox)
            {
                AAEQuestion5NCheckBox.IsChecked = false;
                AAEQuestion5NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion5NCheckBox)
            {
                AAEQuestion5YCheckBox.IsChecked = false;
                AAEQuestion5NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion5NACheckBox)
            {
                AAEQuestion5YCheckBox.IsChecked = false;
                AAEQuestion5NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion6_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion6YCheckBox)
            {
                AAEQuestion6NCheckBox.IsChecked = false;
                AAEQuestion6NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion6NCheckBox)
            {
                AAEQuestion6YCheckBox.IsChecked = false;
                AAEQuestion6NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion6NACheckBox)
            {
                AAEQuestion6YCheckBox.IsChecked = false;
                AAEQuestion6NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEQuestion7_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQuestion7YCheckBox)
            {
                AAEQuestion7NCheckBox.IsChecked = false;
                AAEQuestion7NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion7NCheckBox)
            {
                AAEQuestion7YCheckBox.IsChecked = false;
                AAEQuestion7NACheckBox.IsChecked = false;
            }
            else if (checkbox == AAEQuestion7NACheckBox)
            {
                AAEQuestion7YCheckBox.IsChecked = false;
                AAEQuestion7NCheckBox.IsChecked = false;
            }
            
            ValidateAAEOutcomePrerequisites();
        }

        private void AAEOutcome_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null || checkbox.IsChecked != true) return;
            
            if (checkbox == AAEQualifiedCheckBox)
            {
                AAEUnqualifiedCheckBox.IsChecked = false;
                AAEReviewLaterCheckBox.IsChecked = false;
            }
            else if (checkbox == AAEUnqualifiedCheckBox)
            {
                AAEQualifiedCheckBox.IsChecked = false;
                AAEReviewLaterCheckBox.IsChecked = false;
            }
            else if (checkbox == AAEReviewLaterCheckBox)
            {
                AAEQualifiedCheckBox.IsChecked = false;
                AAEUnqualifiedCheckBox.IsChecked = false;
            }
        }

        private async void SubmitAAEScreeningButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(AdminLastNameTextBox.Text) || 
                    string.IsNullOrWhiteSpace(AdminFirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AdminDODIdTextBox.Text))
                {
                    AdminStatusTextBlock.Text = "Please fill in all required fields (Last Name, First Name, DOD ID)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate that all questions are answered
                if (!IsAAEQuestionAnswered(1) || !IsAAEQuestionAnswered(2) || !IsAAEQuestionAnswered(3) ||
                    !IsAAEQuestionAnswered(4) || !IsAAEQuestionAnswered(5) || !IsAAEQuestionAnswered(6) ||
                    !IsAAEQuestionAnswered(7))
                {
                    AdminStatusTextBlock.Text = "Please answer all 7 screening questions";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate that an outcome is selected
                if (AAEQualifiedCheckBox.IsChecked != true && AAEUnqualifiedCheckBox.IsChecked != true && 
                    AAEReviewLaterCheckBox.IsChecked != true)
                {
                    AdminStatusTextBlock.Text = "Please select an outcome (Qualified, Unqualified, or Review Later)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate signature dates
                if (!AAEDateScreenedPicker.SelectedDate.HasValue || !AAEDateScreenerPicker.SelectedDate.HasValue)
                {
                    AdminStatusTextBlock.Text = "Please select dates for both Screened and Screener signatures";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                
                var personnelRepo = new PersonnelRepository();
                var additionalRequirementsRepo = new AdditionalRequirementsRepository(dbContext);
                var aaeRepo = new AAEScreeningFormRepository();

                // Check if personnel exists
                var existingPersonnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, AdminDODIdTextBox.Text.Trim());
                Personnel? personnelForPdf;
                int personnelId;

                if (existingPersonnel == null)
                {
                    // Create new personnel
                    var newPersonnel = new Personnel
                    {
                        LastName = AdminLastNameTextBox.Text.Trim(),
                        FirstName = AdminFirstNameTextBox.Text.Trim(),
                        DODId = AdminDODIdTextBox.Text.Trim(),
                        Rate = AdminRateComboBox.SelectedItem?.ToString() ?? "ET",
                        Rank = AdminRankComboBox.SelectedItem?.ToString() ?? "E-1"
                    };

                    personnelId = await personnelRepo.AddPersonnelAsync(dbContext, newPersonnel);
                    newPersonnel.Id = personnelId;
                    personnelForPdf = newPersonnel;
                }
                else
                {
                    personnelId = existingPersonnel.Id;
                    personnelForPdf = existingPersonnel;
                }

                // Determine completion date (use screener date or today)
                var completionDate = AAEDateScreenerPicker.SelectedDate.Value;

                // Get question responses
                var q1Response = GetAAEQuestionResponse(1);
                var q2Response = GetAAEQuestionResponse(2);
                var q3Response = GetAAEQuestionResponse(3);
                var q4Response = GetAAEQuestionResponse(4);
                var q5Response = GetAAEQuestionResponse(5);
                var q6Response = GetAAEQuestionResponse(6);
                var q7Response = GetAAEQuestionResponse(7);

                // Determine qualification status based on business rules
                // All questions must be Y (or N/A for #3), any N = unqualified
                bool isQualified = (q1Response == "Y") && 
                                   (q2Response == "Y") && 
                                   (q3Response == "Y" || q3Response == "na") &&
                                   (q4Response == "Y") && 
                                   (q5Response == "Y") && 
                                   (q6Response == "Y") && 
                                   (q7Response == "Y");

                // Create AA&E Screening Form
                // Person being screened comes from the search fields at the top
                var nameScreened = $"{AdminLastNameTextBox.Text.Trim()}, {AdminFirstNameTextBox.Text.Trim()}";
                
                // Get rank and rate from ComboBox - need to extract Content from ComboBoxItem
                var rankValue = "E-1";
                if (AdminRankComboBox.SelectedItem is ComboBoxItem rankItem)
                {
                    rankValue = rankItem.Content?.ToString() ?? "E-1";
                }
                else if (AdminRankComboBox.SelectedItem != null)
                {
                    rankValue = AdminRankComboBox.SelectedItem.ToString() ?? "E-1";
                }
                
                var rateValue = "ET";
                if (AdminRateComboBox.SelectedItem is ComboBoxItem rateItem)
                {
                    rateValue = rateItem.Content?.ToString() ?? "ET";
                }
                else if (AdminRateComboBox.SelectedItem != null)
                {
                    rateValue = AdminRateComboBox.SelectedItem.ToString() ?? "ET";
                }
                
                var rankScreened = $"{rankValue} {rateValue}";
                
                var aaeForm = new AAEScreeningForm
                {
                    PersonnelId = personnelId,
                    DateCompleted = completionDate,
                    DateExpires = completionDate.AddYears(1),
                    DateCreated = DateTime.Now,
                    NameScreened = nameScreened,
                    RankScreened = rankScreened,
                    DODIDScreened = AdminDODIdTextBox.Text.Trim(),
                    SignatureScreened = AAESignatureScreenedTextBox.Text.Trim(),
                    DateScreened = AAEDateScreenedPicker.SelectedDate,
                    NameScreener = AAENameScreenerTextBox.Text.Trim(),
                    RankScreener = AAERankScreenerTextBox.Text.Trim(),
                    DODIDScreener = AAEDODIDScreenerTextBox.Text.Trim(),
                    SignatureScreener = AAESignatureScreenerTextBox.Text.Trim(),
                    DateScreener = AAEDateScreenerPicker.SelectedDate,
                    Question1Response = q1Response,
                    Question2Response = q2Response,
                    Question3Response = q3Response,
                    Question4Response = q4Response,
                    Question5Response = q5Response,
                    Question6Response = q6Response,
                    Question7Response = q7Response,
                    Remarks1 = AAERemarks1TextBox.Text.Trim(),
                    Remarks2 = AAERemarks2TextBox.Text.Trim(),
                    Remarks3 = AAERemarks3TextBox.Text.Trim(),
                    Remarks4 = AAERemarks4TextBox.Text.Trim(),
                    Remarks5 = AAERemarks5TextBox.Text.Trim(),
                    Remarks6 = AAERemarks6TextBox.Text.Trim(),
                    Remarks7 = AAERemarks7TextBox.Text.Trim(),
                    Qualified = AAEQualifiedCheckBox.IsChecked == true,
                    Unqualified = AAEUnqualifiedCheckBox.IsChecked == true,
                    ReviewLater = AAEReviewLaterCheckBox.IsChecked == true,
                    OtherQualifiedField = AAEOtherQualifiedTextBox.Text.Trim()
                };

                var aaeFormId = await aaeRepo.AddAsync(dbContext, aaeForm);
                aaeForm.Id = aaeFormId;
                var success = aaeFormId > 0;

                // Generate PDF
                bool pdfGenerated = false;
                string? pdfPath = null;
                try
                {
                    var pdfService = new AAEPdfGenerationService();
                    pdfPath = pdfService.GenerateAAEPdf(aaeForm, personnelForPdf, null);
                    
                    // Verify PDF was created
                    if (!File.Exists(pdfPath))
                    {
                        throw new FileNotFoundException($"PDF file was not created at: {pdfPath}");
                    }
                    
                    var signedPath = await TrySignPdfWithCacAsync(pdfPath, "AA&E Screening Form", AAENameScreenerTextBox.Text);
                    if (!string.IsNullOrWhiteSpace(signedPath))
                    {
                        pdfPath = signedPath;
                    }

                    // Update the form with PDF path
                    aaeForm.PdfFilePath = pdfPath;
                    aaeForm.PdfFileName = Path.GetFileName(pdfPath);
                    await aaeRepo.UpdateAsync(dbContext, aaeForm);
                    pdfGenerated = true;
                    
                    // Display the PDF
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    }
                catch (Exception)
                    {
                        // If opening fails, show the path so user can open manually
                        AdminStatusTextBlock.Text = $"PDF generated successfully but could not open automatically. File location: {pdfPath}";
                        AdminStatusTextBlock.Foreground = Brushes.Orange;
                        return; // Exit early so we don't overwrite this message
                    }
                }
                catch (Exception pdfEx)
                {
                    AdminStatusTextBlock.Text = $"Form saved but PDF generation failed: {pdfEx.Message}";
                    AdminStatusTextBlock.Foreground = Brushes.Orange;
                    return; // Exit early so we don't overwrite this message
                }

                if (success && pdfGenerated)
                {
                    AdminStatusTextBlock.Text = "AA&E Screening form submitted and PDF generated successfully!";
                    AdminStatusTextBlock.Foreground = Brushes.Green;
                    
                    // Refresh dashboard data
                    await LoadData();
                    ApplyFilters();
                }
                else if (success)
                {
                    AdminStatusTextBlock.Text = "AA&E Screening form submitted, but PDF generation encountered an issue.";
                    AdminStatusTextBlock.Foreground = Brushes.Orange;
                    
                    // Refresh dashboard data
                    await LoadData();
                    ApplyFilters();
                }
                else
                {
                    AdminStatusTextBlock.Text = "Failed to save AA&E Screening form.";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                AdminStatusTextBlock.Text = $"Error: {ex.Message}";
                AdminStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private bool IsAAEQuestionAnswered(int questionNumber)
        {
            return questionNumber switch
            {
                1 => AAEQuestion1YCheckBox.IsChecked == true || AAEQuestion1NCheckBox.IsChecked == true || AAEQuestion1NACheckBox.IsChecked == true,
                2 => AAEQuestion2YCheckBox.IsChecked == true || AAEQuestion2NCheckBox.IsChecked == true || AAEQuestion2NACheckBox.IsChecked == true,
                3 => AAEQuestion3YCheckBox.IsChecked == true || AAEQuestion3NCheckBox.IsChecked == true || AAEQuestion3NACheckBox.IsChecked == true,
                4 => AAEQuestion4YCheckBox.IsChecked == true || AAEQuestion4NCheckBox.IsChecked == true || AAEQuestion4NACheckBox.IsChecked == true,
                5 => AAEQuestion5YCheckBox.IsChecked == true || AAEQuestion5NCheckBox.IsChecked == true || AAEQuestion5NACheckBox.IsChecked == true,
                6 => AAEQuestion6YCheckBox.IsChecked == true || AAEQuestion6NCheckBox.IsChecked == true || AAEQuestion6NACheckBox.IsChecked == true,
                7 => AAEQuestion7YCheckBox.IsChecked == true || AAEQuestion7NCheckBox.IsChecked == true || AAEQuestion7NACheckBox.IsChecked == true,
                _ => false
            };
        }

        private string? GetAAEQuestionResponse(int questionNumber)
        {
            return questionNumber switch
            {
                1 => AAEQuestion1YCheckBox.IsChecked == true ? "Y" : (AAEQuestion1NCheckBox.IsChecked == true ? "N" : (AAEQuestion1NACheckBox.IsChecked == true ? "na" : null)),
                2 => AAEQuestion2YCheckBox.IsChecked == true ? "Y" : (AAEQuestion2NCheckBox.IsChecked == true ? "N" : (AAEQuestion2NACheckBox.IsChecked == true ? "na" : null)),
                3 => AAEQuestion3YCheckBox.IsChecked == true ? "Y" : (AAEQuestion3NCheckBox.IsChecked == true ? "N" : (AAEQuestion3NACheckBox.IsChecked == true ? "na" : null)),
                4 => AAEQuestion4YCheckBox.IsChecked == true ? "Y" : (AAEQuestion4NCheckBox.IsChecked == true ? "N" : (AAEQuestion4NACheckBox.IsChecked == true ? "na" : null)),
                5 => AAEQuestion5YCheckBox.IsChecked == true ? "Y" : (AAEQuestion5NCheckBox.IsChecked == true ? "N" : (AAEQuestion5NACheckBox.IsChecked == true ? "na" : null)),
                6 => AAEQuestion6YCheckBox.IsChecked == true ? "Y" : (AAEQuestion6NCheckBox.IsChecked == true ? "N" : (AAEQuestion6NACheckBox.IsChecked == true ? "na" : null)),
                7 => AAEQuestion7YCheckBox.IsChecked == true ? "Y" : (AAEQuestion7NCheckBox.IsChecked == true ? "N" : (AAEQuestion7NACheckBox.IsChecked == true ? "na" : null)),
                _ => null
            };
        }

        private void ValidateAAEOutcomePrerequisites()
        {
            // Check if all 7 questions are answered
            bool allQuestionsAnswered = IsAAEQuestionAnswered(1) && IsAAEQuestionAnswered(2) && 
                                       IsAAEQuestionAnswered(3) && IsAAEQuestionAnswered(4) && 
                                       IsAAEQuestionAnswered(5) && IsAAEQuestionAnswered(6) && 
                                       IsAAEQuestionAnswered(7);

            if (!allQuestionsAnswered)
            {
                // Disable all outcome checkboxes if not all questions are answered
                AAEQualifiedCheckBox.IsEnabled = false;
                AAEUnqualifiedCheckBox.IsEnabled = false;
                AAEReviewLaterCheckBox.IsEnabled = false;
                return;
            }

            // Get responses for all questions
            string? q1 = GetAAEQuestionResponse(1);
            string? q2 = GetAAEQuestionResponse(2);
            string? q3 = GetAAEQuestionResponse(3);
            string? q4 = GetAAEQuestionResponse(4);
            string? q5 = GetAAEQuestionResponse(5);
            string? q6 = GetAAEQuestionResponse(6);
            string? q7 = GetAAEQuestionResponse(7);

            // Check for Qualified: All questions must be 'Y' (Q3 can be 'Y' or 'N/A), AND Q5 must be 'N'
            bool canBeQualified = q1 == "Y" &&           // Q1 must be Y
                                  q2 == "Y" &&           // Q2 must be Y
                                  (q3 == "Y" || q3 == "na") && // Q3 can be Y or N/A
                                  q4 == "Y" &&           // Q4 must be Y
                                  q5 == "N" &&           // Q5 MUST be 'N' for qualified
                                  q6 == "Y" &&           // Q6 must be Y
                                  q7 == "Y";             // Q7 must be Y

            // Check for Unqualified: Any question is 'N' (or Q5 is 'Y')
            bool canBeUnqualified = q1 == "N" || q2 == "N" || q3 == "N" || q4 == "N" || 
                                    q5 == "Y" || q6 == "N" || q7 == "N";

            // Enable/disable outcome checkboxes based on validation
            AAEQualifiedCheckBox.IsEnabled = canBeQualified;
            AAEUnqualifiedCheckBox.IsEnabled = canBeUnqualified;
            AAEReviewLaterCheckBox.IsEnabled = true; // Can always select Review Later if all questions are answered

            // If a previously selected outcome is no longer valid, uncheck it
            if (AAEQualifiedCheckBox.IsChecked == true && !canBeQualified)
            {
                AAEQualifiedCheckBox.IsChecked = false;
            }
            if (AAEUnqualifiedCheckBox.IsChecked == true && !canBeUnqualified)
            {
                AAEUnqualifiedCheckBox.IsChecked = false;
            }
        }

        private async void SubmitDeadlyForceTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(AdminLastNameTextBox.Text) || 
                    string.IsNullOrWhiteSpace(AdminFirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AdminDODIdTextBox.Text))
                {
                    AdminStatusTextBlock.Text = "Please fill in all required fields (Last Name, First Name, DOD ID)";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Validate Deadly Force Training date
                if (!DeadlyForceDatePicker.SelectedDate.HasValue)
                {
                    AdminStatusTextBlock.Text = "Please select a Deadly Force Training date";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                if (string.IsNullOrWhiteSpace(DeadlyForceTraineeSignatureTextBox.Text) ||
                    !DeadlyForceTraineeDatePicker.SelectedDate.HasValue ||
                    string.IsNullOrWhiteSpace(DeadlyForceObserverSignatureTextBox.Text) ||
                    !DeadlyForceObserverDatePicker.SelectedDate.HasValue)
                {
                    AdminStatusTextBlock.Text = "Please complete trainee and observer signature/date fields.";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                
                var personnelRepo = new PersonnelRepository();
                var additionalRequirementsRepo = new AdditionalRequirementsRepository(dbContext);

                // Check if personnel exists
                var existingPersonnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, AdminDODIdTextBox.Text.Trim());
                int personnelId;

                if (existingPersonnel == null)
                {
                    // Create new personnel
                    var newPersonnel = new Personnel
                    {
                        LastName = AdminLastNameTextBox.Text.Trim(),
                        FirstName = AdminFirstNameTextBox.Text.Trim(),
                        DODId = AdminDODIdTextBox.Text.Trim(),
                        Rate = AdminRateComboBox.SelectedItem?.ToString() ?? "ET",
                        Rank = AdminRankComboBox.SelectedItem?.ToString() ?? "E-1"
                    };

                    personnelId = await personnelRepo.AddPersonnelAsync(dbContext, newPersonnel);
                }
                else
                {
                    personnelId = existingPersonnel.Id;
                }

                // Get or create admin requirements
                var adminRequirements = new AdditionalRequirements
                {
                    PersonnelId = personnelId,
                    DeadlyForceTrainingDate = DeadlyForceDatePicker.SelectedDate.Value
                };

                // Save admin requirements
                var success = await additionalRequirementsRepo.SaveAsync(personnelId, adminRequirements);

                if (!success)
                {
                    AdminStatusTextBlock.Text = "Failed to save Deadly Force Training.";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                    return;
                }

                // Generate PDF
                try
                {
                    var pdfService = new DeadlyForcePdfGenerationService();
                    var pdfPath = pdfService.GenerateDeadlyForcePdf(
                        DeadlyForceTraineeSignatureTextBox.Text.Trim(),
                        DeadlyForceTraineeDatePicker.SelectedDate.Value,
                        DeadlyForceObserverSignatureTextBox.Text.Trim(),
                        DeadlyForceObserverDatePicker.SelectedDate.Value);

                    var signedPath = await TrySignPdfWithCacAsync(pdfPath, "Deadly Force Training", DeadlyForceObserverSignatureTextBox.Text.Trim());
                    if (!string.IsNullOrWhiteSpace(signedPath))
                    {
                        pdfPath = signedPath;
                    }

                    var document = await _documentService.SaveDocumentAsync(pdfPath, DocumentTypes.DeadlyForceTraining, personnelId);
                    await _documentRepository.AddDocumentAsync(document);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception pdfEx)
                {
                    AdminStatusTextBlock.Text = $"Training saved but PDF generation failed: {pdfEx.Message}";
                    AdminStatusTextBlock.Foreground = Brushes.Orange;
                    return;
                }

                AdminStatusTextBlock.Text = "Deadly Force Training submitted and PDF generated successfully!";
                AdminStatusTextBlock.Foreground = Brushes.Green;
                
                // Refresh dashboard data
                await LoadData();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                AdminStatusTextBlock.Text = $"Error: {ex.Message}";
                AdminStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        // Legacy method - kept for backward compatibility but not used
        private async void AddAdminRequirementsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(AdminLastNameTextBox.Text) || 
                    string.IsNullOrWhiteSpace(AdminFirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AdminDODIdTextBox.Text))
                {
                    AdminStatusTextBlock.Text = "Please fill in all required fields (Last Name, First Name, DOD ID)";
                    return;
                }

                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                
                var personnelRepo = new PersonnelRepository();
                var additionalRequirementsRepo = new AdditionalRequirementsRepository(dbContext);

                // Check if personnel exists
                var existingPersonnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, AdminDODIdTextBox.Text.Trim());
                int personnelId;

                if (existingPersonnel == null)
                {
                    // Create new personnel
                    var newPersonnel = new Personnel
                    {
                        LastName = AdminLastNameTextBox.Text.Trim(),
                        FirstName = AdminFirstNameTextBox.Text.Trim(),
                        DODId = AdminDODIdTextBox.Text.Trim(),
                        Rate = AdminRateComboBox.SelectedItem?.ToString() ?? "ET",
                        Rank = AdminRankComboBox.SelectedItem?.ToString() ?? "E-1"
                    };

                    personnelId = await personnelRepo.AddPersonnelAsync(dbContext, newPersonnel);
                }
                else
                {
                    personnelId = existingPersonnel.Id;
                }

                // Create admin requirements
                var adminRequirements = new AdditionalRequirements
                {
                    PersonnelId = personnelId
                };

                // Form 2760 - use DD2760 Date Signed if provided
                if (DD2760DateSignedPicker.SelectedDate.HasValue)
                {
                    adminRequirements.Form2760Date = DD2760DateSignedPicker.SelectedDate.Value;
                }

                // AA&E Screening
                if (AAEDateScreenerPicker.SelectedDate.HasValue)
                {
                    adminRequirements.AAEScreeningDate = AAEDateScreenerPicker.SelectedDate.Value;
                }

                // Deadly Force Training
                if (DeadlyForceDatePicker.SelectedDate.HasValue)
                {
                    adminRequirements.DeadlyForceTrainingDate = DeadlyForceDatePicker.SelectedDate.Value;
                }

                // Save admin requirements
                var success = await additionalRequirementsRepo.SaveAsync(personnelId, adminRequirements);

                // Save DD2760 form if Date Signed is provided
                if (DD2760DateSignedPicker.SelectedDate.HasValue)
                {
                    var dd2760Repo = new DD2760FormRepository();
                    
                    // Determine domestic violence response
                    string? domesticViolenceResponse = null;
                    string? domesticViolenceInitials = null;
                    
                    if (DD2760YesCheckBox.IsChecked == true)
                    {
                        domesticViolenceResponse = "yes";
                        domesticViolenceInitials = DD2760YesInitialsTextBox.Text.Trim();
                    }
                    else if (DD2760NoCheckBox.IsChecked == true)
                    {
                        domesticViolenceResponse = "no";
                        domesticViolenceInitials = DD2760NoInitialsTextBox.Text.Trim();
                    }
                    else if (DD2760UnknownCheckBox.IsChecked == true)
                    {
                        domesticViolenceResponse = "dontknow";
                        domesticViolenceInitials = DD2760UnknownInitialsTextBox.Text.Trim();
                    }
                    
                    var dd2760Form = new DD2760Form
                    {
                        PersonnelId = personnelId,
                        DateCompleted = DD2760DateSignedPicker.SelectedDate.Value,
                        DateExpires = DD2760DateSignedPicker.SelectedDate.Value.AddYears(1),
                        DateCreated = DateTime.Now,
                        DomesticViolenceResponse = domesticViolenceResponse,
                        DomesticViolenceInitials = domesticViolenceInitials,
                        DomesticViolenceDate = DD2760DomesticViolenceDatePicker.SelectedDate,
                        CourtJurisdiction = string.IsNullOrWhiteSpace(DD2760CourtJurisdictionTextBox.Text) ? null : DD2760CourtJurisdictionTextBox.Text.Trim(),
                        DocketCaseNumber = string.IsNullOrWhiteSpace(DD2760DocketCaseNumberTextBox.Text) ? null : DD2760DocketCaseNumberTextBox.Text.Trim(),
                        StatuteCharge = string.IsNullOrWhiteSpace(DD2760StatuteChargeTextBox.Text) ? null : DD2760StatuteChargeTextBox.Text.Trim(),
                        DateSentenced = DD2760DateSentencedPicker.SelectedDate,
                        CertifierSSN = string.IsNullOrWhiteSpace(DD2760SSNTextBox.Text) ? null : DD2760SSNTextBox.Text.Trim(),
                        IsCertified = DD2760CertificationCheckBox.IsChecked == true,
                        CertifierSignatureDate = DD2760DateSignedPicker.SelectedDate
                    };
                    
                    await dd2760Repo.AddAsync(dbContext, dd2760Form);
                }

                if (success)
                {
                    AdminStatusTextBlock.Text = "Admin requirements saved successfully!";
                    AdminStatusTextBlock.Foreground = Brushes.Green;
                    
                    // Clear form
                    ClearAdminForm_Click(sender, e);
                    
                    // Refresh dashboard data
                    await LoadData();
                    ApplyFilters();
                }
                else
                {
                    AdminStatusTextBlock.Text = "Failed to save admin requirements.";
                    AdminStatusTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                AdminStatusTextBlock.Text = $"Error: {ex.Message}";
                AdminStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private void DD2760Response_Changed(object sender, RoutedEventArgs e)
        {
            // Ensure only one checkbox is selected at a time
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            // If this checkbox is being checked, uncheck the others
            if (checkbox.IsChecked == true)
            {
                if (checkbox == DD2760YesCheckBox)
                {
                    DD2760NoCheckBox.IsChecked = false;
                    DD2760UnknownCheckBox.IsChecked = false;
                }
                else if (checkbox == DD2760NoCheckBox)
                {
                    DD2760YesCheckBox.IsChecked = false;
                    DD2760UnknownCheckBox.IsChecked = false;
                }
                else if (checkbox == DD2760UnknownCheckBox)
                {
                    DD2760YesCheckBox.IsChecked = false;
                    DD2760NoCheckBox.IsChecked = false;
                }
            }
        }

        private void ClearAdminForm_Click(object sender, RoutedEventArgs e)
        {
            // Clear search fields
            SearchLastNameTextBox.Clear();
            SearchResultTextBlock.Text = "";
            AdminSearchResultsComboBox.ItemsSource = null;
            AdminSearchResultsComboBox.SelectedIndex = -1;
            AdminSearchResultsComboBox.Visibility = Visibility.Collapsed;
            
            // Clear sailor information
            AdminLastNameTextBox.Clear();
            AdminFirstNameTextBox.Clear();
            AdminDODIdTextBox.Clear();
            AdminRateComboBox.SelectedIndex = -1;
            AdminRankComboBox.SelectedIndex = -1;
            
            // Clear admin requirements
            // Note: Form2760DatePicker was replaced with DD2760DateSignedPicker in the expanded form
            DeadlyForceDatePicker.SelectedDate = null;
            
            // Clear AA&E Screening form fields
            // Note: Person being screened fields are auto-populated from search, so we don't clear them
            AAENameScreenerTextBox.Clear();
            AAERankScreenerTextBox.Clear();
            AAEDODIDScreenerTextBox.Clear();
            AAEQuestion1YCheckBox.IsChecked = false;
            AAEQuestion1NCheckBox.IsChecked = false;
            AAEQuestion1NACheckBox.IsChecked = false;
            AAEQuestion2YCheckBox.IsChecked = false;
            AAEQuestion2NCheckBox.IsChecked = false;
            AAEQuestion2NACheckBox.IsChecked = false;
            AAEQuestion3YCheckBox.IsChecked = false;
            AAEQuestion3NCheckBox.IsChecked = false;
            AAEQuestion3NACheckBox.IsChecked = false;
            AAEQuestion4YCheckBox.IsChecked = false;
            AAEQuestion4NCheckBox.IsChecked = false;
            AAEQuestion4NACheckBox.IsChecked = false;
            AAEQuestion5YCheckBox.IsChecked = false;
            AAEQuestion5NCheckBox.IsChecked = false;
            AAEQuestion5NACheckBox.IsChecked = false;
            AAEQuestion6YCheckBox.IsChecked = false;
            AAEQuestion6NCheckBox.IsChecked = false;
            AAEQuestion6NACheckBox.IsChecked = false;
            AAEQuestion7YCheckBox.IsChecked = false;
            AAEQuestion7NCheckBox.IsChecked = false;
            AAEQuestion7NACheckBox.IsChecked = false;
            AAERemarks1TextBox.Clear();
            AAERemarks2TextBox.Clear();
            AAERemarks3TextBox.Clear();
            AAERemarks4TextBox.Clear();
            AAERemarks5TextBox.Clear();
            AAERemarks6TextBox.Clear();
            AAERemarks7TextBox.Clear();
            AAESignatureScreenedTextBox.Clear();
            AAEDateScreenedPicker.SelectedDate = null;
            AAESignatureScreenerTextBox.Clear();
            AAEDateScreenerPicker.SelectedDate = null;
            AAEQualifiedCheckBox.IsChecked = false;
            AAEUnqualifiedCheckBox.IsChecked = false;
            AAEReviewLaterCheckBox.IsChecked = false;
            AAEOtherQualifiedTextBox.Clear();
            
            // Validate outcome prerequisites (will disable checkboxes since questions are cleared)
            ValidateAAEOutcomePrerequisites();
            
            // Clear DD2760 form fields
            DD2760YesCheckBox.IsChecked = false;
            DD2760NoCheckBox.IsChecked = false;
            DD2760UnknownCheckBox.IsChecked = false;
            DD2760YesInitialsTextBox.Clear();
            DD2760NoInitialsTextBox.Clear();
            DD2760UnknownInitialsTextBox.Clear();
            DD2760DomesticViolenceDatePicker.SelectedDate = null;
            DD2760CourtJurisdictionTextBox.Clear();
            DD2760DocketCaseNumberTextBox.Clear();
            DD2760StatuteChargeTextBox.Clear();
            DD2760DateSentencedPicker.SelectedDate = null;
            DD2760CertificationCheckBox.IsChecked = false;
            DD2760SSNTextBox.Clear();
            DD2760DateSignedPicker.SelectedDate = null;
            
            AdminStatusTextBlock.Text = "";
        }

        private void AdminDODIdTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                return;
            }
            
            // Get the current text and the new text that would be added
            var textBox = sender as TextBox;
            var currentText = textBox?.Text ?? "";
            var newText = currentText + e.Text;
            
            // Enforce exactly 10 digits
            if (newText.Length > 10)
            {
                e.Handled = true;
            }
        }

        private void SearchLastNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SearchSailor_Click(sender, e);
            }
        }

        private async void SearchSailor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lastName = SearchLastNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(lastName))
                {
                    SearchResultTextBlock.Text = "Please enter a last name to search.";
                    return;
                }

                using var dbContext = new DatabaseContext();
                dbContext.InitializeDatabase();
                
                var personnelRepo = new PersonnelRepository();
                
                // Search for personnel by last name
                var allPersonnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
                var matchingPersonnel = allPersonnel.Where(p => 
                    p.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase)).ToList();

                AdminSearchResultsComboBox.Visibility = Visibility.Collapsed;
                AdminSearchResultsComboBox.ItemsSource = null;

                if (matchingPersonnel.Count == 0)
                {
                    SearchResultTextBlock.Text = $"No sailors found with last name '{lastName}'";
                    return;
                }

                if (matchingPersonnel.Count == 1)
                {
                    // Found exactly one match - populate the form
                    var sailor = matchingPersonnel.First();
                    PopulateAdminFormFromSailor(sailor);
                    SearchResultTextBlock.Text = $"Found: {sailor.FirstName} {sailor.LastName} ({sailor.Rate})";
                }
                else
                {
                    var matches = matchingPersonnel
                        .Select(p => new SailorDisplayModel(p.Id, p.LastName, p.FirstName, p.DODId, p.Rank, p.Rate))
                        .ToList();
                    AdminSearchResultsComboBox.ItemsSource = matches;
                    AdminSearchResultsComboBox.Visibility = Visibility.Visible;
                    AdminSearchResultsComboBox.SelectedIndex = -1;
                    SearchResultTextBlock.Text = $"Found {matchingPersonnel.Count} matches. Please select one.";
                }
            }
            catch (Exception ex)
            {
                SearchResultTextBlock.Text = $"Error searching: {ex.Message}";
            }
        }

        private void AdminSearchResultsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AdminSearchResultsComboBox.SelectedItem is SailorDisplayModel selectedSailor)
            {
                PopulateAdminFormFromSailorDisplay(selectedSailor);
                SearchResultTextBlock.Text = $"Selected: {selectedSailor.DisplayName}";
            }
        }

        private void PopulateAdminFormFromSailor(Personnel sailor)
        {
            AdminLastNameTextBox.Text = sailor.LastName;
            AdminFirstNameTextBox.Text = sailor.FirstName;
            AdminDODIdTextBox.Text = sailor.DODId;
            
            // Set rate
            for (int i = 0; i < AdminRateComboBox.Items.Count; i++)
            {
                var item = AdminRateComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == sailor.Rate)
                {
                    AdminRateComboBox.SelectedIndex = i;
                    break;
                }
            }
            
            // Set rank
            for (int i = 0; i < AdminRankComboBox.Items.Count; i++)
            {
                var item = AdminRankComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == sailor.Rank)
                {
                    AdminRankComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PopulateAdminFormFromSailorDisplay(SailorDisplayModel sailor)
        {
            AdminLastNameTextBox.Text = sailor.DisplayName.Split(',').FirstOrDefault()?.Trim() ?? "";
            var nameParts = sailor.DisplayName.Split(',');
            if (nameParts.Length > 1)
            {
                var firstPart = nameParts[1];
                var firstName = firstPart.Split('(').FirstOrDefault()?.Trim() ?? "";
                AdminFirstNameTextBox.Text = firstName;
            }

            AdminDODIdTextBox.Text = sailor.DODId;

            for (int i = 0; i < AdminRateComboBox.Items.Count; i++)
            {
                var item = AdminRateComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == sailor.RankRate.Split(' ').LastOrDefault())
                {
                    AdminRateComboBox.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < AdminRankComboBox.Items.Count; i++)
            {
                var item = AdminRankComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == sailor.RankRate.Split(' ').FirstOrDefault())
                {
                    AdminRankComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private async void ExportQualifications_Click(object sender, RoutedEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ManageSystem, "Export Qualifications"))
            {
                return;
            }

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
                    using var dbContext = new DatabaseContext();
                    dbContext.InitializeDatabase();
                    
                    var personnelRepo = new PersonnelRepository();
                    var personnel = await personnelRepo.GetPersonnelWithQualificationsAsync(dbContext);
                    
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
                                $"\"{person.LastName}, {person.FirstName}\",\"{person.Rate}\",\"{dutySections}\",\"{qualification.Weapon}\"," +
                                $"{qualification.Category},\"{qualification.DateQualified:yyyy-MM-dd}\",\"{status}\"," +
                                $"\"{qualification.Status?.ExpiresOn:yyyy-MM-dd}\",{qualification.Status?.DaysUntilExpiration}");
                        }
                    }
                    
                    MessageBox.Show($"Data exported successfully to {saveFileDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Log the export action
                    var totalRecords = personnel.Sum(p => p.Qualifications.Count);
                    var auditService = new AuditService(dbContext);
                    await auditService.LogExportActionAsync(saveFileDialog.FileName, totalRecords);
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
            if (!RequirePermission(RbacPermission.ManageSystem, "Load Test Data"))
            {
                return;
            }

            try
            {
                await TestData.PopulateTestDataAsync();
                TestDataStatusTextBlock.Text = "Test data added successfully!";
                await LoadData(); // Refresh the dashboard
            }
            catch (Exception ex)
            {
                TestDataStatusTextBlock.Text = $"Error adding test data: {ex.Message}";
            }
        }





        // TEST ONLY: Add test sailors and CAT II qualifications. DELETE BEFORE PRODUCTION.
        private async void AddTestDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await TestData.PopulateTestDataAsync();
                TestDataStatusTextBlock.Text = "Test data added successfully!";
                
                // Clear cache and reload data to ensure fresh data is displayed
                _allPersonnelCache.Clear();
                await LoadData(); // Refresh the dashboard
                
                // Refresh the sailor dropdown list
                await LoadSailorComboBoxData();
            }
            catch (Exception ex)
            {
                TestDataStatusTextBlock.Text = $"Error adding test data: {ex.Message}";
            }
        }

        // TEST ONLY: Clear all test data from the database. DELETE BEFORE PRODUCTION.
        private async void ClearDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("This will delete ALL data from the database and recreate the schema. Are you sure?", "Reset Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    TestDataStatusTextBlock.Text = "Resetting database...";
                    
                    bool resetSuccessful = false;
                    string dbPath = "";
                    
                    try
                    {
                        // Create a fresh context for the reset operation
                        using var dbContext = new DatabaseContext();
                        dbPath = dbContext.GetDatabaseFilePath();
                        
                        // Try the regular reset first
                        dbContext.ResetDatabase();
                        TestDataStatusTextBlock.Text = "Database reset successfully! Schema has been updated.";
                        resetSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        // If regular reset fails, try the file deletion method
                        TestDataStatusTextBlock.Text = $"Regular reset failed: {ex.Message}. Trying alternative method...";
                        await Task.Delay(1000); // Give user time to see the message
                        
                        try
                        {
                            using var dbContext = new DatabaseContext();
                            dbContext.ResetDatabaseByFile();
                            TestDataStatusTextBlock.Text = "Database reset successfully using alternative method!";
                            resetSuccessful = true;
                        }
                        catch (Exception ex2)
                        {
                            // If both methods fail, try direct file deletion
                            TestDataStatusTextBlock.Text = $"Both methods failed: {ex2.Message}. Trying direct file deletion...";
                            await Task.Delay(1000);
                            
                            await DirectDatabaseReset(dbPath);
                            TestDataStatusTextBlock.Text = "Database reset using direct file deletion!";
                            resetSuccessful = true;
                        }
                    }
                    
                    if (resetSuccessful)
                    {
                        // Verify the database was actually cleared by checking if there's any data
                        await Task.Delay(500); // Brief delay to ensure database operations complete
                        
                        try
                        {
                            // Use a fresh context for verification
                            using var verifyContext = new DatabaseContext();
                            verifyContext.InitializeDatabase();
                            var verifyPersonnelRepo = new PersonnelRepository();
                            var personnel = await verifyPersonnelRepo.GetAllPersonnelAsync(verifyContext);
                            
                            // Debug: Show the database file path and personnel count
                            var verifyDbPath = verifyContext.GetDatabaseFilePath();
                            var fileExists = System.IO.File.Exists(verifyDbPath);
                            var fileSize = fileExists ? new System.IO.FileInfo(verifyDbPath).Length : 0;
                            
                            TestDataStatusTextBlock.Text = $"Verification: {personnel.Count} personnel found. " +
                                                          $"DB: {Path.GetFileName(verifyDbPath)}, " +
                                                          $"Size: {fileSize} bytes, " +
                                                          $"Exists: {fileExists}";
                            
                            if (personnel.Any())
                            {
                                TestDataStatusTextBlock.Text += "\nWarning: Database may not have been fully cleared. Some data remains.";
                            }
                            else
                            {
                                TestDataStatusTextBlock.Text = "Database successfully cleared and verified!";
                            }
                            
                            // Refresh the sailor dropdown list
                            await LoadSailorComboBoxData();
                        }
                        catch (Exception verifyEx)
                        {
                            TestDataStatusTextBlock.Text = $"Database reset completed, but verification failed: {verifyEx.Message}";
                        }
                        
                        // Clear the cache and reload data
                        _allPersonnelCache.Clear();
                        await LoadData();
                        
                        TestDataStatusTextBlock.Text += "\nDatabase reset complete. Dashboard has been refreshed.";
                    }
                }
            }
            catch (Exception ex)
            {
                TestDataStatusTextBlock.Text = $"Error resetting database: {ex.Message}";
            }
        }

        private async Task DirectDatabaseReset(string dbPath)
        {
            try
            {
                // Debug: Show what we're trying to delete
                TestDataStatusTextBlock.Text = $"Direct reset: Attempting to delete {dbPath}";
                
                // Wait for any locks to be released
                await Task.Delay(500);
                
                // Delete the file directly
                if (System.IO.File.Exists(dbPath))
                {
                    System.IO.File.Delete(dbPath);
                    TestDataStatusTextBlock.Text = $"Direct reset: Deleted {dbPath}";
                    await Task.Delay(200);
                }
                else
                {
                    TestDataStatusTextBlock.Text = $"Direct reset: File not found {dbPath}";
                }
            }
            catch (Exception ex)
            {
                TestDataStatusTextBlock.Text = $"Direct reset error: {ex.Message}";
                throw new InvalidOperationException($"Direct database reset failed: {ex.Message}", ex);
            }
        }

        // TEST ONLY: Add test sailors and CAT II qualifications. DELETE BEFORE PRODUCTION.
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void ApplyColumnVisibility()
        {
            // Clear existing columns
            PersonnelDataGrid.Columns.Clear();

            // Add columns based on settings
            if (_dashboardSettings.ShowName)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "Name", 
                    Binding = new System.Windows.Data.Binding("NameDisplay"), 
                    Width = 150 
                });
            }

            if (_dashboardSettings.ShowRate)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "Rate", 
                    Binding = new System.Windows.Data.Binding("Rate"), 
                    Width = 80 
                });
            }

            // Add duty section columns
            if (_dashboardSettings.ShowDutySection3)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "3 Section", 
                    Binding = new System.Windows.Data.Binding("DutySection3Display"), 
                    Width = 80 
                });
            }

            if (_dashboardSettings.ShowDutySection6)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "6 Section", 
                    Binding = new System.Windows.Data.Binding("DutySection6Display"), 
                    Width = 80 
                });
            }

            // Add weapon columns
            if (_dashboardSettings.ShowM9)
            {
                PersonnelDataGrid.Columns.Add(CreateWeaponColumn("M9", "M9Qualified", "M9Color", "M9AdminBlocked", 40));
            }

            if (_dashboardSettings.ShowM4M16)
            {
                PersonnelDataGrid.Columns.Add(CreateWeaponColumn("M4/M16", "M4M16Qualified", "M4M16Color", "M4M16AdminBlocked", 60));
            }

            if (_dashboardSettings.ShowM500)
            {
                PersonnelDataGrid.Columns.Add(CreateWeaponColumn("M500", "M500Qualified", "M500Color", "M500AdminBlocked", 50));
            }

            if (_dashboardSettings.ShowM240)
            {
                PersonnelDataGrid.Columns.Add(CreateWeaponColumn("M240", "M240Qualified", "M240Color", "M240AdminBlocked", 50));
            }

            if (_dashboardSettings.ShowM2)
            {
                PersonnelDataGrid.Columns.Add(CreateWeaponColumn(".50", "FiftyCalQualified", "FiftyCalColor", "FiftyCalAdminBlocked", 40));
            }

            if (_dashboardSettings.ShowDateQualified)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "Date Qualified", 
                    Binding = new System.Windows.Data.Binding("MostRecentQualificationDateDisplay"), 
                    Width = 120 
                });
            }

            if (_dashboardSettings.ShowLapsedQualifications)
            {
                PersonnelDataGrid.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "Lapsed Qualifications", 
                    Binding = new System.Windows.Data.Binding("LapsedQualificationsDisplay"), 
                    Width = 180 
                });
            }

            // Add admin columns
            if (_dashboardSettings.ShowAdmin)
            {
                PersonnelDataGrid.Columns.Add(CreateAdminColumn("Admin", "AdminStatus", "AdminColor", 50));
            }

            if (_dashboardSettings.ShowForm2760)
            {
                PersonnelDataGrid.Columns.Add(CreateAdminColumn("2760", "Form2760Status", "Form2760Color", 50));
            }

            if (_dashboardSettings.ShowAAEScreening)
            {
                PersonnelDataGrid.Columns.Add(CreateAdminColumn("AA&E", "AAEScreeningStatus", "AAEScreeningColor", 50));
            }

            if (_dashboardSettings.ShowDeadlyForceTraining)
            {
                PersonnelDataGrid.Columns.Add(CreateAdminColumn("DFT", "DeadlyForceTrainingStatus", "DeadlyForceTrainingColor", 50));
            }
        }

        private DataGridTemplateColumn CreateWeaponColumn(string header, string bindingPath, string colorBindingPath, string adminBlockBindingPath, int width)
        {
            var column = new DataGridTemplateColumn { Header = header, Width = width };
            
            // Create the cell template
            var template = new System.Windows.DataTemplate();
            var grid = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Grid));
            var border = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            var textBlock = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            var overlayText = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            
            // Set up the text block
            textBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding(bindingPath));
            textBlock.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            textBlock.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            textBlock.SetValue(System.Windows.Controls.TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold);
            
            // Set up the border with background color
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding(colorBindingPath));
            border.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new System.Windows.Thickness(1));
            border.SetValue(System.Windows.Controls.Border.BorderBrushProperty, System.Windows.Media.Brushes.Gray);
            border.SetValue(System.Windows.Controls.Border.MarginProperty, new System.Windows.Thickness(1));
            
            // Add text block to border
            border.AppendChild(textBlock);

            // Red X overlay when admin is not current
            overlayText.SetValue(System.Windows.Controls.TextBlock.TextProperty, "X");
            overlayText.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Red);
            overlayText.SetValue(System.Windows.Controls.TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold);
            overlayText.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 14.0);
            overlayText.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            overlayText.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            overlayText.SetBinding(System.Windows.Controls.TextBlock.VisibilityProperty,
                new System.Windows.Data.Binding(adminBlockBindingPath)
                {
                    Converter = new System.Windows.Controls.BooleanToVisibilityConverter()
                });

            grid.AppendChild(border);
            grid.AppendChild(overlayText);
            template.VisualTree = grid;
            
            column.CellTemplate = template;
            return column;
        }

        /// <summary>
        /// Gets current performance statistics for monitoring
        /// </summary>
        public (int AvgLoadTime, int AvgFilterTime, int LoadCount, int FilterCount, int CacheSize, int ExpiredEntries) GetPerformanceStats()
        {
            var cacheStats = _qualificationService.GetCacheStats();
            var avgLoadTime = _loadCount > 0 ? _totalLoadTime / _loadCount : 0;
            var avgFilterTime = _filterCount > 0 ? _totalFilterTime / _filterCount : 0;
            
            return (avgLoadTime, avgFilterTime, _loadCount, _filterCount, cacheStats.CacheSize, cacheStats.ExpiredEntries);
        }

        // Performance tab event handlers
        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            _qualificationService.ClearCache();
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] Cache cleared manually";
            lock (_logLock)
            {
                _performanceLog.Add(logEntry);
                if (_performanceLog.Count > 50) _performanceLog.RemoveAt(0);
            }
            UpdatePerformanceDisplay();
            MessageBox.Show("Cache cleared successfully!", "Cache Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            UpdatePerformanceDisplay();
        }

        private void ExportPerformanceReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"QualTrack_Performance_Report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var stats = GetPerformanceStats();
                    var cacheStats = _qualificationService.GetCacheStats();
                    
                    var report = $@"QualTrack Performance Report
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

DATABASE PERFORMANCE:
- Average Load Time: {stats.AvgLoadTime} ms
- Load Count: {stats.LoadCount}
- Average Filter Time: {stats.AvgFilterTime} ms
- Filter Count: {stats.FilterCount}
- Total Personnel: {_allPersonnelCache.Count}
- Total Qualifications: {_allPersonnelCache.Sum(p => p.Qualifications.Count)}

CACHE PERFORMANCE:
- Cache Size: {cacheStats.CacheSize} entries
- Expired Entries: {cacheStats.ExpiredEntries}
- Cache Hit Rate: {(stats.LoadCount + stats.FilterCount > 0 ? (cacheStats.CacheSize * 100.0 / (stats.LoadCount + stats.FilterCount)) : 0):F1}%

SYSTEM STATUS:
- Memory Usage: {GC.GetTotalMemory(false) / (1024 * 1024)} MB
- Database Status: Connected

RECENT PERFORMANCE LOG:
{string.Join(Environment.NewLine, _performanceLog)}

END OF REPORT";

                    File.WriteAllText(saveDialog.FileName, report);
                    MessageBox.Show($"Performance report exported to: {saveDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting performance report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPerformanceCounters_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will reset all performance counters. Continue?", "Reset Counters", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _totalLoadTime = 0;
                _totalFilterTime = 0;
                _loadCount = 0;
                _filterCount = 0;
                
                lock (_logLock)
                {
                    _performanceLog.Clear();
                    _performanceLog.Add($"[{DateTime.Now:HH:mm:ss}] Performance counters reset");
                }
                
                UpdatePerformanceDisplay();
                MessageBox.Show("Performance counters reset successfully!", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Reset all filters
            StatusFilterComboBox.SelectedIndex = 0;
            DutySectionTypeFilterComboBox.SelectedIndex = 0;
            DutySectionNumberFilterComboBox.SelectedIndex = 0;
            WeaponFilterComboBox.SelectedIndex = 0;
            _selectedDutySectionType = "All";
            _selectedDutySectionNumber = "All";
            _selectedWeapon = "All";
            _selectedStatus = "All";
            ApplyFilters();
        }

        private void ShowDatabasePath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dbContext = new DatabaseContext();
                var dbPath = dbContext.GetDatabaseFilePath();
                var fileExists = System.IO.File.Exists(dbPath);
                var fileSize = fileExists ? new System.IO.FileInfo(dbPath).Length : 0;
                
                // Check for other database files in the directory
                var directory = Path.GetDirectoryName(dbPath);
                var allDbFiles = System.IO.Directory.GetFiles(directory ?? ".", "*.db", SearchOption.TopDirectoryOnly);
                
                var message = $"Database File Path: {dbPath}\n" +
                             $"File Exists: {fileExists}\n" +
                             $"File Size: {fileSize} bytes\n\n" +
                             $"All .db files in directory:\n";
                
                foreach (var file in allDbFiles)
                {
                    var info = new System.IO.FileInfo(file);
                    message += $"- {Path.GetFileName(file)} ({info.Length} bytes)\n";
                }
                
                MessageBox.Show(message, "Database Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting database info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataGridTemplateColumn CreateAdminColumn(string header, string bindingPath, string colorBindingPath, int width)
        {
            var column = new DataGridTemplateColumn { Header = header, Width = width };
            
            // Create the cell template
            var template = new System.Windows.DataTemplate();
            var border = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            var textBlock = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            
            // Set up the text block
            textBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding(bindingPath));
            textBlock.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            textBlock.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            textBlock.SetValue(System.Windows.Controls.TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold);
            
            // Set up the border with background color
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding(colorBindingPath));
            border.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new System.Windows.Thickness(1));
            border.SetValue(System.Windows.Controls.Border.BorderBrushProperty, System.Windows.Media.Brushes.Gray);
            border.SetValue(System.Windows.Controls.Border.MarginProperty, new System.Windows.Thickness(1));
            
            // Add text block to border
            border.AppendChild(textBlock);
            template.VisualTree = border;
            
            column.CellTemplate = template;
            return column;
        }



        // 3591/1 Upload: Browse button








        private async Task InitializeDigitalEntryTab()
        {
            // Clear any existing data and start with completely empty grid
            deSailorQualifications.Clear();
            DE_SailorGrid.ItemsSource = deSailorQualifications;
                
            // Load sailor data for ComboBox
            await LoadSailorComboBoxData();
            
            // Set default session-level values
            DE_ShipStationBox.SelectedIndex = 0;
            DE_DivisionActivityBox.SelectedIndex = 0;
            DE_RangeNameLocationBox.SelectedIndex = 0;
            DE_DateOfFiringPicker.SelectedDate = DateTime.Today;
        }
        
        private async Task LoadSailorComboBoxData()
        {
            try
            {
                using var dbContext = new DatabaseContext();
                var personnelRepo = new PersonnelRepository();
                var allPersonnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
                
                var sailorModels = allPersonnel.Select(p => new SailorDisplayModel(
                    p.Id, p.LastName, p.FirstName, p.DODId, p.Rank, p.Rate)).ToList();
                
                SailorSelectionComboBox.ItemsSource = sailorModels;
                
                // Debug: Show how many sailors were loaded
                Console.WriteLine($"Loaded {sailorModels.Count} sailors for dropdown");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sailor data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DE_AddRow_Click(object sender, RoutedEventArgs e)
        {
            deSailorQualifications.Add(new SailorQualification());
        }

        private void SailorSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Show preview of selected sailor
        }
        
        private void AddSelectedSailor_Click(object sender, RoutedEventArgs e)
        {
            if (SailorSelectionComboBox.SelectedItem is SailorDisplayModel selectedSailor)
            {
                var newRow = new SailorQualification
                {
                    FullName = selectedSailor.DisplayName,
                    DodId = selectedSailor.DODId,
                    RankRate = selectedSailor.RankRate
                };
                
                // Add at the bottom of the grid
                deSailorQualifications.Add(newRow);
                
                // Clear selection
                SailorSelectionComboBox.SelectedItem = null;
            }
        }
        
        private async void RefreshSailorList_Click(object sender, RoutedEventArgs e)
        {
            await LoadSailorComboBoxData();
        }

        private void DE_RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (DE_SailorGrid.SelectedItem is SailorQualification selected && deSailorQualifications.Contains(selected))
                deSailorQualifications.Remove(selected);
        }



        private void ClearWeaponCheckboxes()
        {
            DE_M9CheckBox.IsChecked = false;
            DE_M16CheckBox.IsChecked = false;
            DE_M500CheckBox.IsChecked = false;
        }

        private string GetWeaponsFiredFromCheckboxes()
        {
            var weapons = new List<string>();
            
            if (DE_M9CheckBox.IsChecked == true)
                weapons.Add("M9");
            if (DE_M16CheckBox.IsChecked == true)
                weapons.Add("M4/M16");
            if (DE_M500CheckBox.IsChecked == true)
                weapons.Add("M500");
            
            return string.Join(", ", weapons);
        }

        private void SetWeaponCheckboxesFromText(string weaponsText)
        {
            if (string.IsNullOrWhiteSpace(weaponsText))
            {
                ClearWeaponCheckboxes();
                return;
            }
            
            var weapons = weaponsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(w => w.Trim().ToUpper())
                                   .ToList();
            
            DE_M9CheckBox.IsChecked = weapons.Contains("M9");
            DE_M16CheckBox.IsChecked = weapons.Contains("M4/M16") || weapons.Contains("M16") || weapons.Contains("M4");
            DE_M500CheckBox.IsChecked = weapons.Contains("M500");
        }

        private bool Validate3591Scores(string weapon, SailorQualification row, out string error)
        {
            error = "";
            weapon = weapon.ToUpper();
            
            // Validate all weapon scores regardless of which weapons were fired
            // This prevents invalid data entry
            
            // NHQC validation (180-240)
            if (row.NHQC.HasValue && (row.NHQC < 180 || row.NHQC > 240))
                error += $"NHQC score {row.NHQC} is out of range (180–240).\n";
            
            // RQC validation (140-200)
            if (row.RQC.HasValue && (row.RQC < 140 || row.RQC > 200))
                error += $"RQC score {row.RQC} is out of range (140–200).\n";
            
            // SPWC validation (90-162 total)
            int spwcTotal = (row.SPWCT1 ?? 0) + (row.SPWCT2 ?? 0) + (row.SPWCT3 ?? 0);
            if ((row.SPWCT1.HasValue || row.SPWCT2.HasValue || row.SPWCT3.HasValue) && (spwcTotal < 90 || spwcTotal > 162))
                error += $"SPWC total {spwcTotal} is out of range (90–162).\n";
            
            // HPWC validation (12-18 total)
            int hpwcTotal = (row.HPWCT1 ?? 0) + (row.HPWCT2 ?? 0) + (row.HPWCT3 ?? 0);
            if ((row.HPWCT1.HasValue || row.HPWCT2.HasValue || row.HPWCT3.HasValue) && (hpwcTotal < 12 || hpwcTotal > 18))
                error += $"HPWC total {hpwcTotal} is out of range (12–18).\n";
            
            // HLLC validation (12-18)
            if (row.HLLC.HasValue && (row.HLLC < 12 || row.HLLC > 18))
                error += $"HLLC score {row.HLLC} is out of range (12–18).\n";
            
            // RLLC validation (14-20)
            if (row.RLLC.HasValue && (row.RLLC < 14 || row.RLLC > 20))
                error += $"RLLC score {row.RLLC} is out of range (14–20).\n";
            
            return string.IsNullOrEmpty(error);
        }

        private async void DE_SaveForm_Click(object sender, RoutedEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ManageQualifications, "Save 3591/1 Qualification"))
            {
                return;
            }

            DatabaseContext dbContext = null;
            try
            {
                Console.WriteLine("Starting 3591/1 form save process...");
                
                // Setup repositories and context
                dbContext = new DatabaseContext();
                var sessionRepo = new QualificationSessionRepository();
                var personnelRepo = new PersonnelRepository();
                var qualRepo = new QualificationRepository();

                // 1. Save session-level data
                var session = new QualificationSession
                {
                    ShipStation = DE_ShipStationBox.SelectedValue?.ToString() ?? string.Empty,
                    DivisionActivity = DE_DivisionActivityBox.SelectedValue?.ToString() ?? string.Empty,
                    WeaponsFired = GetWeaponsFiredFromCheckboxes(),
                    RangeNameLocation = DE_RangeNameLocationBox.SelectedValue?.ToString() ?? string.Empty,
                    DateOfFiring = DE_DateOfFiringPicker.SelectedDate,
                    CreatedDate = DateTime.Now
                };
                int sessionId = await sessionRepo.AddSessionAsync(dbContext, session);
                Console.WriteLine($"Session saved with ID: {sessionId}");

                int savedCount = 0;
                int pdfGeneratedCount = 0;

                // 2. Generate filled PDF for the session
                string pdfPath = "";
                try
                {
                    pdfPath = await Generate3591PdfForSession(session, deSailorQualifications.ToList());
                    pdfGeneratedCount++;
                    
                    if (!string.IsNullOrWhiteSpace(pdfPath))
                    {
                        var signedPath = await TrySignPdfWithCacAsync(pdfPath, "3591/1 Qualification", session.RsoSignature ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(signedPath))
                        {
                            pdfPath = signedPath;
                        }

                        session.PdfFilePath = pdfPath;
                        await sessionRepo.UpdateSessionPdfFilePathAsync(dbContext, sessionId, pdfPath);
                    }
                }
                catch (Exception pdfEx)
                {
                    MessageBox.Show($"Warning: PDF generation failed: {pdfEx.Message}", "PDF Generation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Collect all valid sailors first
                var validSailors = new List<SailorQualification>();
                foreach (var row in deSailorQualifications)
                {
                    if (string.IsNullOrWhiteSpace(row.DodId)) continue;

                    // Validate DODID format
                    if (row.DodId.Length != 10 || !row.DodId.All(char.IsDigit))
                    {
                        MessageBox.Show($"Invalid DODID format for sailor '{row.FullName}': {row.DodId}. DODID must be exactly 10 digits.", 
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    // Validate score ranges
                    if (!Validate3591Scores(session.WeaponsFired, row, out string scoreError))
                    {
                        MessageBox.Show($"Score validation error for sailor '{row.FullName}':\n" + scoreError, "Score Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    validSailors.Add(row);
                }

                if (validSailors.Count == 0)
                {
                    MessageBox.Show("No valid sailors found to save.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Sequential sailor review process
                for (int i = 0; i < validSailors.Count; i++)
                {
                    var row = validSailors[i];
                    bool isLastSailor = (i == validSailors.Count - 1);
                    
                    // Check if personnel exists by DODID
                    var existingPersonnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, row.DodId);
                    
                    if (existingPersonnel != null)
                    {
                        // Load existing personnel with qualifications for comparison
                        var personnelWithQuals = existingPersonnel;
                        personnelWithQuals.Qualifications = await qualRepo.GetQualificationsForPersonnelAsync(dbContext, existingPersonnel.Id);
                        
                        // Show qualification preview window
                        var assessments = AssessQualificationStatus(row);
                        var previewWindow = new QualificationPreviewWindow(row, assessments);
                        previewWindow.Owner = this;
                        previewWindow.Title = $"Review Qualifications - {row.FullName} ({i + 1} of {validSailors.Count})";
                        
                        var dialogResult = previewWindow.ShowDialog();
                        
                        if (dialogResult == true && previewWindow.SaveConfirmed)
                        {
                            // User approved - process the qualification update
                            try
                            {
                                await ProcessQualificationUpdate(dbContext, qualRepo, personnelWithQuals, row, session, sessionId);
                                savedCount++;
                                Console.WriteLine($"Qualification update completed for {row.FullName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing qualification update for {row.FullName}: {ex.Message}");
                                MessageBox.Show($"Error saving qualifications for {row.FullName}: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return; // Exit the process on error
                            }
                        }
                        else if (dialogResult == false)
                        {
                            // User clicked Edit - return to form without saving
                            MessageBox.Show("Returning to form for editing. No changes have been saved.", "Edit Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else
                        {
                            // User cancelled - exit the process
                            return;
                        }
                    }
                    else
                    {
                        // New personnel - show qualification preview window
                        var assessments = AssessQualificationStatus(row);
                        var previewWindow = new QualificationPreviewWindow(row, assessments);
                        previewWindow.Owner = this;
                        previewWindow.Title = $"Review New Sailor - {row.FullName} ({i + 1} of {validSailors.Count})";
                        
                        var dialogResult = previewWindow.ShowDialog();
                        
                        if (dialogResult == true && previewWindow.SaveConfirmed)
                        {
                            // User approved - create new personnel and process qualification
                            try
                            {
                                var newPersonnel = new Personnel
                                {
                                    DODId = row.DodId,
                                    LastName = row.FullName.Contains(",") ? row.FullName.Split(",")[0].Trim() : row.FullName,
                                    FirstName = row.FullName.Contains(",") ? row.FullName.Split(",")[1].Trim() : "",
                                    Rate = row.RankRate,
                                    Rank = row.RankRate,
                                    DutySection = row.DutySection,
                                    Designator = row.Designator
                                };
                                
                                int personnelId = await personnelRepo.AddPersonnelAsync(dbContext, newPersonnel);
                                newPersonnel.Id = personnelId;
                                Console.WriteLine($"Created new personnel with ID: {personnelId}");
                                
                                await ProcessQualificationUpdate(dbContext, qualRepo, newPersonnel, row, session, sessionId);
                                savedCount++;
                                Console.WriteLine($"Qualification update completed for new personnel {row.FullName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing qualification update for new personnel {row.FullName}: {ex.Message}");
                                MessageBox.Show($"Error saving qualifications for new personnel {row.FullName}: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return; // Exit the process on error
                            }
                        }
                        else if (dialogResult == false)
                        {
                            // User clicked Edit - return to form without saving
                            MessageBox.Show("Returning to form for editing. No changes have been saved.", "Edit Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else
                        {
                            // User cancelled - exit the process
                            return;
                        }
                    }
                }

                // All sailors have been approved - show final success message
                string successMessage = $"3591/1 Form Generated and Added to Training Jackets.\nDashboard Updated.\n\nSaved {savedCount} sailor qualifications for session '{session.ShipStation}'";
                if (pdfGeneratedCount > 0)
                    successMessage += $"\nGenerated filled 3591/1 PDF: {Path.GetFileName(pdfPath)}";
                
                MessageBox.Show(successMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Auto-open the generated PDF
                if (pdfGeneratedCount > 0 && !string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening PDF: {ex.Message}");
                        MessageBox.Show($"PDF was generated but could not be opened automatically.\nLocation: {pdfPath}", "PDF Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                
                // Refresh the dashboard
                Console.WriteLine("Refreshing dashboard...");
                _allPersonnelCache.Clear();
                await LoadData();
                Console.WriteLine("Dashboard refresh completed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving qualifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                dbContext?.Dispose();
            }
        }

        private string CheckPdfAcroFormFields(string pdfPath)
        {
            try
            {
                using var reader = new iTextSharp.text.pdf.PdfReader(pdfPath);
                var form = reader.AcroForm;
                if (form != null && form.Fields != null && form.Fields.Count > 0)
                {
                    return $"✓ PDF '{Path.GetFileName(pdfPath)}' contains {form.Fields.Count} AcroForm fields and is compatible.";
                }
                else
                {
                    return $"✗ PDF '{Path.GetFileName(pdfPath)}' does NOT contain AcroForm fields and is NOT compatible.";
                }
            }
            catch (Exception ex)
            {
                return $"✗ Error checking AcroForm fields: {ex.Message}";
            }
        }

        private Task<string> Generate3591PdfForSession(QualificationSession session, List<SailorQualification> sailors)
        {
            try
            {
                // Get the path to your 3591/1 template PDF
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3591_1QualTrack.pdf");
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Could not find 3591_1QualTrack.pdf template. Please ensure the file is in the application directory.");
                }

                // Create output path for the filled PDF
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedForms");
                Directory.CreateDirectory(outputDir);
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = Path.Combine(outputDir, $"3591_1_Filled_{session.ShipStation}_{timestamp}.pdf");

                // Fill the PDF with session and sailor data
                using var reader = new iTextSharp.text.pdf.PdfReader(templatePath);
                using var stamper = new iTextSharp.text.pdf.PdfStamper(reader, new FileStream(outputPath, FileMode.Create));
                var fields = stamper.AcroFields;

                // Fill session-level fields (Header fields)
                SetFieldValue(fields, "SHIP OR STATION", session.ShipStation ?? string.Empty);
                SetFieldValue(fields, "DIVISION OR ACTIVITY", session.DivisionActivity ?? string.Empty);
                SetFieldValue(fields, "RANGE NAMELOCATION", session.RangeNameLocation ?? string.Empty);
                SetFieldValue(fields, "DATE OF FIRING", session.DateOfFiring?.ToString("MM/dd/yyyy") ?? string.Empty);
                SetFieldValue(fields, "WEAPON S FIRED", session.WeaponsFired ?? string.Empty);

                // Fill sailor data (up to 15 rows)
                for (int i = 0; i < Math.Min(sailors.Count, 15); i++)
                {
                    var sailor = sailors[i];
                    int rowNumber = i + 1;

                    // Basic sailor info
                    SetFieldValue(fields, $"FULL NAMERow{rowNumber}", sailor.FullName ?? string.Empty);
                    SetFieldValue(fields, $"DoDIDRow{rowNumber}", sailor.DodId ?? string.Empty);
                    SetFieldValue(fields, $"RANK RATERow{rowNumber}", sailor.RankRate ?? string.Empty);

                    // NHQC Column (Column 1)
                    if (sailor.NHQC.HasValue)
                    {
                        string nhqcValue = sailor.NHQC.Value.ToString();
                        SetFieldValue(fields, $"Score  Award CodeRow{rowNumber}", nhqcValue);
                        Console.WriteLine($"Setting Score  Award CodeRow{rowNumber} = {nhqcValue}");
                    }
                    
                    // NHQC Q/U Status
                    string nhqcQualStatus = DetermineQualificationStatus(sailor, "NHQC");
                    SetFieldValue(fields, $"Score  Award CodeRow{rowNumber}_2", nhqcQualStatus);
                    Console.WriteLine($"Setting Score  Award CodeRow{rowNumber}_2 = {nhqcQualStatus}");

                    // RQC Column (Column 2)
                    if (sailor.RQC.HasValue)
                    {
                        string rqcValue = sailor.RQC.Value.ToString();
                        SetFieldValue(fields, $"Score  Award CodeRow{rowNumber}_3", rqcValue);
                        Console.WriteLine($"Setting Score  Award CodeRow{rowNumber}_3 = {rqcValue}");
                    }
                    
                    // RQC Q/U Status
                    string rqcQualStatus = DetermineQualificationStatus(sailor, "RQC");
                    SetFieldValue(fields, $"Score  Award CodeRow{rowNumber}_4", rqcQualStatus);
                    Console.WriteLine($"Setting Score  Award CodeRow{rowNumber}_4 = {rqcQualStatus}");

                    // SPWC Column (Column 3)
                    if (sailor.SPWCT1.HasValue)
                        SetFieldValue(fields, $"T1Row{rowNumber}", sailor.SPWCT1.Value.ToString());
                    if (sailor.SPWCT2.HasValue)
                        SetFieldValue(fields, $"T2Row{rowNumber}", sailor.SPWCT2.Value.ToString());
                    if (sailor.SPWCT3.HasValue)
                        SetFieldValue(fields, $"T3Row{rowNumber}", sailor.SPWCT3.Value.ToString());
                    
                    // SPWC Q/U Status
                    string spwcQualStatus = DetermineQualificationStatus(sailor, "SPWC");
                    SetFieldValue(fields, $"Q or uRow{rowNumber}", spwcQualStatus);
                    Console.WriteLine($"Setting Q or uRow{rowNumber} = {spwcQualStatus}");

                    // HPWC Column (Column 4)
                    if (sailor.HPWCT1.HasValue)
                        SetFieldValue(fields, $"T1Row{rowNumber}_2", sailor.HPWCT1.Value.ToString());
                    if (sailor.HPWCT2.HasValue)
                        SetFieldValue(fields, $"T2Row{rowNumber}_2", sailor.HPWCT2.Value.ToString());
                    if (sailor.HPWCT3.HasValue)
                        SetFieldValue(fields, $"T3Row{rowNumber}_2", sailor.HPWCT3.Value.ToString());
                    
                    // HPWC Q/U Status
                    string hpwcQualStatus = DetermineQualificationStatus(sailor, "HPWC");
                    SetFieldValue(fields, $"Q or uRow{rowNumber}_2", hpwcQualStatus);
                    Console.WriteLine($"Setting Q or uRow{rowNumber}_2 = {hpwcQualStatus}");

                    // HLLC Column (Column 5)
                    if (sailor.HLLC.HasValue)
                    {
                        string hllcValue = sailor.HLLC.Value.ToString();
                        SetFieldValue(fields, $"Numeric Score QoruRow{rowNumber}", hllcValue);
                        Console.WriteLine($"Setting Numeric Score QoruRow{rowNumber} = {hllcValue}");
                    }
                    
                    // HLLC Q/U Status
                    string hllcQualStatus = DetermineQualificationStatus(sailor, "HLLC");
                    SetFieldValue(fields, $"Numeric Score QoruRow{rowNumber}_2", hllcQualStatus);
                    Console.WriteLine($"Setting Numeric Score QoruRow{rowNumber}_2 = {hllcQualStatus}");

                    // RLLC Column (Column 6)
                    if (sailor.RLLC.HasValue)
                    {
                        string rllcValue = sailor.RLLC.Value.ToString();
                        SetFieldValue(fields, $"Numeric Score QoruRow{rowNumber}_3", rllcValue);
                        Console.WriteLine($"Setting Numeric Score QoruRow{rowNumber}_3 = {rllcValue}");
                    }
                    
                    // RLLC Q/U Status
                    string rllcQualStatus = DetermineQualificationStatus(sailor, "RLLC");
                    SetFieldValue(fields, $"Numeric Score QoruRow{rowNumber}_4", rllcQualStatus);
                    Console.WriteLine($"Setting Numeric Score QoruRow{rowNumber}_4 = {rllcQualStatus}");
                }

                // Fill footer fields
                SetFieldValue(fields, "CERTIFYING SIGNATURE", session.RsoSignature ?? string.Empty);
                SetFieldValue(fields, "CERTIFYING DATE", session.RsoSignatureDate?.ToString("MM/dd/yyyy") ?? DateTime.Now.ToString("MM/dd/yyyy"));
                SetFieldValue(fields, "Page_Numerator", "1");
                SetFieldValue(fields, "Page_Denominator", "1");

                stamper.Close();
                reader.Close();

                return Task.FromResult(outputPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating 3591/1 PDF: {ex.Message}", ex);
            }
        }

        private void SetFieldValue(iTextSharp.text.pdf.AcroFields form, string fieldName, string value)
        {
            try
            {
                if (form.Fields.ContainsKey(fieldName))
                {
                    // Use explicit method call to avoid overload confusion
                    bool success = form.SetField(fieldName, value ?? "", value ?? "");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire process
                System.Diagnostics.Debug.WriteLine($"Error setting field {fieldName}: {ex.Message}");
            }
        }

        private string DetermineQualificationStatus(SailorQualification sailor, string weapon)
        {
            // Determine if sailor is qualified based on weapon and scores
            // TODO: When DEPLOYMENT MODE is implemented, supporting shoots (HLLC, RLLC, HPWC) 
            // will not be required for qualification - only primary courses (NHQC, RQC, SPWC)
            
            weapon = weapon.ToUpper();
            
            switch (weapon)
            {
                case "NHQC":
                    // NHQC qualification: Score must be between 180-240
                    if (sailor.NHQC.HasValue && sailor.NHQC > 0 && sailor.NHQC >= 180 && sailor.NHQC <= 240)
                        return "Q";
                    return ""; // Leave blank if no score or score is 0
                    
                case "RQC":
                    // RQC qualification: Score must be between 140-200
                    if (sailor.RQC.HasValue && sailor.RQC > 0 && sailor.RQC >= 140 && sailor.RQC <= 200)
                        return "Q";
                    return ""; // Leave blank if no score or score is 0
                    
                case "SPWC":
                    // SPWC qualification: Total score must be between 90-162
                    int spwcTotal = (sailor.SPWCT1 ?? 0) + (sailor.SPWCT2 ?? 0) + (sailor.SPWCT3 ?? 0);
                    if (spwcTotal > 0 && spwcTotal >= 90 && spwcTotal <= 162)
                        return "Q";
                    return ""; // Leave blank if no scores or total is 0
                    
                case "HPWC":
                    // HPWC qualification: Total score must be between 12-18
                    int hpwcTotal = (sailor.HPWCT1 ?? 0) + (sailor.HPWCT2 ?? 0) + (sailor.HPWCT3 ?? 0);
                    if (hpwcTotal > 0 && hpwcTotal >= 12 && hpwcTotal <= 18)
                        return "Q";
                    return ""; // Leave blank if no scores or total is 0
                    
                case "HLLC":
                    // HLLC qualification: Score must be between 12-18
                    if (sailor.HLLC.HasValue && sailor.HLLC > 0 && sailor.HLLC >= 12 && sailor.HLLC <= 18)
                        return "Q";
                    return ""; // Leave blank if no score or score is 0
                    
                case "RLLC":
                    // RLLC qualification: Score must be between 14-20
                    if (sailor.RLLC.HasValue && sailor.RLLC > 0 && sailor.RLLC >= 14 && sailor.RLLC <= 20)
                        return "Q";
                    return ""; // Leave blank if no score or score is 0
                    
                default:
                    return ""; // Default to blank
            }
        }





        private void TestPdfFields_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fieldNames = _pdfGenerationService.GetPdfFieldNames();
                var fieldList = string.Join("\n", fieldNames);
                MessageBox.Show($"PDF Field Names:\n\n{fieldList}", "PDF Fields", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading PDF fields: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenGeneratedForms_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string generatedFormsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedForms");
                
                if (!Directory.Exists(generatedFormsPath))
                {
                    MessageBox.Show("GeneratedForms folder does not exist yet. No PDFs have been generated.", "No Forms Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Open the folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", generatedFormsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening GeneratedForms folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private int DetermineQualificationCategory(string weapon, SailorQualification row)
        {
            // Determine category based on weapon and scores
            switch (weapon.ToUpper())
            {
                case "M9":
                    // CAT II requires NHQC, HLLC, and HPWC scores
                    if (row.NHQC.HasValue && row.HLLC.HasValue && row.HPWCT1.HasValue)
                    {
                        return 2; // CAT II
                    }
                    return 1; // CAT I (default)
                    
                case "M4/M16":
                    // CAT II requires RQC and RLC scores for full qual, but sustainment may have only RQC
                    // If RQC is present, treat as CAT II (supports sustainment quals)
                    if (row.RQC.HasValue)
                    {
                        return 2; // CAT II (full or sustainment)
                    }
                    return 1; // CAT I (default, but not valid for M4/M16)
                    
                case "M500":
                    // CAT II requires SPWC scores
                    if (row.SPWCT1.HasValue || row.SPWCT2.HasValue || row.SPWCT3.HasValue)
                    {
                        return 2; // CAT II
                    }
                    return 1; // CAT I (default)
                    
                case "M240":
                case "M2":
                    // These are typically CAT II weapons
                    return 2;
                    
                default:
                    return 1; // Default to CAT I
            }
        }

        // This method is not currently used - keeping for future reference
        /*
        private Qualification CreateQualificationFromRow(SailorQualification row, QualificationSession session, int personnelId, int sessionId)
        {
            var qualDate = session.DateOfFiring ?? DateTime.Now;
            var weapon = session.WeaponsFired;
            var category = DetermineQualificationCategory(weapon, row);

            // Aggregate multi-stage scores
            int? hpwcSum = null;
            if (row.HPWCT1.HasValue || row.HPWCT2.HasValue || row.HPWCT3.HasValue)
                hpwcSum = (row.HPWCT1 ?? 0) + (row.HPWCT2 ?? 0) + (row.HPWCT3 ?? 0);
            int? spwcSum = null;
            if (row.SPWCT1.HasValue || row.SPWCT2.HasValue || row.SPWCT3.HasValue)
                spwcSum = (row.SPWCT1 ?? 0) + (row.SPWCT2 ?? 0) + (row.SPWCT3 ?? 0);

            var qualification = new Qualification
            {
                PersonnelId = personnelId,
                Weapon = weapon,
                Category = category,
                DateQualified = qualDate,
                QualificationSessionId = sessionId // Link to the 3591/1 session
            };

            // Create details object
            qualification.Details = new QualificationDetails();

            // Set weapon-specific scores
            switch (weapon.ToUpper())
            {
                case "M9":
                    if (category == 2) // CAT II
                    {
                        qualification.Details.NHQCScore = row.NHQC;
                        qualification.Details.HLLCScore = row.HLLC;
                        qualification.Details.HPWCScore = hpwcSum;
                    }
                    else // CAT I
                    {
                        qualification.Details.HQCScore = row.HQC;
                    }
                    break;

                case "M4/M16":
                    if (category == 2) // CAT II
                    {
                        qualification.Details.RQCScore = row.RQC;
                        qualification.Details.RLCScore = row.RLLC;
                    }
                    else // CAT I
                    {
                        qualification.Details.RQCScore = row.RQC;
                    }
                    break;

                case "M500":
                    if (category == 2) // CAT II
                    {
                        qualification.Details.SPWCScore = spwcSum;
                    }
                    else // CAT I
                    {
                        qualification.Details.SPWCScore = row.SPWC;
                    }
                    break;

                case "M240":
                case "M2":
                    qualification.Details.COFScore = row.M240Score;
                    break;

                default:
                    // For other weapons, we might need to add more fields
                    break;
            }

            return qualification;
        }
        */

        private SailorQualification ConvertToSailorQualification(Qualification qualification)
        {
            var row = new SailorQualification();
            
            // Set weapon-specific scores based on the qualification
            switch (qualification.Weapon.ToUpper())
            {
                case "M9":
                    if (qualification.Category == 2) // CAT II
                    {
                        row.NHQC = qualification.Details?.NHQCScore;
                        row.HLLC = qualification.Details?.HLLCScore;
                        row.HPWCT1 = qualification.Details?.HPWCScore; // This is the sum, we'll need to handle this differently
                    }
                    else // CAT I
                    {
                        row.HQC = qualification.Details?.HQCScore;
                    }
                    break;

                case "M4/M16":
                    if (qualification.Category == 2) // CAT II
                    {
                        row.RQC = qualification.Details?.RQCScore;
                        row.RLLC = qualification.Details?.RLCScore;
                    }
                    else // CAT I
                    {
                        row.RQC = qualification.Details?.RQCScore;
                    }
                    break;

                case "M500":
                    if (qualification.Category == 2) // CAT II
                    {
                        row.SPWCT1 = qualification.Details?.SPWCScore; // This is the sum, we'll need to handle this differently
                    }
                    else // CAT I
                    {
                        row.SPWC = qualification.Details?.SPWCScore;
                    }
                    break;

                case "M240":
                case "M2":
                    row.M240Score = qualification.Details?.COFScore;
                    break;

                default:
                    // For other weapons, we might need to add more fields
                    break;
            }

            return row;
        }

        private async Task ProcessQualificationUpdate(DatabaseContext dbContext, QualificationRepository qualRepo, Personnel personnel, SailorQualification row, QualificationSession session, int sessionId)
        {
            var qualDate = session.DateOfFiring ?? DateTime.Now;
            
            Console.WriteLine($"Processing qualification update for {personnel.LastName}, {personnel.FirstName} (ID: {personnel.Id})");
            
            // Assess qualification status for all weapons
            var assessments = AssessQualificationStatus(row);
            
            // Ensure Qualifications list is initialized
            if (personnel.Qualifications == null)
            {
                personnel.Qualifications = new List<Qualification>();
            }

            // Process each weapon that has data
            foreach (var assessment in assessments)
            {
                // Skip weapons with no data - check if any scores exist for this weapon
                bool hasData = false;
                switch (assessment.Weapon)
                {
                    case "M9":
                        hasData = row.NHQC.HasValue || row.HLLC.HasValue || 
                                 row.HPWCT1.HasValue || row.HPWCT2.HasValue || row.HPWCT3.HasValue;
                        break;
                    case "M4/M16":
                        hasData = row.RQC.HasValue || row.RLLC.HasValue;
                        break;
                    case "M500":
                        hasData = row.SPWCT1.HasValue || row.SPWCT2.HasValue || row.SPWCT3.HasValue;
                        break;
                }
                
                if (!hasData)
                {
                    Console.WriteLine($"Skipping {assessment.Weapon} - no data");
                    continue;
                }

                Console.WriteLine($"Processing {assessment.Weapon} - has data, qualified: {assessment.IsQualified}");

                string weapon = assessment.Weapon;
                var category = DetermineQualificationCategory(weapon, row);

                // Aggregate multi-stage scores
                int? hpwcSum = null;
                if (row.HPWCT1.HasValue || row.HPWCT2.HasValue || row.HPWCT3.HasValue)
                    hpwcSum = (row.HPWCT1 ?? 0) + (row.HPWCT2 ?? 0) + (row.HPWCT3 ?? 0);
                int? spwcSum = null;
                if (row.SPWCT1.HasValue || row.SPWCT2.HasValue || row.SPWCT3.HasValue)
                    spwcSum = (row.SPWCT1 ?? 0) + (row.SPWCT2 ?? 0) + (row.SPWCT3 ?? 0);

                // Check if this sailor already has a qualification for this weapon
                var existingQual = personnel.Qualifications.FirstOrDefault(q => q.Weapon == weapon);

                if (existingQual != null)
                {
                    // Check if the new qualification is newer (sustainment)
                    if (qualDate > existingQual.DateQualified)
                    {
                        Console.WriteLine($"Adding sustainment qualification for {weapon}");
                        // This is a sustainment qualification - add it as a new record
                        var sustainmentQual = new Qualification
                        {
                            PersonnelId = personnel.Id,
                            Weapon = weapon,
                            Category = category,
                            DateQualified = qualDate,
                            QualificationSessionId = sessionId,
                            Details = new QualificationDetails
                            {
                                NHQCScore = weapon == "M9" ? row.NHQC : null,
                                RQCScore = weapon == "M4/M16" ? row.RQC : null,
                                SPWCScore = weapon == "M500" ? spwcSum : null,
                                HPWCScore = weapon == "M9" ? hpwcSum : null,
                                HLLCScore = weapon == "M9" ? row.HLLC : null,
                                RLCScore = weapon == "M4/M16" ? row.RLLC : null,
                                // Mark as sustainment
                                SustainmentDate = qualDate,
                                SustainmentScore = weapon == "M9" ? (row.NHQC ?? 0) : 
                                                  weapon == "M4/M16" ? (row.RQC ?? 0) : 
                                                  weapon == "M500" ? (spwcSum ?? 0) : 0
                            }
                        };
                        await qualRepo.AddQualificationAsync(dbContext, sustainmentQual);
                        Console.WriteLine($"Sustainment qualification saved for {weapon}");
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {weapon} - existing qualification is newer");
                    }
                }
                else
                {
                    Console.WriteLine($"Adding new qualification for {weapon}");
                    // No existing qualification for this weapon - add new qualification
                    var newQual = new Qualification
                    {
                        PersonnelId = personnel.Id,
                        Weapon = weapon,
                        Category = category,
                        DateQualified = qualDate,
                        QualificationSessionId = sessionId,
                        Details = new QualificationDetails
                        {
                            NHQCScore = weapon == "M9" ? row.NHQC : null,
                            RQCScore = weapon == "M4/M16" ? row.RQC : null,
                            SPWCScore = weapon == "M500" ? spwcSum : null,
                            HPWCScore = weapon == "M9" ? hpwcSum : null,
                            HLLCScore = weapon == "M9" ? row.HLLC : null,
                            RLCScore = weapon == "M4/M16" ? row.RLLC : null
                        }
                    };
                    await qualRepo.AddQualificationAsync(dbContext, newQual);
                    Console.WriteLine($"New qualification saved for {weapon}");
                }
            }
        }





        private List<QualificationAssessment> AssessQualificationStatus(SailorQualification sailor)
        {
            var assessments = new List<QualificationAssessment>();

            // Assess M9 qualification
            var m9Assessment = new QualificationAssessment { Weapon = "M9" };
            if (sailor.NHQC.HasValue)
            {
                m9Assessment.PresentFields.Add($"NHQC: {sailor.NHQC.Value}");
                if (sailor.NHQC.Value < 180)
                    m9Assessment.MissingFields.Add("NHQC score below 180");
            }
            else
                m9Assessment.MissingFields.Add("NHQC score");

            if (sailor.HLLC.HasValue)
            {
                m9Assessment.PresentFields.Add($"HLLC: {sailor.HLLC.Value}");
                if (sailor.HLLC.Value < 12)
                    m9Assessment.MissingFields.Add("HLLC score below 12");
            }
            else
                m9Assessment.MissingFields.Add("HLLC score");

            int hpwcTotal = (sailor.HPWCT1 ?? 0) + (sailor.HPWCT2 ?? 0) + (sailor.HPWCT3 ?? 0);
            if (hpwcTotal > 0)
            {
                m9Assessment.PresentFields.Add($"HPWC Total: {hpwcTotal}");
                if (hpwcTotal < 12)
                    m9Assessment.MissingFields.Add("HPWC total below 12");
            }
            else
                m9Assessment.MissingFields.Add("HPWC scores");

            m9Assessment.IsQualified = m9Assessment.MissingFields.Count == 0;
            assessments.Add(m9Assessment);

            // Assess M4/M16 qualification
            var m16Assessment = new QualificationAssessment { Weapon = "M4/M16" };
            if (sailor.RQC.HasValue)
            {
                m16Assessment.PresentFields.Add($"RQC: {sailor.RQC.Value}");
                if (sailor.RQC.Value < 140)
                    m16Assessment.MissingFields.Add("RQC score below 140");
            }
            else
                m16Assessment.MissingFields.Add("RQC score");

            if (sailor.RLLC.HasValue)
            {
                m16Assessment.PresentFields.Add($"RLLC: {sailor.RLLC.Value}");
                if (sailor.RLLC.Value < 14)
                    m16Assessment.MissingFields.Add("RLLC score below 14");
            }
            else
                m16Assessment.MissingFields.Add("RLLC score");

            m16Assessment.IsQualified = m16Assessment.MissingFields.Count == 0;
            assessments.Add(m16Assessment);

            // Assess M500 qualification
            var m500Assessment = new QualificationAssessment { Weapon = "M500" };
            int spwcTotal = (sailor.SPWCT1 ?? 0) + (sailor.SPWCT2 ?? 0) + (sailor.SPWCT3 ?? 0);
            if (spwcTotal > 0)
            {
                m500Assessment.PresentFields.Add($"SPWC Total: {spwcTotal}");
                if (spwcTotal < 90)
                    m500Assessment.MissingFields.Add("SPWC total below 90");
            }
            else
                m500Assessment.MissingFields.Add("SPWC scores");

            m500Assessment.IsQualified = m500Assessment.MissingFields.Count == 0;
            assessments.Add(m500Assessment);

            return assessments;
        }

        private void DE_ClearForm_Click(object sender, RoutedEventArgs e)
        {
            deSailorQualifications.Clear();
            ClearWeaponCheckboxes();
            DE_ShipStationBox.SelectedIndex = 0;
            DE_DivisionActivityBox.SelectedIndex = 0;
            DE_RangeNameLocationBox.SelectedIndex = 0;
            DE_DateOfFiringPicker.SelectedDate = DateTime.Today;
        }

        // ========== 3591/2 Crew Served Weapon Tab Handlers ==========
        
        private async Task InitializeCrewServedWeaponTab()
        {
            cswEntries.Clear();
            CSW_EntryGrid.ItemsSource = cswEntries;
            await LoadCSWSailorComboBoxData();
            
            CSW_ShipStationBox.SelectedIndex = 0;
            CSW_DivisionActivityBox.SelectedIndex = 0;
            CSW_RangeNameLocationBox.SelectedIndex = 0;
            CSW_DateOfFiringPicker.SelectedDate = DateTime.Today;
            CSW_CSWIComboBox.SelectedIndex = -1;
        }

        private async Task LoadCSWSailorComboBoxData()
        {
            try
            {
                using var dbContext = new DatabaseContext();
                var personnelRepo = new PersonnelRepository();
                var allPersonnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
                
                var sailorModels = allPersonnel.Select(p => new SailorDisplayModel(
                    p.Id, p.LastName, p.FirstName, p.DODId, p.Rank, p.Rate)).ToList();
                
                CSW_SailorSelectionComboBox.ItemsSource = sailorModels;
                CSW_CSWIComboBox.ItemsSource = sailorModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sailor data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CSW_AddRow_Click(object sender, RoutedEventArgs e)
        {
            cswEntries.Add(new CrewServedWeaponEntry());
        }

        private void CSW_RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (CSW_EntryGrid.SelectedItem is CrewServedWeaponEntry selected && cswEntries.Contains(selected))
                cswEntries.Remove(selected);
        }

        private void CSW_SailorSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Show preview of selected sailor
        }

        private void CSW_SailorLookupTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            var query = CSW_SailorLookupTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            var sailorModels = CSW_SailorSelectionComboBox.ItemsSource as IEnumerable<SailorDisplayModel>;
            var matches = sailorModels?
                .Where(sailor => sailor.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<SailorDisplayModel>();

            if (matches.Count == 0)
            {
                MessageBox.Show($"No sailor found matching '{query}'.", "Lookup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (matches.Count == 1)
            {
                CSW_SailorSelectionComboBox.SelectedItem = matches[0];
            }
            else
            {
                CSW_SailorSelectionComboBox.SelectedItem = null;
                CSW_SailorSelectionComboBox.IsDropDownOpen = true;
                MessageBox.Show($"Found {matches.Count} matches. Please select the correct sailor from the list (DODID shown).", "Lookup", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            e.Handled = true;
        }

        private void CSW_AddSelectedSailor_Click(object sender, RoutedEventArgs e)
        {
            if (CSW_SailorSelectionComboBox.SelectedItem is SailorDisplayModel selectedSailor)
            {
                var newEntry = new CrewServedWeaponEntry
                {
                    GunnerName = selectedSailor.DisplayName,
                    GunnerDodId = selectedSailor.DODId,
                    GunnerRankRate = selectedSailor.RankRate
                };
                cswEntries.Insert(0, newEntry);
            }
        }

        private static int? CalculateCrewServedCofScore(CrewServedWeaponEntry entry, string weapon)
        {
            int Sum(params int?[] scores) => scores.Where(score => score.HasValue).Sum(score => score.Value);

            return weapon switch
            {
                "M240" => Sum(
                    entry.LightFreeP1, entry.LightFreeP2, entry.LightFreeP3, entry.LightFreeP4, entry.LightFreeP5, entry.LightFreeP6,
                    entry.LightTeP1, entry.LightTeP2, entry.LightTeP3, entry.LightTeP4, entry.LightTeP5),
                "M2A1" => Sum(
                    entry.HeavyFreeP1, entry.HeavyFreeP2, entry.HeavyFreeP3, entry.HeavyFreeP4, entry.HeavyFreeP5, entry.HeavyFreeP6,
                    entry.HeavyTeP1, entry.HeavyTeP2, entry.HeavyTeP3, entry.HeavyTeP4, entry.HeavyTeP5),
                "M2" => Sum(
                    entry.HeavyFreeP1, entry.HeavyFreeP2, entry.HeavyFreeP3, entry.HeavyFreeP4, entry.HeavyFreeP5, entry.HeavyFreeP6,
                    entry.HeavyTeP1, entry.HeavyTeP2, entry.HeavyTeP3, entry.HeavyTeP4, entry.HeavyTeP5),
                _ => null
            };
        }

        private void CSW_RefreshSailorList_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadCSWSailorComboBoxData();
        }

        private void CSW_ClearForm_Click(object sender, RoutedEventArgs e)
        {
            cswEntries.Clear();
            CSW_M240CheckBox.IsChecked = false;
            CSW_M2CheckBox.IsChecked = false;
            CSW_ShipStationBox.SelectedIndex = 0;
            CSW_DivisionActivityBox.SelectedIndex = 0;
            CSW_RangeNameLocationBox.SelectedIndex = 0;
            CSW_DateOfFiringPicker.SelectedDate = DateTime.Today;
            CSW_CSWIComboBox.SelectedIndex = -1;
        }

        private string GetCSWWeaponsFiredFromCheckboxes()
        {
            var weapons = new List<string>();
            if (CSW_M240CheckBox.IsChecked == true) weapons.Add("M240");
            if (CSW_M2CheckBox.IsChecked == true) weapons.Add("M2A1");
            return string.Join(", ", weapons);
        }

        private async void CSW_SaveForm_Click(object sender, RoutedEventArgs e)
        {
            if (!RequirePermission(RbacPermission.ManageCrewServed, "Save 3591/2 Crew Served"))
            {
                return;
            }

            DatabaseContext? dbContext = null;
            try
            {
                Console.WriteLine("Starting 3591/2 form save process...");
                
                dbContext = new DatabaseContext();
                var sessionRepo = new CrewServedWeaponSessionRepository();
                var personnelRepo = new PersonnelRepository();
                var qualRepo = new QualificationRepository();
                var crewServedService = new CrewServedWeaponService(
                    _qualificationService,
                    new CrewServedWeaponSessionRepository(),
                    qualRepo,
                    _rbacService,
                    _currentUserContext);

                // Get selected weapon
                string weapon = GetCSWWeaponsFiredFromCheckboxes();
                if (string.IsNullOrEmpty(weapon))
                {
                    MessageBox.Show("Please select at least one weapon (M240 or M2A1).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // For now, handle single weapon - if both selected, use first one
                weapon = weapon.Split(',')[0].Trim();

                // 1. Save session-level data
                var session = new CrewServedWeaponSession
                {
                    ShipStation = CSW_ShipStationBox.SelectedValue?.ToString() ?? string.Empty,
                    DivisionActivity = CSW_DivisionActivityBox.SelectedValue?.ToString() ?? string.Empty,
                    Weapon = weapon,
                    RangeNameLocation = CSW_RangeNameLocationBox.SelectedValue?.ToString() ?? string.Empty,
                    DateOfFiring = CSW_DateOfFiringPicker.SelectedDate,
                    InstructorName = (CSW_CSWIComboBox.SelectedItem as SailorDisplayModel)?.DisplayName ?? string.Empty,
                    InstructorRankRate = (CSW_CSWIComboBox.SelectedItem as SailorDisplayModel)?.RankRate ?? string.Empty,
                    CreatedDate = DateTime.Now
                };

                int savedCount = 0;
                string pdfPath = "";

                // Process each entry
                foreach (var entry in cswEntries)
                {
                    if (string.IsNullOrWhiteSpace(entry.GunnerDodId)) continue;

                    // Validate DODID format
                    if (entry.GunnerDodId.Length != 10 || !entry.GunnerDodId.All(char.IsDigit))
                    {
                        MessageBox.Show($"Invalid DODID format for gunner '{entry.GunnerName}': {entry.GunnerDodId}. DODID must be exactly 10 digits.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    // Validate COF score
                    var cofScore = CalculateCrewServedCofScore(entry, weapon);
                    if (!cofScore.HasValue || cofScore.Value < 100)
                    {
                        MessageBox.Show($"Invalid COF score for entry '{entry.GunnerName}': {cofScore}. COF score must be >= 100.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    // Set crew member data
                    session.GunnerName = entry.GunnerName;
                    session.GunnerDODID = entry.GunnerDodId;
                    session.GunnerRankRate = entry.GunnerRankRate;
                    session.AssistantGunnerName = entry.AssistantGunnerName;
                    session.AssistantGunnerDODID = entry.AssistantGunnerDodId;
                    session.AssistantGunnerRankRate = entry.AssistantGunnerRankRate;
                    session.AmmunitionHandlerName = entry.AmmunitionHandlerName;
                    session.AmmunitionHandlerDODID = entry.AmmunitionHandlerDodId;
                    session.AmmunitionHandlerRankRate = entry.AmmunitionHandlerRankRate;
                    entry.COFScore = cofScore;
                    session.CourseOfFireScore = cofScore;
                    session.IsQualified = entry.IsQualified;

                    // Save session
                    int sessionId = await sessionRepo.AddSessionAsync(dbContext, session);

                    // Find or create personnel for gunner
                    var personnel = await personnelRepo.GetPersonnelByDODIdAsync(dbContext, entry.GunnerDodId);
                    if (personnel == null)
                    {
                        MessageBox.Show($"Personnel not found for DODID: {entry.GunnerDodId}. Please add personnel first.",
                            "Personnel Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    // Create qualification for gunner
                    int qualId = await crewServedService.CreateQualificationFromSessionAsync(dbContext, session, personnel.Id);
                    savedCount++;

                    // Generate PDF if this is the first entry
                    if (string.IsNullOrEmpty(pdfPath))
                    {
                        try
                        {
                            pdfPath = await Generate3591_2PdfForSession(session, cswEntries.ToList());
                            if (!string.IsNullOrWhiteSpace(pdfPath))
                            {
                                var signedPath = await TrySignPdfWithCacAsync(pdfPath, "3591/2 Qualification", session.InstructorName ?? string.Empty);
                                if (!string.IsNullOrWhiteSpace(signedPath))
                                {
                                    pdfPath = signedPath;
                                }

                                await sessionRepo.UpdateSessionPdfFilePathAsync(dbContext, sessionId, pdfPath);
                            }
                        }
                        catch (Exception pdfEx)
                        {
                            MessageBox.Show($"Warning: PDF generation failed: {pdfEx.Message}", "PDF Generation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                if (savedCount > 0)
                {
                    string successMessage = $"3591/2 Form Generated and Added to Training Jackets.\nDashboard Updated.\n\nSaved {savedCount} crew served weapon qualification(s).";
                    if (!string.IsNullOrEmpty(pdfPath))
                    {
                        successMessage += $"\nGenerated filled 3591/2 PDF: {Path.GetFileName(pdfPath)}";
                    }
                    MessageBox.Show(successMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = pdfPath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception openEx)
                        {
                            MessageBox.Show($"PDF generated but could not open automatically: {openEx.Message}", "Open PDF Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    
                    // Refresh dashboard
                    await LoadData();
                }
                else
                {
                    MessageBox.Show("No valid entries were saved.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving 3591/2 form: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                dbContext?.Dispose();
            }
        }

        private Task<string> Generate3591_2PdfForSession(CrewServedWeaponSession session, List<CrewServedWeaponEntry> entries)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3591_2QualTrack.pdf");
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedForms");

            var fieldMap = new CrewServedWeaponPdfFieldMap
            {
                // Session-level fields (top section)
                ShipStation = "Ship",
                DivisionActivity = "Division",
                Weapon = "Weapons",
                RangeNameLocation = "Range Name",
                DateOfFiring = "Date Firing",
                PageNumber = "Pg_Num",
                PageTotal = "Pg_Den",

                // Bottom signature fields
                Signature = "Signature",
                SignatureDate = "Signature Date",

                // Other session fields (to be mapped later)
                InstructorName = string.Empty,
                InstructorRankRate = string.Empty,

                // Entry-row fields (use "{row}" where the PDF expects a row number)
                NamePattern = "Name{row}",
                RankPattern = "Rank{row}",
                LightFreeP1Pattern = "light_FreeP1_{row}",
                LightFreeP2Pattern = "light_FreeP2_{row}",
                LightFreeP3Pattern = "light_FreeP3_{row}",
                LightFreeP4Pattern = "light_FreeP4_{row}",
                LightFreeP5Pattern = "light_FreeP5_{row}",
                LightFreeP6Pattern = "light_FreeP6_{row}",
                LightTeP1Pattern = "light_teP1_{row}",
                LightTeP2Pattern = "light_teP2_{row}",
                LightTeP3Pattern = "light_teP3_{row}",
                LightTeP4Pattern = "light_teP4_{row}",
                LightTeP5Pattern = "light_teP5_{row}",
                HeavyFreeP1Pattern = "heavy_FreeP1_{row}",
                HeavyFreeP2Pattern = "heavy_FreeP2_{row}",
                HeavyFreeP3Pattern = "heavy_FreeP3_{row}",
                HeavyFreeP4Pattern = "heavy_FreeP4_{row}",
                HeavyFreeP5Pattern = "heavy_FreeP5_{row}",
                HeavyFreeP6Pattern = "heavy_FreeP6_{row}",
                HeavyTeP1Pattern = "heavy_teP1_{row}",
                HeavyTeP2Pattern = "heavy_teP2_{row}",
                HeavyTeP3Pattern = "heavy_teP3_{row}",
                HeavyTeP4Pattern = "heavy_teP4_{row}",
                HeavyTeP5Pattern = "heavy_teP5_{row}"
            };

            var pdfService = new CrewServedWeaponPdfGenerationService(templatePath, outputDir, fieldMap);
            return Task.FromResult(pdfService.Generate3591_2Pdf(session, entries));
        }

        private async Task<string?> TrySignPdfWithCacAsync(string pdfPath, string purpose, string signerDisplayName, string? signatureFieldName = null)
        {
            var provider = new CacSignatureProvider();
            if (!provider.IsAvailable)
            {
                return null;
            }

            var request = new SignatureRequest
            {
                DocumentPath = pdfPath,
                Purpose = purpose,
                SignerDisplayName = string.IsNullOrWhiteSpace(signerDisplayName) ? Environment.UserName : signerDisplayName,
                SignatureFieldName = signatureFieldName
            };

            var result = await provider.RequestSignatureAsync(request);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "CAC Signature", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return result.SignedDocumentPath;
        }

        private void CSW_OpenGeneratedForms_Click(object sender, RoutedEventArgs e)
        {
            string formsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedForms");
            if (Directory.Exists(formsDir))
            {
                Process.Start("explorer.exe", formsDir);
            }
            else
            {
                MessageBox.Show("Generated forms directory not found.", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
} 