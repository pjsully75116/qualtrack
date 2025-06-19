namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a weapon qualification for a personnel member
    /// </summary>
    public class Qualification
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        public string Weapon { get; set; } = string.Empty;
        public int Category { get; set; }
        public DateTime DateQualified { get; set; }
        public QualificationStatus? Status { get; set; }
        public QualificationDetails? Details { get; set; }

        public Qualification()
        {
        }

        public Qualification(string weapon, int category, DateTime dateQualified)
        {
            Weapon = weapon;
            Category = category;
            DateQualified = dateQualified;
        }
    }

    public class QualificationDetails
    {
        // Handgun (M9) fields
        public int NHQCScore { get; set; }
        public int HLLCScore { get; set; }
        public int HPWCScore { get; set; }
        public bool NHQCPass => NHQCScore >= 180 && NHQCScore <= 240;
        public bool HLLCPass => HLLCScore >= 12 && HLLCScore <= 18;
        public bool HPWCPass => HPWCScore >= 12 && HPWCScore <= 18;
        public bool OverallHandgunPass => NHQCPass && HLLCPass && HPWCPass;

        // Rifle (M4/M16) fields
        public int? RQCScore { get; set; } // Rifle Qualification Course
        public int? RLCScore { get; set; } // Rifle Lowlight Course
        public bool RQCPass => RQCScore.HasValue && RQCScore.Value >= 140 && RQCScore.Value <= 200;
        public bool RLCPass => RLCScore.HasValue && RLCScore.Value >= 14 && RLCScore.Value <= 20;
        public bool OverallRiflePass => RQCPass && RLCPass;

        public string Instructor { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }
} 