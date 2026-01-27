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
        public int? QualificationSessionId { get; set; } // Link to 3591/1 session
        public int? CrewServedWeaponSessionId { get; set; } // Link to 3591/2 session (for M240, M2)
        public QualificationStatus? Status { get; set; }
        public QualificationDetails? Details { get; set; }

        // Display properties for UI
        public string CategoryDisplay => $"CAT {Category}";
        public string DateQualifiedDisplay => DateQualified.ToString("yyyy-MM-dd");
        public string StatusDisplay => Status?.IsQualified == true ? (Status.SustainmentDue ? "Sustainment Due" : "Qualified") : "Disqualified";
        public string ExpirationDisplay => Status != null ? Status.ExpiresOn.ToString("yyyy-MM-dd") : "Unknown";

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
        // CAT I (M9) fields
        public int? HQCScore { get; set; } // Handgun Qualification Course (CAT I only, or also present in CAT II as NHQC)
        public bool HQCPass => HQCScore.HasValue && HQCScore.Value >= 180 && HQCScore.Value <= 240;

        // Handgun (M9) CAT II fields
        public int? NHQCScore { get; set; } // For CAT II, this is the same as HQCScore
        public int? HLLCScore { get; set; }
        public int? HPWCScore { get; set; }
        public bool NHQCPass => NHQCScore.HasValue && NHQCScore.Value >= 180 && NHQCScore.Value <= 240;
        public bool HLLCPass => HLLCScore.HasValue && HLLCScore.Value >= 12 && HLLCScore.Value <= 18;
        public bool HPWCPass => HPWCScore.HasValue && HPWCScore.Value >= 12 && HPWCScore.Value <= 18;
        public bool OverallHandgunPass => NHQCPass && HLLCPass && HPWCPass;
        // Note: In future, logic can infer CAT I qualification from a passing CAT II (NHQCScore)

        // Rifle (M4/M16) fields
        public int? RQCScore { get; set; } // Rifle Qualification Course
        public int? RLCScore { get; set; } // Rifle Lowlight Course
        public bool RQCPass => RQCScore.HasValue && RQCScore.Value >= 140 && RQCScore.Value <= 200;
        public bool RLCPass => RLCScore.HasValue && RLCScore.Value >= 14 && RLCScore.Value <= 20;
        public bool OverallRiflePass => RQCPass && RLCPass;

        // Shotgun (M500) fields
        public int? SPWCScore { get; set; } // Shotgun Practical Weapons Course
        public bool SPWCPass => SPWCScore.HasValue && SPWCScore.Value >= 90 && SPWCScore.Value <= 162;

        // Machine Gun (M240, M2) CAT II
        public int? COFScore { get; set; } // Course of Fire (future review: verify technical accuracy)
        public bool COFPass => COFScore.HasValue && COFScore.Value >= 100;
        public string CSWI { get; set; } = string.Empty; // For M240/M2 only (future: restrict to qualified personnel)

        public string Instructor { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public bool QualifiedUnderway { get; set; } = false; // For CAT II M9/M16, omits COFs if true

        public DateTime? SustainmentDate { get; set; }
        public int? SustainmentScore { get; set; }
    }
} 