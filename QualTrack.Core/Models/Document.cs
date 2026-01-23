using System;

namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents a document uploaded to the system
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public int? PersonnelId { get; set; }
        public string DocumentType { get; set; } = string.Empty; // '3591', '2760', 'AAE', 'DEADLY_FORCE'
        public string OriginalFilename { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public bool OcrProcessed { get; set; }
        public double? OcrConfidence { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Navigation properties
        public Personnel? Personnel { get; set; }
        public List<OcrExtraction> OcrExtractions { get; set; } = new List<OcrExtraction>();
    }

    /// <summary>
    /// Represents OCR extraction results for a document
    /// </summary>
    public class OcrExtraction
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string FieldName { get; set; } = string.Empty; // 'weapon', 'date_qualified', 'instructor', etc.
        public string? ExtractedValue { get; set; }
        public double? Confidence { get; set; }
        public string? BoundingBox { get; set; } // JSON coordinates
        public bool Reviewed { get; set; }
        public bool Approved { get; set; }
        public string? CorrectedValue { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Navigation property
        public Document? Document { get; set; }
    }

    /// <summary>
    /// Document types supported by the system
    /// </summary>
    public static class DocumentTypes
    {
        public const string Form3591 = "3591";
        public const string Form2760 = "2760";
        public const string AAEScreening = "AAE";
        public const string DeadlyForceTraining = "DEADLY_FORCE";

        public static readonly string[] AllTypes = { Form3591, Form2760, AAEScreening, DeadlyForceTraining };

        public static string GetDisplayName(string documentType)
        {
            return documentType switch
            {
                Form3591 => "3591/1 Form",
                Form2760 => "Form 2760 (Lautenberg Act)",
                AAEScreening => "AA&E Screening Form",
                DeadlyForceTraining => "Deadly Force Training Record",
                _ => documentType
            };
        }
    }
} 