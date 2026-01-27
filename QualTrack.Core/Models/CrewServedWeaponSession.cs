using System;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a 3591/2 Machine Gun Performance Record session for crew served weapons (M240, M2)
    /// </summary>
    public class CrewServedWeaponSession
    {
        public int Id { get; set; }
        public string ShipStation { get; set; } = string.Empty;
        public string DivisionActivity { get; set; } = string.Empty;
        public string Weapon { get; set; } = string.Empty; // "M240" or "M2"
        public string RangeNameLocation { get; set; } = string.Empty;
        public DateTime? DateOfFiring { get; set; }
        
        // Crew served weapon specific fields
        public string? GunnerName { get; set; }
        public string? GunnerRankRate { get; set; }
        public string? GunnerDODID { get; set; }
        public string? AssistantGunnerName { get; set; }
        public string? AssistantGunnerRankRate { get; set; }
        public string? AssistantGunnerDODID { get; set; }
        public string? AmmunitionHandlerName { get; set; }
        public string? AmmunitionHandlerRankRate { get; set; }
        public string? AmmunitionHandlerDODID { get; set; }
        
        // Qualification scores
        public int? CourseOfFireScore { get; set; } // COF score (passing: ≥100)
        public bool IsQualified { get; set; }
        
        // Instructor/RSO information
        public string? InstructorName { get; set; }
        public string? InstructorRankRate { get; set; }
        public string? RsoSignature { get; set; }
        public string? RsoSignatureRate { get; set; }
        public DateTime? RsoSignatureDate { get; set; }
        
        // PDF storage
        public string? PdfFilePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Display properties for UI
        public string DateOfFiringDisplay => DateOfFiring?.ToString("yyyy-MM-dd") ?? "Unknown";
        public string SessionDisplay => $"{DateOfFiringDisplay} - {Weapon} - {ShipStation}";
        public string CategoryDisplay => Weapon == "M240" ? "CAT III" : Weapon == "M2" ? "CAT IV" : "Unknown";
    }
}
