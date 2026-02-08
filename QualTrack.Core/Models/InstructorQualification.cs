using System;

namespace QualTrack.Core.Models
{
    public class InstructorQualification
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime QualificationDate { get; set; }
        public string QualificationType { get; set; } = string.Empty;
        public string? PdfFilePath { get; set; }
        public string? PdfFileName { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
