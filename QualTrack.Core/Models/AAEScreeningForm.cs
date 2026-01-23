using System;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents an OPNAV Form 5530/1 (AA&E Screening Form)
    /// </summary>
    public class AAEScreeningForm
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        
        // Form completion data
        public DateTime DateCompleted { get; set; }
        public DateTime DateExpires { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        
        // Person being screened
        public string? NameScreened { get; set; }
        public string? RankScreened { get; set; }
        public string? DODIDScreened { get; set; }
        public string? SignatureScreened { get; set; }
        public DateTime? DateScreened { get; set; }
        
        // Screener/Qualifier information
        public string? NameScreener { get; set; }
        public string? RankScreener { get; set; }
        public string? DODIDScreener { get; set; }
        public string? SignatureScreener { get; set; }
        public DateTime? DateScreener { get; set; }
        
        // 7 Questions - each can be "Y", "N", or "na" (for question 3)
        public string? Question1Response { get; set; } // "Y", "N", "na"
        public string? Question2Response { get; set; }
        public string? Question3Response { get; set; } // Can be "Y", "N", or "na"
        public string? Question4Response { get; set; }
        public string? Question5Response { get; set; }
        public string? Question6Response { get; set; }
        public string? Question7Response { get; set; }
        
        // Remarks for each question
        public string? Remarks1 { get; set; }
        public string? Remarks2 { get; set; }
        public string? Remarks3 { get; set; }
        public string? Remarks4 { get; set; }
        public string? Remarks5 { get; set; }
        public string? Remarks6 { get; set; }
        public string? Remarks7 { get; set; }
        
        // Outcome
        public bool Qualified { get; set; }
        public bool Unqualified { get; set; }
        public bool ReviewLater { get; set; }
        public string? OtherQualifiedField { get; set; }
        
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
