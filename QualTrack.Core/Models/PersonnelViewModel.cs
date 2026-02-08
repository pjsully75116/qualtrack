using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Windows.Media;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// View model for displaying personnel data in the UI
    /// </summary>
    public class PersonnelViewModel
    {
        public int Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public string DODId { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public ObservableCollection<(string Type, string Section)> DutySections { get; set; } = new ObservableCollection<(string, string)>();
        public ObservableCollection<Qualification> Qualifications { get; set; } = new ObservableCollection<Qualification>();

        // Admin Requirements
        public AdditionalRequirements? AdminRequirements { get; set; }

        // Display properties for UI binding
        public string NameDisplay => $"{LastName}, {FirstName}";
        public string DutySectionsDisplay => string.Join(", ", DutySections.Select(ds => $"{ds.Section}/{ds.Type}"));
        public string WeaponsDisplay => string.Join(", ", Qualifications.Select(q => q.Weapon));
        public string StatusDisplay => GetOverallStatus();
        public string MostRecentQualificationDateDisplay =>
            Qualifications.OrderByDescending(q => q.DateQualified).FirstOrDefault()?.DateQualified.ToString("yyyy-MM-dd") ?? string.Empty;
        public string LapsedQualificationsDisplay =>
            string.Join(", ", Qualifications.Where(IsQualificationLapsed).Select(q => q.Weapon));

        // Individual weapon qualification properties for dashboard columns
        public string M9Qualified => HasQualification("M9") ? "X" : "";
        public string M4M16Qualified => HasQualification("M4/M16") ? "X" : "";
        public string M500Qualified => HasQualification("M500") ? "X" : "";
        public string M240Qualified => HasQualification("M240") ? "X" : "";
        public string M2Qualified => HasQualification("M2") ? "X" : "";
        public string M2A1Qualified => HasQualification("M2A1") ? "X" : "";
        public string FiftyCalQualified => HasQualification("M2") || HasQualification("M2A1") ? "X" : "";
        public bool M9AdminBlocked => IsWeaponQualified("M9") && !IsAdminCurrent();
        public bool M4M16AdminBlocked => IsWeaponQualified("M4/M16") && !IsAdminCurrent();
        public bool M500AdminBlocked => IsWeaponQualified("M500") && !IsAdminCurrent();
        public bool M240AdminBlocked => IsWeaponQualified("M240") && !IsAdminCurrent();
        public bool M2AdminBlocked => IsWeaponQualified("M2") && !IsAdminCurrent();
        public bool M2A1AdminBlocked => IsWeaponQualified("M2A1") && !IsAdminCurrent();
        public bool FiftyCalAdminBlocked => (IsWeaponQualified("M2") || IsWeaponQualified("M2A1")) && !IsAdminCurrent();

        // Duty section properties for dashboard columns
        public string DutySection3Display => GetDutySectionDisplay("3");
        public string DutySection6Display => GetDutySectionDisplay("6");

        // Admin requirements properties
        public string AdminStatus => GetAdminStatus();
        public string Form2760Status => GetForm2760Status();
        public string AAEScreeningStatus => GetAAEScreeningStatus();
        public string DeadlyForceTrainingStatus => GetDeadlyForceTrainingStatus();

        // Admin color properties
        public Brush AdminColor => GetAdminColor();
        public Brush Form2760Color => GetForm2760Color();
        public Brush AAEScreeningColor => GetAAEScreeningColor();
        public Brush DeadlyForceTrainingColor => GetDeadlyForceTrainingColor();

        // Color properties for weapon qualification status
        public Brush M9Color => GetWeaponQualificationColor("M9");
        public Brush M4M16Color => GetWeaponQualificationColor("M4/M16");
        public Brush M500Color => GetWeaponQualificationColor("M500");
        public Brush M240Color => GetWeaponQualificationColor("M240");
        public Brush M2Color => GetWeaponQualificationColor("M2");
        public Brush M2A1Color => GetWeaponQualificationColor("M2A1");
        public Brush FiftyCalColor => GetWeaponQualificationColorForAny("M2A1", "M2");

        // DD2760 Form Status
        public string DD2760Status => GetDD2760Status();
        public Brush DD2760Color => GetDD2760Color();
        public DateTime? DD2760ExpirationDate { get; set; }

        // AA&E Screening Forms Status
        public List<AAEScreeningForm>? AAEScreeningForms { get; set; }
        public DateTime? AAEScreeningEarliestDate { get; set; }
        public DateTime? AAEScreeningExpirationDate { get; set; }

        public PersonnelViewModel()
        {
        }

        public PersonnelViewModel(Personnel personnel)
        {
            Id = personnel.Id;
            LastName = personnel.LastName;
            FirstName = personnel.FirstName;
            Rate = personnel.Rate;
            DODId = personnel.DODId;
            Rank = personnel.Rank;
            foreach (var dutySection in personnel.DutySections)
            {
                DutySections.Add(dutySection);
            }
            foreach (var qualification in personnel.Qualifications)
            {
                Qualifications.Add(qualification);
            }
            AdminRequirements = personnel.AdminRequirements;
        }

        /// <summary>
        /// Gets the overall qualification status for this personnel
        /// </summary>
        /// <returns>Status string for display</returns>
        private string GetOverallStatus()
        {
            if (!Qualifications.Any())
            {
                return "No Qualifications";
            }

            var disqualifiedCount = Qualifications.Count(q => q.Status?.IsDisqualified == true);
            var sustainmentCount = Qualifications.Count(q => q.Status?.SustainmentDue == true);
            var qualifiedCount = Qualifications.Count(q => q.Status?.IsQualified == true && !q.Status.SustainmentDue);

            if (disqualifiedCount > 0)
            {
                return $"Disqualified ({disqualifiedCount})";
            }
            else if (sustainmentCount > 0)
            {
                return $"Sustainment Due ({sustainmentCount})";
            }
            else if (qualifiedCount > 0)
            {
                return $"Qualified ({qualifiedCount})";
            }
            else
            {
                return "Unknown";
            }
        }

        private bool IsQualificationLapsed(Qualification q)
        {
            var date = q.DateQualified.Date;
            var details = q.Details;
            DateTime? sustainmentDate = details?.SustainmentDate;
            int sustainmentWindow = sustainmentDate.HasValue ? 365 : 240;
            var baseDate = date.AddDays(sustainmentWindow);
            // Lapse is first day of next month after baseDate
            var lapseDate = new DateTime(baseDate.Year, baseDate.Month, 1).AddMonths(1);
            // Only lapsed if today is on or after lapseDate
            return DateTime.Today >= lapseDate;
        }

        private bool HasQualification(string weapon)
        {
            return Qualifications.Any(q => q.Weapon == weapon && q.Status?.IsQualified == true);
        }

        private bool IsWeaponQualified(string weapon)
        {
            return Qualifications.Any(q => q.Weapon == weapon && q.Status?.IsQualified == true);
        }

        private bool IsAdminCurrent()
        {
            if (AdminRequirements == null)
            {
                return false;
            }

            return IsForm2760Valid() && IsAAEScreeningValid() && IsDeadlyForceTrainingValid();
        }

        private string GetDutySectionDisplay(string dutySectionType)
        {
            var sections = DutySections.Where(ds => ds.Type == dutySectionType).Select(ds => ds.Section).ToList();
            return sections.Any() ? string.Join(", ", sections) : "";
        }

        private Brush GetWeaponQualificationColor(string weapon)
        {
            var qualification = Qualifications.FirstOrDefault(q => q.Weapon == weapon);
            if (qualification == null || qualification.Status == null)
                return Brushes.Transparent;

            var status = qualification.Status;
            var today = DateTime.Today;

            // If disqualified, no color (transparent)
            if (status.IsDisqualified)
                return Brushes.Transparent;

            // If in sustainment window (120-240 days after qualification), YELLOW
            if (status.SustainmentDue)
                return Brushes.Yellow;

            // If qualification expires in 30 days or less, ORANGE
            if (status.DaysUntilExpiration <= 30)
                return Brushes.Orange;

            // Otherwise, GREEN (before sustainment window or already sustained)
            return Brushes.LightGreen;
        }

        private Brush GetWeaponQualificationColorForAny(params string[] weapons)
        {
            var qualification = Qualifications.FirstOrDefault(q => weapons.Contains(q.Weapon));
            if (qualification == null || qualification.Status == null)
                return Brushes.Transparent;

            var status = qualification.Status;
            var today = DateTime.Today;

            if (status.IsDisqualified)
                return Brushes.Transparent;

            if (status.SustainmentDue)
                return Brushes.Yellow;

            if (status.DaysUntilExpiration <= 30)
                return Brushes.Orange;

            return Brushes.LightGreen;
        }

        // DD2760 Form data
        public DD2760Form? DD2760Form { get; set; }

        private string GetDD2760Status()
        {
            // Only valid if form exists, response is "no", and not expired
            if (DD2760Form == null || DD2760Form.DomesticViolenceResponse?.ToLower() != "no")
                return "";

            if (DD2760ExpirationDate == null)
                return "";

            var today = DateTime.Today;
            var expirationDate = DD2760ExpirationDate.Value.Date;

            if (expirationDate < today)
                return "X";
            else if (expirationDate <= today.AddDays(30))
                return "X";
            else
                return "✓";
        }

        private Brush GetDD2760Color()
        {
            // Only valid if form exists and response is "no"
            if (DD2760Form == null || DD2760Form.DomesticViolenceResponse?.ToLower() != "no")
                return Brushes.Gray;

            if (DD2760ExpirationDate == null)
                return Brushes.Gray;

            var today = DateTime.Today;
            var expirationDate = DD2760ExpirationDate.Value.Date;

            if (expirationDate < today)
                return Brushes.Red;
            else if (expirationDate <= today.AddDays(30))
                return Brushes.Orange;
            else
                return Brushes.Green;
        }

        /// <summary>
        /// Converts back to a Personnel model
        /// </summary>
        /// <returns>Personnel object</returns>
        public Personnel ToPersonnel()
        {
            return new Personnel
            {
                Id = Id,
                LastName = LastName,
                FirstName = FirstName,
                Rate = Rate,
                DODId = DODId,
                Rank = Rank,
                DutySections = DutySections.ToList(),
                Qualifications = Qualifications.ToList(),
                AdminRequirements = AdminRequirements
            };
        }

        // Admin requirements methods
        private string GetAdminStatus()
        {
            if (AdminRequirements == null)
                return "X";

            var form2760Valid = IsForm2760Valid();
            var aaeScreeningValid = IsAAEScreeningValid();
            var deadlyForceValid = IsDeadlyForceTrainingValid();

            if (form2760Valid && aaeScreeningValid && deadlyForceValid)
                return "✓";
            else
                return "X";
        }

        private string GetForm2760Status()
        {
            if (AdminRequirements?.Form2760Date == null)
                return "X";

            return IsForm2760Valid() ? "✓" : "X";
        }

        private string GetAAEScreeningStatus()
        {
            // Use earliest date from all AA&E forms, not just AdminRequirements
            if (AAEScreeningEarliestDate == null)
                return "";

            if (AAEScreeningExpirationDate == null)
                return "";

            var today = DateTime.Today;
            var expirationDate = AAEScreeningExpirationDate.Value.Date;

            if (expirationDate < today)
                return "X";
            else if (expirationDate <= today.AddDays(30))
                return "X";
            else
                return "✓";
        }

        private string GetDeadlyForceTrainingStatus()
        {
            if (AdminRequirements?.DeadlyForceTrainingDate == null)
                return "X";

            return IsDeadlyForceTrainingValid() ? "✓" : "X";
        }

        private Brush GetAdminColor()
        {
            if (AdminRequirements == null)
                return Brushes.Red;

            var form2760Valid = IsForm2760Valid();
            var aaeScreeningValid = IsAAEScreeningValid();
            var deadlyForceValid = IsDeadlyForceTrainingValid();

            // If any are expired or missing, RED
            if (!form2760Valid || !aaeScreeningValid || !deadlyForceValid)
                return Brushes.Red;

            // If any are expiring soon (within 30 days), ORANGE
            if (IsForm2760ExpiringSoon() || IsAAEScreeningExpiringSoon() || IsDeadlyForceTrainingExpiringSoon())
                return Brushes.Orange;

            // Otherwise, all are valid and not expiring soon
            return Brushes.LightGreen;
        }

        private Brush GetForm2760Color()
        {
            if (AdminRequirements?.Form2760Date == null)
                return Brushes.Red;

            if (IsForm2760Valid())
            {
                if (IsForm2760ExpiringSoon())
                    return Brushes.Orange;
                else
                    return Brushes.LightGreen;
            }
            else
                return Brushes.Red;
        }

        private Brush GetAAEScreeningColor()
        {
            // Use earliest date from all AA&E forms, not just AdminRequirements
            if (AAEScreeningEarliestDate == null)
                return Brushes.Transparent;

            if (IsAAEScreeningValid())
            {
                if (IsAAEScreeningExpiringSoon())
                    return Brushes.Orange;
                else
                    return Brushes.LightGreen;
            }
            else
                return Brushes.Red; // Expired
        }

        private Brush GetDeadlyForceTrainingColor()
        {
            if (AdminRequirements?.DeadlyForceTrainingDate == null)
                return Brushes.Red;

            if (IsDeadlyForceTrainingValid())
            {
                if (IsDeadlyForceTrainingExpiringSoon())
                    return Brushes.Orange;
                else
                    return Brushes.LightGreen;
            }
            else
                return Brushes.Red;
        }

        private bool IsForm2760Valid()
        {
            if (AdminRequirements?.Form2760Date == null)
                return false;

            var completionDate = AdminRequirements.Form2760Date.Value;
            var expirationDate = completionDate.AddYears(1);
            return DateTime.Today <= expirationDate;
        }

        private bool IsForm2760ExpiringSoon()
        {
            if (AdminRequirements?.Form2760Date == null)
                return false;

            var completionDate = AdminRequirements.Form2760Date.Value;
            var expirationDate = completionDate.AddYears(1);
            var warningDate = expirationDate.AddDays(-30);
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }

        private bool IsAAEScreeningValid()
        {
            // Use earliest date from all AA&E forms
            if (AAEScreeningEarliestDate == null)
                return false;

            // Expiration is 1 year from the earliest recorded date
            var expirationDate = AAEScreeningEarliestDate.Value.AddYears(1);
            return DateTime.Today <= expirationDate;
        }

        private bool IsAAEScreeningExpiringSoon()
        {
            // Use earliest date from all AA&E forms
            if (AAEScreeningEarliestDate == null)
                return false;

            // Expiration is 1 year from the earliest recorded date
            var expirationDate = AAEScreeningEarliestDate.Value.AddYears(1);
            var warningDate = expirationDate.AddDays(-30);
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }

        private bool IsDeadlyForceTrainingValid()
        {
            if (AdminRequirements?.DeadlyForceTrainingDate == null)
                return false;

            var completionDate = AdminRequirements.DeadlyForceTrainingDate.Value;
            var expirationDate = completionDate.AddMonths(3); // Quarterly
            return DateTime.Today <= expirationDate;
        }

        private bool IsDeadlyForceTrainingExpiringSoon()
        {
            if (AdminRequirements?.DeadlyForceTrainingDate == null)
                return false;

            var completionDate = AdminRequirements.DeadlyForceTrainingDate.Value;
            var expirationDate = completionDate.AddMonths(3);
            var warningDate = expirationDate.AddDays(-30); // 30 day warning
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }
    }
} 