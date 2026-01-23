using System.Windows;
using System.Windows.Controls;
using QualTrack.Core.Models;

namespace QualTrack.UI
{
    /// <summary>
    /// Interaction logic for DashboardSetupWindow.xaml
    /// </summary>
    public partial class DashboardSetupWindow : Window
    {
        public DashboardColumnSettings ColumnSettings { get; private set; }

        public DashboardSetupWindow(DashboardColumnSettings currentSettings)
        {
            InitializeComponent();
            ColumnSettings = currentSettings.Clone();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            NameCheckBox.IsChecked = ColumnSettings.ShowName;
            RateCheckBox.IsChecked = ColumnSettings.ShowRate;
            DutySection3CheckBox.IsChecked = ColumnSettings.ShowDutySection3;
            DutySection6CheckBox.IsChecked = ColumnSettings.ShowDutySection6;
            M9CheckBox.IsChecked = ColumnSettings.ShowM9;
            M4M16CheckBox.IsChecked = ColumnSettings.ShowM4M16;
            M500CheckBox.IsChecked = ColumnSettings.ShowM500;
            M240CheckBox.IsChecked = ColumnSettings.ShowM240;
            M2CheckBox.IsChecked = ColumnSettings.ShowM2;
            DateQualifiedCheckBox.IsChecked = ColumnSettings.ShowDateQualified;
            LapsedQualificationsCheckBox.IsChecked = ColumnSettings.ShowLapsedQualifications;
            
            // Admin requirements
            AdminCheckBox.IsChecked = ColumnSettings.ShowAdmin;
            Form2760CheckBox.IsChecked = ColumnSettings.ShowForm2760;
            AAEScreeningCheckBox.IsChecked = ColumnSettings.ShowAAEScreening;
            DeadlyForceTrainingCheckBox.IsChecked = ColumnSettings.ShowDeadlyForceTraining;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            NameCheckBox.IsChecked = true;
            RateCheckBox.IsChecked = true;
            DutySection3CheckBox.IsChecked = true;
            DutySection6CheckBox.IsChecked = true;
            M9CheckBox.IsChecked = true;
            M4M16CheckBox.IsChecked = true;
            M500CheckBox.IsChecked = true;
            M240CheckBox.IsChecked = true;
            M2CheckBox.IsChecked = true;
            DateQualifiedCheckBox.IsChecked = true;
            LapsedQualificationsCheckBox.IsChecked = true;
            
            // Admin requirements
            AdminCheckBox.IsChecked = true;
            Form2760CheckBox.IsChecked = true;
            AAEScreeningCheckBox.IsChecked = true;
            DeadlyForceTrainingCheckBox.IsChecked = true;
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            NameCheckBox.IsChecked = false;
            RateCheckBox.IsChecked = false;
            DutySection3CheckBox.IsChecked = false;
            DutySection6CheckBox.IsChecked = false;
            M9CheckBox.IsChecked = false;
            M4M16CheckBox.IsChecked = false;
            M500CheckBox.IsChecked = false;
            M240CheckBox.IsChecked = false;
            M2CheckBox.IsChecked = false;
            DateQualifiedCheckBox.IsChecked = false;
            LapsedQualificationsCheckBox.IsChecked = false;
            
            // Admin requirements
            AdminCheckBox.IsChecked = false;
            Form2760CheckBox.IsChecked = false;
            AAEScreeningCheckBox.IsChecked = false;
            DeadlyForceTrainingCheckBox.IsChecked = false;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Update the settings with current checkbox values
            ColumnSettings.ShowName = NameCheckBox.IsChecked ?? true;
            ColumnSettings.ShowRate = RateCheckBox.IsChecked ?? true;
            ColumnSettings.ShowDutySection3 = DutySection3CheckBox.IsChecked ?? true;
            ColumnSettings.ShowDutySection6 = DutySection6CheckBox.IsChecked ?? true;
            ColumnSettings.ShowM9 = M9CheckBox.IsChecked ?? true;
            ColumnSettings.ShowM4M16 = M4M16CheckBox.IsChecked ?? true;
            ColumnSettings.ShowM500 = M500CheckBox.IsChecked ?? true;
            ColumnSettings.ShowM240 = M240CheckBox.IsChecked ?? true;
            ColumnSettings.ShowM2 = M2CheckBox.IsChecked ?? true;
            ColumnSettings.ShowDateQualified = DateQualifiedCheckBox.IsChecked ?? true;
            ColumnSettings.ShowLapsedQualifications = LapsedQualificationsCheckBox.IsChecked ?? true;
            
            // Admin requirements
            ColumnSettings.ShowAdmin = AdminCheckBox.IsChecked ?? true;
            ColumnSettings.ShowForm2760 = Form2760CheckBox.IsChecked ?? false;
            ColumnSettings.ShowAAEScreening = AAEScreeningCheckBox.IsChecked ?? false;
            ColumnSettings.ShowDeadlyForceTraining = DeadlyForceTrainingCheckBox.IsChecked ?? false;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 