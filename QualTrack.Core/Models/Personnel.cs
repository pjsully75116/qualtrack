namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a Navy personnel member
    /// </summary>
    public class Personnel
    {
        public int Id { get; set; }
        public string DODId { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public List<(string Type, string Section)> DutySections { get; set; } = new List<(string, string)>();
        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();

        /// <summary>
        /// The member's primary duty section (e.g., "1/3", "1/6").
        /// </summary>
        public string DutySection { get; set; } = string.Empty;

        /// <summary>
        /// The member's designator (e.g., officer designator code).
        /// </summary>
        public string Designator { get; set; } = string.Empty;

        /// <summary>
        /// The member's administrative status (e.g., "Up to date", "Overdue").
        /// </summary>
        public string AdminStatus { get; set; } = string.Empty;

        // Admin Requirements
        public AdditionalRequirements? AdminRequirements { get; set; }

        public Personnel()
        {
        }

        public Personnel(string lastName, string firstName, string rate, string rank)
        {
            LastName = lastName;
            FirstName = firstName;
            Rate = rate;
            Rank = rank;
        }
    }
} 