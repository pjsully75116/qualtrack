using QualTrack.Core.Models;
using System.Windows;

namespace QualTrack.UI
{
    public partial class DODIDConfirmationWindow : Window
    {
        public enum UserChoice
        {
            Confirm,
            Skip,
            Cancel
        }

        public UserChoice Result { get; private set; } = UserChoice.Cancel;
        public bool IsSustainment { get; private set; } = false;

        private readonly Personnel _existingPersonnel;
        private readonly SailorQualification _newData;
        private readonly string _weapon;
        private readonly DateTime _qualDate;

        public DODIDConfirmationWindow(Personnel existingPersonnel, SailorQualification newData, string weapon, DateTime qualDate)
        {
            InitializeComponent();
            _existingPersonnel = existingPersonnel;
            _newData = newData;
            _weapon = weapon;
            _qualDate = qualDate;
            
            PopulateData();
        }

        private void PopulateData()
        {
            // Existing sailor data
            ExistingNameText.Text = $"{_existingPersonnel.LastName}, {_existingPersonnel.FirstName}";
            ExistingDODIDText.Text = _existingPersonnel.DODId;
            ExistingRateRankText.Text = $"{_existingPersonnel.Rate}/{_existingPersonnel.Rank}";
            
            // Duty sections
            var dutySections = string.Join(", ", _existingPersonnel.DutySections.Select(ds => $"{ds.Item1}-{ds.Item2}"));
            ExistingDutySectionsText.Text = string.IsNullOrEmpty(dutySections) ? "None" : dutySections;
            
            // Current qualifications
            var qualifications = _existingPersonnel.Qualifications?.Select(q => 
                $"{q.Weapon} (CAT {q.Category}) - {q.DateQualified:MM/dd/yyyy}") ?? new List<string>();
            ExistingQualificationsText.Text = qualifications.Any() ? string.Join("\n", qualifications) : "None";

            // New data
            NewNameText.Text = _newData.FullName;
            NewDODIDText.Text = _newData.DodId;
            NewRateRankText.Text = _newData.RankRate;
            NewWeaponText.Text = _weapon;
            NewQualDateText.Text = _qualDate.ToString("MM/dd/yyyy");

            // Build scores text
            var scores = new List<string>();
            if (_newData.NHQC.HasValue) scores.Add($"NHQC: {_newData.NHQC}");
            if (_newData.RQC.HasValue) scores.Add($"RQC: {_newData.RQC}");
            if (_newData.SPWCT1.HasValue) scores.Add($"SPWC T1: {_newData.SPWCT1}");
            if (_newData.SPWCT2.HasValue) scores.Add($"SPWC T2: {_newData.SPWCT2}");
            if (_newData.SPWCT3.HasValue) scores.Add($"SPWC T3: {_newData.SPWCT3}");
            if (_newData.HPWCT1.HasValue) scores.Add($"HPWC T1: {_newData.HPWCT1}");
            if (_newData.HPWCT2.HasValue) scores.Add($"HPWC T2: {_newData.HPWCT2}");
            if (_newData.HPWCT3.HasValue) scores.Add($"HPWC T3: {_newData.HPWCT3}");
            if (_newData.HLLC.HasValue) scores.Add($"HLLC: {_newData.HLLC}");
            if (_newData.RLLC.HasValue) scores.Add($"RLLC: {_newData.RLLC}");
            
            NewScoresText.Text = scores.Any() ? string.Join("\n", scores) : "No scores provided";

            // Check if this is a sustainment qualification
            var existingQual = _existingPersonnel.Qualifications?.FirstOrDefault(q => q.Weapon == _weapon);
            if (existingQual != null)
            {
                IsSustainment = _qualDate > existingQual.DateQualified;
                if (IsSustainment)
                {
                    StatusText.Text = "This appears to be a sustainment qualification (newer date than existing qualification).";
                    ConfirmButton.Content = "Confirm Match & Add Sustainment";
                }
                else
                {
                    StatusText.Text = "This qualification date is older than the existing qualification. It will not be added.";
                    ConfirmButton.IsEnabled = false;
                }
            }
            else
            {
                // No existing qualification for this weapon - this is a new qualification
                StatusText.Text = "This is a new qualification for this weapon system.";
                ConfirmButton.Content = "Confirm Match & Add Qualification";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UserChoice.Confirm;
            DialogResult = true;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UserChoice.Skip;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UserChoice.Cancel;
            DialogResult = false;
            Close();
        }
    }
} 