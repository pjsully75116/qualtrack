using System;
using System.IO;
using iTextSharp.text.pdf;
using QualTrack.Core.Models;

namespace QualTrack.UI.Services
{
    public class AAEPdfGenerationService
    {
        private readonly string _templatePath;
        private readonly string _outputDirectory;

        public AAEPdfGenerationService(string? templatePath = null, string? outputDirectory = null)
        {
            _templatePath = templatePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "5530_1 QualTrack.pdf");
            _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "AAE_Forms");
            
            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
        }

        /// <summary>
        /// Generates a filled AA&E Screening PDF form
        /// </summary>
        /// <param name="form">The AA&E Screening form data</param>
        /// <param name="personnel">The personnel data (person being screened)</param>
        /// <param name="screener">The screener personnel data (optional, can be null)</param>
        /// <returns>Path to the generated PDF file</returns>
        public string GenerateAAEPdf(AAEScreeningForm form, Personnel personnel, Personnel? screener = null)
        {
            try
            {
                // Create filename using personnel and date data
                var dateStr = form.DateCompleted.ToString("yyyyMMdd");
                var personnelName = $"{personnel.LastName}_{personnel.FirstName}".Replace(" ", "");
                var filename = $"AAE_{personnelName}_{dateStr}.pdf";
                var outputPath = Path.Combine(_outputDirectory, filename);

                // Fill out the PDF with form data
                using var reader = new iTextSharp.text.pdf.PdfReader(_templatePath);
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var stamper = new iTextSharp.text.pdf.PdfStamper(reader, fs);
                var fields = stamper.AcroFields;

                // Debug: Log all available fields
                System.Diagnostics.Debug.WriteLine("=== AA&E PDF Fields ===");
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
                FillFormFields(fields, form, personnel, screener);

                stamper.FormFlattening = true;
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating AA&E PDF: {ex.Message}", ex);
            }
        }

        private void FillFormFields(AcroFields fields, AAEScreeningForm form, Personnel personnel, Personnel? screener)
        {
            // Person being screened
            var nameScreened = form.NameScreened ?? $"{personnel.LastName}, {personnel.FirstName}";
            SetFieldValue(fields, "Name_Screened", nameScreened);
            SetFieldValue(fields, "Rank_Screened", form.RankScreened ?? $"{personnel.Rank} {personnel.Rate}");
            SetFieldValue(fields, "DODID_Screened", form.DODIDScreened ?? personnel.DODId ?? "");

            // Screener/Qualifier
            if (screener != null)
            {
                var nameScreener = form.NameScreener ?? $"{screener.LastName}, {screener.FirstName}";
                SetFieldValue(fields, "Name_Screener", nameScreener);
                SetFieldValue(fields, "Rank_Screener", form.RankScreener ?? $"{screener.Rank} {screener.Rate}");
                SetFieldValue(fields, "DODID_Screener", form.DODIDScreener ?? screener.DODId ?? "");
            }
            else
            {
                SetFieldValue(fields, "Name_Screener", form.NameScreener ?? "");
                SetFieldValue(fields, "Rank_Screener", form.RankScreener ?? "");
                SetFieldValue(fields, "DODID_Screener", form.DODIDScreener ?? "");
            }

            // 7 Questions - checkboxes
            SetQuestionResponse(fields, "1", form.Question1Response);
            SetQuestionResponse(fields, "2", form.Question2Response);
            SetQuestionResponse(fields, "3", form.Question3Response);
            SetQuestionResponse(fields, "4", form.Question4Response);
            SetQuestionResponse(fields, "5", form.Question5Response);
            SetQuestionResponse(fields, "6", form.Question6Response);
            SetQuestionResponse(fields, "7", form.Question7Response);

            // Remarks
            SetFieldValue(fields, "REMARKS_1", form.Remarks1 ?? "");
            SetFieldValue(fields, "REMARKS_2", form.Remarks2 ?? "");
            SetFieldValue(fields, "REMARKS_3", form.Remarks3 ?? "");
            SetFieldValue(fields, "REMARKS_4", form.Remarks4 ?? "");
            SetFieldValue(fields, "REMARKS_5", form.Remarks5 ?? "");
            SetFieldValue(fields, "REMARKS_6", form.Remarks6 ?? "");
            SetFieldValue(fields, "REMARKS_7", form.Remarks7 ?? "");

            // Signatures and dates
            SetFieldValue(fields, "Signature_Screened", form.SignatureScreened ?? nameScreened);
            if (form.DateScreened.HasValue)
            {
                SetFieldValue(fields, "Date_Screened", form.DateScreened.Value.ToString("yyyyMMdd"));
            }

            SetFieldValue(fields, "Signature_Screener", form.SignatureScreener ?? form.NameScreener ?? "");
            if (form.DateScreener.HasValue)
            {
                SetFieldValue(fields, "Date_Screener", form.DateScreener.Value.ToString("yyyyMMdd"));
            }

            // Outcome checkboxes - explicitly set only the selected one
            // First, unset all outcome checkboxes
            UnsetCheckboxValue(fields, "Qualified");
            UnsetCheckboxValue(fields, "Unqualified");
            UnsetCheckboxValue(fields, "Review_Later");
            
            // Then set only the selected outcome
            if (form.Qualified)
            {
                SetCheckboxValue(fields, "Qualified");
            }
            else if (form.Unqualified)
            {
                SetCheckboxValue(fields, "Unqualified");
            }
            else if (form.ReviewLater)
            {
                SetCheckboxValue(fields, "Review_Later");
            }
            
            SetFieldValue(fields, "Other qualified Field", form.OtherQualifiedField ?? "");
        }

        private void SetQuestionResponse(AcroFields fields, string questionNumber, string? response)
        {
            if (string.IsNullOrEmpty(response))
                return;

            response = response.ToUpper();
            if (response == "Y" || response == "YES")
            {
                SetCheckboxValue(fields, $"{questionNumber}.Y");
            }
            else if (response == "N" || response == "NO")
            {
                SetCheckboxValue(fields, $"{questionNumber}.N");
            }
            else if (response == "NA" || response == "N/A")
            {
                SetCheckboxValue(fields, $"{questionNumber}.na");
            }
        }

        private void SetCheckboxValue(AcroFields fields, string fieldName)
        {
            try
            {
                if (fields == null || string.IsNullOrEmpty(fieldName))
                    return;

                // Try exact match first
                if (fields.Fields.ContainsKey(fieldName))
                {
                    // For checkboxes, try different export values
                    var exportValues = new[] { "Yes", "On", "1", "True" };
                    bool set = false;
                    
                    foreach (var exportValue in exportValues)
                    {
                        try
                        {
                            fields.SetField(fieldName, exportValue);
                            System.Diagnostics.Debug.WriteLine($"✓ Set checkbox '{fieldName}' = '{exportValue}'");
                            set = true;
                            break;
                        }
                        catch
                        {
                            // Try next export value
                        }
                    }
                    
                    if (!set)
                    {
                        // Try setting as checkbox using SetFieldProperty
                        var field = fields.GetFieldItem(fieldName);
                        if (field != null)
                        {
                            fields.SetFieldProperty(fieldName, "setfflags", iTextSharp.text.pdf.PdfFormField.FF_READ_ONLY, null);
                            fields.SetField(fieldName, "Yes");
                            System.Diagnostics.Debug.WriteLine($"✓ Set checkbox '{fieldName}' using SetFieldProperty");
                        }
                    }
                    return;
                }

                // Try case-insensitive match
                var matchingKey = fields.Fields.Keys.Cast<string>()
                    .FirstOrDefault(k => string.Equals(k, fieldName, StringComparison.OrdinalIgnoreCase));

                if (matchingKey != null)
                {
                    fields.SetField(matchingKey, "Yes");
                    System.Diagnostics.Debug.WriteLine($"✓ Set checkbox '{matchingKey}' (matched from '{fieldName}') = 'Yes'");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✗ Checkbox field not found: '{fieldName}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting checkbox '{fieldName}': {ex.Message}");
            }
        }

        private void UnsetCheckboxValue(AcroFields fields, string fieldName)
        {
            try
            {
                if (fields == null || string.IsNullOrEmpty(fieldName))
                    return;

                // Try exact match first
                if (fields.Fields.ContainsKey(fieldName))
                {
                    // Unset checkbox by setting to Off/No/False/0
                    var unsetValues = new[] { "Off", "No", "0", "False", "" };
                    
                    foreach (var unsetValue in unsetValues)
                    {
                        try
                        {
                            fields.SetField(fieldName, unsetValue);
                            System.Diagnostics.Debug.WriteLine($"✓ Unset checkbox '{fieldName}' = '{unsetValue}'");
                            return;
                        }
                        catch
                        {
                            // Try next unset value
                        }
                    }
                }

                // Try case-insensitive match
                var matchingKey = fields.Fields.Keys.Cast<string>()
                    .FirstOrDefault(k => string.Equals(k, fieldName, StringComparison.OrdinalIgnoreCase));

                if (matchingKey != null)
                {
                    fields.SetField(matchingKey, "Off");
                    System.Diagnostics.Debug.WriteLine($"✓ Unset checkbox '{matchingKey}' (matched from '{fieldName}') = 'Off'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unsetting checkbox '{fieldName}': {ex.Message}");
            }
        }

        private void SetFieldValue(AcroFields fields, string fieldName, string value)
        {
            if (fields == null || string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(value))
                return;

            try
            {
                // Try exact match first
                if (fields.Fields.ContainsKey(fieldName))
                {
                    fields.SetField(fieldName, value);
                    return;
                }

                // Try case-insensitive match
                var matchingKey = fields.Fields.Keys.Cast<string>()
                    .FirstOrDefault(k => string.Equals(k, fieldName, StringComparison.OrdinalIgnoreCase));

                if (matchingKey != null)
                {
                    fields.SetField(matchingKey, value);
                    return;
                }

                // Try normalized match (remove spaces, special chars)
                var normalizedFieldName = fieldName.Replace(" ", "").Replace("_", "").Replace("-", "");
                matchingKey = fields.Fields.Keys.Cast<string>()
                    .FirstOrDefault(k =>
                    {
                        var normalizedKey = k.Replace(" ", "").Replace("_", "").Replace("-", "");
                        return string.Equals(normalizedKey, normalizedFieldName, StringComparison.OrdinalIgnoreCase);
                    });

                if (matchingKey != null)
                {
                    fields.SetField(matchingKey, value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting field '{fieldName}': {ex.Message}");
            }
        }
    }
}
