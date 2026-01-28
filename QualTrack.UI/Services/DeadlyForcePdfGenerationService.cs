using System;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;

namespace QualTrack.UI.Services
{
    public class DeadlyForcePdfGenerationService
    {
        private readonly string _templatePath;
        private readonly string _outputDirectory;

        public DeadlyForcePdfGenerationService(string? templatePath = null, string? outputDirectory = null)
        {
            _templatePath = templatePath ?? ResolveTemplatePath();
            _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "DeadlyForce_Forms");

            Directory.CreateDirectory(_outputDirectory);
        }

        public string GenerateDeadlyForcePdf(string traineeSignature, DateTime traineeDate, string observerSignature, DateTime observerDate)
        {
            try
            {
                if (!File.Exists(_templatePath))
                {
                    throw new FileNotFoundException($"Template not found: {_templatePath}");
                }

                var dateStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"DeadlyForce_{dateStr}.pdf";
                var outputPath = Path.Combine(_outputDirectory, filename);

                using var reader = new PdfReader(_templatePath);
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var stamper = new PdfStamper(reader, fs);
                var fields = stamper.AcroFields;

                SetFieldValue(fields, "Signature (Trainee)", traineeSignature);
                SetFieldValue(fields, "Date (Trainee)", traineeDate.ToString("MM/dd/yyyy"));
                SetFieldValue(fields, "Signature (Observer)", observerSignature);
                SetFieldValue(fields, "Date (Observer)", observerDate.ToString("MM/dd/yyyy"));

                stamper.FormFlattening = true;
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating Deadly Force PDF: {ex.Message}", ex);
            }
        }

        private static void SetFieldValue(AcroFields form, string fieldName, string value)
        {
            try
            {
                if (form == null || form.Fields == null || string.IsNullOrWhiteSpace(fieldName))
                {
                    return;
                }

                if (form.Fields.ContainsKey(fieldName))
                {
                    form.SetField(fieldName, value ?? string.Empty);
                    return;
                }

                var normalizedTarget = NormalizeFieldName(fieldName);
                var matchedField = form.Fields.Keys.FirstOrDefault(key =>
                    NormalizeFieldName(key) == normalizedTarget);

                if (!string.IsNullOrEmpty(matchedField))
                {
                    form.SetField(matchedField, value ?? string.Empty);
                }
            }
            catch
            {
                // Best-effort field fill; ignore individual failures.
            }
        }

        private static string NormalizeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            return new string(fieldName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        private static string ResolveTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var baseCandidate = Path.Combine(baseDir, "Deadly Force Memo Qualtrack.pdf");
            if (File.Exists(baseCandidate))
            {
                return baseCandidate;
            }

            var repoCandidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Deadly Force Memo Qualtrack.pdf"));
            return repoCandidate;
        }
    }
}
