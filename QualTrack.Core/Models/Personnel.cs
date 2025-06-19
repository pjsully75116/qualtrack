namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a Navy personnel member
    /// </summary>
    public class Personnel
    {
        public int Id { get; set; }
        public string DODId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public List<(string Type, string Section)> DutySections { get; set; } = new List<(string, string)>();
        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();

        public Personnel()
        {
        }

        public Personnel(string name, string rate)
        {
            Name = name;
            Rate = rate;
        }
    }
} 