using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    /// <summary>
    /// Service for managing OCR processing workflow for 3591/1 forms
    /// </summary>
    public class OcrProcessingService
    {
        private readonly DocumentService _documentService;
        private readonly object _documentRepository; // Will be properly typed when Data project is referenced

        public OcrProcessingService(DocumentService documentService, object documentRepository)
        {
            _documentService = documentService;
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Processes OCR on a document and saves the extractions
        /// </summary>
        /// <param name="documentId">ID of the document to process</param>
        /// <returns>Processing result</returns>
        public async Task<OcrProcessingResult> ProcessDocumentAsync(int documentId)
        {
            try
            {
                // Create a mock document for now since we don't have proper repository integration
                // In a full implementation, you'd get the document from the repository
                var document = new Document
                {
                    Id = documentId,
                    DocumentType = "3591_1",
                    OriginalFilename = "test.pdf",
                    FilePath = "", // This would be the actual file path from the repository
                    FileSize = 0,
                    UploadDate = DateTime.Now,
                    OcrProcessed = false,
                    DateCreated = DateTime.Now
                };

                // Perform real OCR processing using DocumentService
                var extractions = await _documentService.ProcessOcrAsync(document);

                if (extractions.Any())
                {
                    return new OcrProcessingResult
                    {
                        Success = true,
                        Extractions = extractions,
                        AverageConfidence = extractions.Average(e => e.Confidence ?? 0)
                    };
                }
                else
                {
                    return new OcrProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "No text was extracted from the document"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OcrProcessingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Validates extracted data against expected 3591/1 form fields
        /// </summary>
        /// <param name="extractions">OCR extractions to validate</param>
        /// <returns>Validation results</returns>
        public ValidationResult ValidateExtractions(List<OcrExtraction> extractions)
        {
            var result = new ValidationResult();
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check for required fields
            var requiredFields = new[] { "LastName", "FirstName", "DODId", "Weapon", "Category", "DateQualified" };
            var missingFields = requiredFields.Where(field => 
                !extractions.Any(e => e.FieldName == field && !string.IsNullOrWhiteSpace(e.ExtractedValue)));

            foreach (var field in missingFields)
            {
                errors.Add($"Missing required field: {field}");
            }

            // Validate DOD ID format (should be numeric)
            var dodIdExtraction = extractions.FirstOrDefault(e => e.FieldName == "DODId");
            if (dodIdExtraction != null && !string.IsNullOrWhiteSpace(dodIdExtraction.ExtractedValue))
            {
                if (!dodIdExtraction.ExtractedValue.All(char.IsDigit))
                {
                    warnings.Add("DOD ID should contain only numbers");
                }
            }

            // Validate date format
            var dateExtraction = extractions.FirstOrDefault(e => e.FieldName == "DateQualified");
            if (dateExtraction != null && !string.IsNullOrWhiteSpace(dateExtraction.ExtractedValue))
            {
                if (!DateTime.TryParse(dateExtraction.ExtractedValue, out _))
                {
                    warnings.Add("Date Qualified should be in a valid date format");
                }
            }

            // Check confidence levels
            var lowConfidenceExtractions = extractions.Where(e => e.Confidence < 0.7).ToList();
            foreach (var extraction in lowConfidenceExtractions)
            {
                warnings.Add($"Low confidence ({extraction.Confidence:P0}) for field: {extraction.FieldName}");
            }

            result.IsValid = !errors.Any();
            result.Errors = errors;
            result.Warnings = warnings;

            return result;
        }

        /// <summary>
        /// Gets documents pending OCR review
        /// </summary>
        /// <returns>List of documents needing review</returns>
        public List<Document> GetDocumentsPendingReview()
        {
            // Placeholder implementation
            return new List<Document>();
        }

        /// <summary>
        /// Gets OCR extractions for a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>List of OCR extractions</returns>
        public List<OcrExtraction> GetExtractionsForDocument(int documentId)
        {
            // Placeholder implementation
            return new List<OcrExtraction>();
        }

        /// <summary>
        /// Approves an OCR extraction
        /// </summary>
        /// <param name="extractionId">Extraction ID to approve</param>
        /// <param name="correctedValue">Optional corrected value</param>
        /// <returns>Success status</returns>
        public bool ApproveExtraction(int extractionId, string? correctedValue = null)
        {
            // Placeholder implementation
            return true;
        }

        /// <summary>
        /// Rejects an OCR extraction
        /// </summary>
        /// <param name="extractionId">Extraction ID to reject</param>
        /// <param name="correctedValue">Corrected value</param>
        /// <returns>Success status</returns>
        public bool RejectExtraction(int extractionId, string correctedValue)
        {
            // Placeholder implementation
            return true;
        }
    }

    /// <summary>
    /// Result of OCR processing
    /// </summary>
    public class OcrProcessingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<OcrExtraction>? Extractions { get; set; }
        public double AverageConfidence { get; set; }
    }

    /// <summary>
    /// Result of validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 