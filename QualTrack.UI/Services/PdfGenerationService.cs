using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using QualTrack.Core.Models;
using QualTrack.UI;

namespace QualTrack.UI.Services
{
    public class PdfGenerationService
    {
        private readonly string _templatePath;
        private readonly string _outputDirectory;

        public PdfGenerationService(string? templatePath = null, string? outputDirectory = null)
        {
            _templatePath = ResolveTemplatePath(templatePath);
            _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "3591_1_Forms");
            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
        }

        /// <summary>
        /// Generates a 3591/1 PDF for a qualification session with multiple sailors
        /// </summary>
        /// <param name="session">The qualification session data</param>
        /// <param name="sailorQualifications">List of sailor qualifications</param>
        /// <returns>Path to the generated PDF file</returns>
        public string Generate3591Pdf(QualificationSession session, List<SailorQualification> sailorQualifications)
        {
            try
            {
                // Create filename using session data
                var dateStr = session.DateOfFiring?.ToString("ddMMM") ?? DateTime.Now.ToString("ddMMM");
                var shipName = string.IsNullOrWhiteSpace(session.ShipStation) ? "Unknown" : session.ShipStation.Replace(" ", "");
                var filename = $"{dateStr}_{shipName}.pdf";
                var outputPath = Path.Combine(_outputDirectory, filename);

                // Fill out the PDF with session data and sailor data in one pass
                using (var reader = new iTextSharp.text.pdf.PdfReader(_templatePath))
                using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                using (var stamper = new iTextSharp.text.pdf.PdfStamper(reader, fs))
                {
                    var form = stamper.AcroFields;
                    // Fill session-level data
                    SetFieldValue(form, session.ShipStation ?? "", "SHIP OR STATION");
                    SetFieldValue(form, session.DivisionActivity ?? "", "DIVISION OR ACTIVITY");
                    SetFieldValue(form, session.WeaponsFired ?? "", "WEAPON (S) FIRED");
                    SetFieldValue(form, session.RangeNameLocation ?? "", "RANGE NAME/LOCATION");
                    if (session.DateOfFiring.HasValue)
                    {
                        SetFieldValue(form, session.DateOfFiring.Value.ToString("MM/dd/yyyy"), "DATE OF FIRING");
                    }

                    // Fill out sailor data (for now, just the first one)
                    if (sailorQualifications.Any())
                    {
                        var sailor = sailorQualifications.First();
                        int rowNumber = 1;
                        SetFieldValue(form, $"Name_{rowNumber}", sailor.FullName ?? "");
                        SetFieldValue(form, $"DODID_{rowNumber}", sailor.DodId ?? "");
                        SetFieldValue(form, $"Rank_{rowNumber}", sailor.RankRate ?? "");
                        if (!string.IsNullOrWhiteSpace(sailor.NHQC?.ToString()))
                            SetFieldValue(form, $"NHQC_{rowNumber}", sailor.NHQC.ToString());
                        if (!string.IsNullOrWhiteSpace(sailor.HLLC?.ToString()))
                            SetFieldValue(form, $"HLLC_{rowNumber}", sailor.HLLC.ToString());
                        if (!string.IsNullOrWhiteSpace(sailor.HPWCT1?.ToString()))
                            SetFieldValue(form, $"HPWC_T1_{rowNumber}", sailor.HPWCT1.ToString());
                        if (!string.IsNullOrWhiteSpace(sailor.HPWCT2?.ToString()))
                            SetFieldValue(form, $"HPWC_T2_{rowNumber}", sailor.HPWCT2.ToString());
                        if (!string.IsNullOrWhiteSpace(sailor.HPWCT3?.ToString()))
                            SetFieldValue(form, $"HPWC_T3_{rowNumber}", sailor.HPWCT3.ToString());
                        if (!string.IsNullOrWhiteSpace(sailor.HPWCQualified))
                            SetFieldValue(form, $"HPWC_QU_{rowNumber}", sailor.HPWCQualified ?? "");
                    }
                    stamper.FormFlattening = true;
                }
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating 3591/1 PDF: {ex.Message}", ex);
            }
        }

        private void SetFieldValue(AcroFields form, string value, params string[] fieldNames)
        {
            try
            {
                if (form == null || form.Fields == null || fieldNames == null)
                {
                    return;
                }

                foreach (var fieldName in fieldNames)
                {
                    if (string.IsNullOrWhiteSpace(fieldName))
                    {
                        continue;
                    }

                    if (form.Fields.ContainsKey(fieldName))
                    {
                        // Use explicit method signature to avoid overload ambiguity
                        form.SetField(fieldName, value ?? string.Empty);
                        return;
                    }

                    var normalizedTarget = NormalizeFieldName(fieldName);
                    if (string.IsNullOrEmpty(normalizedTarget))
                    {
                        continue;
                    }

                    var matchedField = form.Fields.Keys.FirstOrDefault(key =>
                        NormalizeFieldName(key) == normalizedTarget);

                    if (!string.IsNullOrEmpty(matchedField))
                    {
                        form.SetField(matchedField, value ?? string.Empty);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire process
                var fieldList = string.Join(", ", fieldNames);
                System.Diagnostics.Debug.WriteLine($"Error setting field(s) {fieldList}: {ex.Message}");
            }
        }

        private static string NormalizeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            var cleaned = fieldName.Trim()
                .Replace("{", string.Empty)
                .Replace("}", string.Empty);

            var chars = cleaned.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToUpperInvariant();
        }

        private static string ResolveTemplatePath(string? templatePath)
        {
            if (!string.IsNullOrWhiteSpace(templatePath))
            {
                return templatePath;
            }

            // Default template path
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3591_1QualTrack.pdf");
        }

        /// <summary>
        /// Gets all field names from the PDF template for debugging
        /// </summary>
        /// <returns>List of field names in the PDF</returns>
        public List<string> GetPdfFieldNames()
        {
            var fieldNames = new List<string>();
            
            try
            {
                using (var reader = new iTextSharp.text.pdf.PdfReader(_templatePath))
                {
                    var form = reader.AcroFields;
                    if (form != null && form.Fields != null)
                    {
                        foreach (var key in form.Fields.Keys)
                        {
                            fieldNames.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading PDF field names: {ex.Message}", ex);
            }

            return fieldNames;
        }
    }
} 