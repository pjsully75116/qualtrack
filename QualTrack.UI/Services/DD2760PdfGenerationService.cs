using System;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using QualTrack.Core.Models;
using System.Collections.Generic;
using QualTrack.Core.Services;

namespace QualTrack.UI.Services
{
    public class DD2760PdfGenerationService
    {
        private readonly string _templatePath;
        private readonly string _outputDirectory;

        public DD2760PdfGenerationService(string? templatePath = null, string? outputDirectory = null)
        {
            _templatePath = templatePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dd2760_QualTrack.pdf");
            _outputDirectory = outputDirectory ?? StoragePathService.GetPendingDocsPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "DD2760_Forms"));
            
            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
        }

        /// <summary>
        /// Generates a filled DD2760 PDF form
        /// </summary>
        /// <param name="form">The DD2760 form data</param>
        /// <param name="personnel">The personnel data</param>
        /// <returns>Path to the generated PDF file</returns>
        public string GenerateDD2760Pdf(DD2760Form form, Personnel personnel)
        {
            try
            {
                // Create filename using personnel and date data
                var dateStr = form.DateCompleted.ToString("yyyyMMdd");
                var personnelName = $"{personnel.LastName}_{personnel.FirstName}".Replace(" ", "");
                var filename = $"DD2760_{personnelName}_{dateStr}.pdf";
                var outputPath = Path.Combine(_outputDirectory, filename);

                // Fill out the PDF with form data
                using var reader = new iTextSharp.text.pdf.PdfReader(_templatePath);
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var stamper = new iTextSharp.text.pdf.PdfStamper(reader, fs);
                var fields = stamper.AcroFields;

                // Debug: Log all available fields
                System.Diagnostics.Debug.WriteLine("=== DD2760 PDF Fields ===");
                if (fields != null && fields.Fields != null)
                {
                    foreach (var key in fields.Fields.Keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: '{key}'");
                    }
                    System.Diagnostics.Debug.WriteLine($"Total fields: {fields.Fields.Count}");
                }
                System.Diagnostics.Debug.WriteLine("=========================");

                // Fill form fields based on the mapping guide
                FillFormFields(fields, form, personnel);

                SetFieldsReadOnly(fields);
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating DD2760 PDF: {ex.Message}", ex);
            }
        }

        private void FillFormFields(AcroFields fields, DD2760Form form, Personnel personnel)
        {
            // Domestic violence question response
            if (!string.IsNullOrEmpty(form.DomesticViolenceResponse))
            {
                switch (form.DomesticViolenceResponse.ToLower())
                {
                    case "yes":
                        SetFieldValue(fields, "Yes_Initial", form.DomesticViolenceInitials ?? "");
                        SetFieldValue(fields, "Yes_YYYYMMDD", form.DomesticViolenceDate?.ToString("yyyyMMdd") ?? "");
                        break;
                    case "no":
                        SetFieldValue(fields, "No_Initial", form.DomesticViolenceInitials ?? "");
                        SetFieldValue(fields, "No_YYYYMMDD", form.DomesticViolenceDate?.ToString("yyyyMMdd") ?? "");
                        break;
                    case "dontknow":
                        SetFieldValue(fields, "Unk_Initial", form.DomesticViolenceInitials ?? "");
                        SetFieldValue(fields, "Unk_YYYYMMDD", form.DomesticViolenceDate?.ToString("yyyyMMdd") ?? "");
                        break;
                }
            }

            // Court information (if applicable)
            if (!string.IsNullOrEmpty(form.CourtJurisdiction))
            {
                SetFieldValue(fields, "a COURTJURISDICTION", form.CourtJurisdiction);
            }
            if (!string.IsNullOrEmpty(form.DocketCaseNumber))
            {
                SetFieldValue(fields, "b DOCKETCASE NUMBER", form.DocketCaseNumber);
            }
            if (!string.IsNullOrEmpty(form.StatuteCharge))
            {
                SetFieldValue(fields, "c STATUTECHARGE", form.StatuteCharge);
            }
            if (form.DateSentenced.HasValue)
            {
                SetFieldValue(fields, "d DATE SENTENCED YYYYMMDD", form.DateSentenced.Value.ToString("yyyyMMdd"));
            }

            // Certifier information
            // Name format: Last, First Middle Initial
            var certifierName = $"{personnel.LastName}, {personnel.FirstName}";
            if (!string.IsNullOrEmpty(form.CertifierName))
            {
                certifierName = form.CertifierName;
            }
            SetFieldValue(fields, "Name (Last, First, Middle Initial)", certifierName);
            
            // Rank/Grade
            var rankGrade = $"{personnel.Rank} {personnel.Rate}";
            if (!string.IsNullOrEmpty(form.CertifierRank))
            {
                rankGrade = form.CertifierRank;
            }
            SetFieldValue(fields, "Rank/Grade", rankGrade);
            
            // Organization
            if (!string.IsNullOrEmpty(form.CertifierOrganization))
            {
                SetFieldValue(fields, "Organization", form.CertifierOrganization);
            }
            
            // Signature (using certifier name or personnel name)
            SetFieldValue(fields, "Signature", certifierName);
            
            // SSN
            if (!string.IsNullOrEmpty(form.CertifierSSN))
            {
                SetFieldValue(fields, "SSN", form.CertifierSSN);
            }
            
            // Date Signed
            if (form.CertifierSignatureDate.HasValue)
            {
                SetFieldValue(fields, "Signature_YYYYMMDD", form.CertifierSignatureDate.Value.ToString("yyyyMMdd"));
            }
            
            System.Diagnostics.Debug.WriteLine("=== Finished filling DD2760 form fields ===");
        }

        private void SetFieldValue(AcroFields form, string fieldName, string value)
        {
            try
            {
                if (form == null || form.Fields == null || string.IsNullOrWhiteSpace(fieldName))
                {
                    return;
                }

                // Try exact match first
                if (form.Fields.ContainsKey(fieldName))
                {
                    form.SetField(fieldName, value ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"✓ Set field '{fieldName}' = '{value}'");
                    return;
                }

                // Try normalized matching (remove spaces, case-insensitive)
                var normalizedTarget = NormalizeFieldName(fieldName);
                if (string.IsNullOrEmpty(normalizedTarget))
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Could not normalize field name: '{fieldName}'");
                    return;
                }

                var matchedField = form.Fields.Keys.FirstOrDefault(key =>
                    NormalizeFieldName(key) == normalizedTarget);

                if (!string.IsNullOrEmpty(matchedField))
                {
                    form.SetField(matchedField, value ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"✓ Set field '{matchedField}' (matched from '{fieldName}') = '{value}'");
                    return;
                }

                // Field not found - log for debugging
                System.Diagnostics.Debug.WriteLine($"✗ Field not found: '{fieldName}' (normalized: '{normalizedTarget}')");
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire process
                System.Diagnostics.Debug.WriteLine($"Error setting DD2760 field {fieldName}: {ex.Message}");
            }
        }

        private static string NormalizeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            // Remove all non-alphanumeric characters and convert to uppercase for comparison
            var cleaned = fieldName.Trim()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("{", "")
                .Replace("}", "");

            var chars = cleaned.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToUpperInvariant();
        }

        private static void SetFieldsReadOnly(AcroFields fields)
        {
            if (fields?.Fields == null)
            {
                return;
            }

            foreach (var key in fields.Fields.Keys)
            {
                try
                {
                    fields.SetFieldProperty(key, "setfflags", PdfFormField.FF_READ_ONLY, null);
                }
                catch
                {
                    // Best-effort read-only; ignore individual failures.
                }
            }
        }

        /// <summary>
        /// Gets all field names from the DD2760 PDF template for debugging
        /// </summary>
        /// <returns>List of field names in the PDF</returns>
        public List<string> GetPdfFieldNames()
        {
            var fieldNames = new List<string>();
            
            try
            {
                using var reader = new iTextSharp.text.pdf.PdfReader(_templatePath);
                var form = reader.AcroForm;
                if (form != null && form.Fields != null)
                {
                    foreach (var field in form.Fields)
                    {
                        fieldNames.Add(field.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading DD2760 PDF field names: {ex.Message}", ex);
            }

            return fieldNames;
        }
    }
}
