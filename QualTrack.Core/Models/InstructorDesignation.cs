using System;

namespace QualTrack.Core.Models
{
    public class InstructorDesignation
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime DesignationDate { get; set; }
        public string? PdfFilePath { get; set; }
        public string? PdfFileName { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateModified { get; set; }
    }
}
