namespace QualTrack.Core.Models
{
    /// <summary>
    /// Settings for dashboard column visibility
    /// </summary>
    public class DashboardColumnSettings
    {
        // Basic Information
        public bool ShowName { get; set; } = true;
        public bool ShowRate { get; set; } = true;
        public bool ShowDutySections { get; set; } = true;

        // Weapon Qualifications
        public bool ShowM9 { get; set; } = true;
        public bool ShowM4M16 { get; set; } = true;
        public bool ShowM500 { get; set; } = true;
        public bool ShowM240 { get; set; } = true;
        public bool ShowM2 { get; set; } = true;

        // Duty Section Columns
        public bool ShowDutySection3 { get; set; } = true;
        public bool ShowDutySection6 { get; set; } = true;

        // Status Information
        public bool ShowDateQualified { get; set; } = false;
        public bool ShowLapsedQualifications { get; set; } = false;

        // Admin Requirements
        public bool ShowAdmin { get; set; } = true;
        public bool ShowForm2760 { get; set; } = true;
        public bool ShowAAEScreening { get; set; } = true;
        public bool ShowDeadlyForceTraining { get; set; } = true;

        /// <summary>
        /// Creates a deep copy of the current settings
        /// </summary>
        /// <returns>A new instance with the same settings</returns>
        public DashboardColumnSettings Clone()
        {
            return new DashboardColumnSettings
            {
                ShowName = this.ShowName,
                ShowRate = this.ShowRate,
                ShowDutySections = this.ShowDutySections,
                ShowM9 = this.ShowM9,
                ShowM4M16 = this.ShowM4M16,
                ShowM500 = this.ShowM500,
                ShowM240 = this.ShowM240,
                ShowM2 = this.ShowM2,
                ShowDutySection3 = this.ShowDutySection3,
                ShowDutySection6 = this.ShowDutySection6,
                ShowDateQualified = this.ShowDateQualified,
                ShowLapsedQualifications = this.ShowLapsedQualifications,
                ShowAdmin = this.ShowAdmin,
                ShowForm2760 = this.ShowForm2760,
                ShowAAEScreening = this.ShowAAEScreening,
                ShowDeadlyForceTraining = this.ShowDeadlyForceTraining
            };
        }
    }
} 