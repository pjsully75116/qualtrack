using System;

namespace QualTrack.Core.Models
{
    public class QualificationSession
    {
        public int Id { get; set; }
        public string ShipStation { get; set; } = string.Empty;
        public string DivisionActivity { get; set; } = string.Empty;
        public string WeaponsFired { get; set; } = string.Empty;
        public string RangeNameLocation { get; set; } = string.Empty;
        public DateTime? DateOfFiring { get; set; }
        public string? RsoSignature { get; set; }
        public string? RsoSignatureRate { get; set; }
        public DateTime? RsoSignatureDate { get; set; }
        public string? PdfFilePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Display properties for UI
        public string DateOfFiringDisplay => DateOfFiring?.ToString("yyyy-MM-dd") ?? "Unknown";
        public string SessionDisplay => $"{DateOfFiringDisplay} - {ShipStation} - {WeaponsFired}";
    }
} 