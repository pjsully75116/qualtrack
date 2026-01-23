using System;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a DD2760 Ammunition and Explosives Safety Qualification form
    /// </summary>
    public class DD2760Form
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        
        // Form completion data
        public DateTime DateCompleted { get; set; }
        public DateTime DateExpires { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        
        // Form field data
        public string? DomesticViolenceResponse { get; set; } // "yes", "no", "dontknow"
        public string? DomesticViolenceInitials { get; set; }
        public DateTime? DomesticViolenceDate { get; set; }
        
        // Court information (if applicable)
        public string? CourtJurisdiction { get; set; }
        public string? DocketCaseNumber { get; set; }
        public string? StatuteCharge { get; set; }
        public DateTime? DateSentenced { get; set; }
        
        // Certifier information
        public string? CertifierName { get; set; }
        public string? CertifierRank { get; set; }
        public string? CertifierSSN { get; set; }
        public string? CertifierOrganization { get; set; }
        public bool IsCertified { get; set; }
        public DateTime? CertifierSignatureDate { get; set; }
        
        // PDF storage
        public string? PdfFilePath { get; set; }
        public string? PdfFileName { get; set; }
        
        // Status tracking
        public bool IsValid { get; set; } = true;
        public string? StatusNotes { get; set; }
        
        // Navigation properties
        public Personnel? Personnel { get; set; }
    }
}
