using System.Collections.ObjectModel;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// View model for displaying personnel data in the UI
    /// </summary>
    public class PersonnelViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public ObservableCollection<(string Type, string Section)> DutySections { get; set; } = new ObservableCollection<(string, string)>();
        public ObservableCollection<Qualification> Qualifications { get; set; } = new ObservableCollection<Qualification>();

        // Display properties for UI binding
        public string DutySectionsDisplay => string.Join(", ", DutySections.Select(ds => $"{ds.Type}-{ds.Section}"));
        public string WeaponsDisplay => string.Join(", ", Qualifications.Select(q => q.Weapon));
        public string StatusDisplay => GetOverallStatus();

        public PersonnelViewModel()
        {
        }

        public PersonnelViewModel(Personnel personnel)
        {
            Id = personnel.Id;
            Name = personnel.Name;
            Rate = personnel.Rate;
            foreach (var dutySection in personnel.DutySections)
            {
                DutySections.Add(dutySection);
            }
            foreach (var qualification in personnel.Qualifications)
            {
                Qualifications.Add(qualification);
            }
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

        /// <summary>
        /// Converts back to a Personnel model
        /// </summary>
        /// <returns>Personnel object</returns>
        public Personnel ToPersonnel()
        {
            return new Personnel
            {
                Id = Id,
                Name = Name,
                Rate = Rate,
                DutySections = DutySections.ToList(),
                Qualifications = Qualifications.ToList()
            };
        }
    }
} 