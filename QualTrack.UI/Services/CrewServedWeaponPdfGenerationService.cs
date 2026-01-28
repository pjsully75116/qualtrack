using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using QualTrack.Core.Models;
using QualTrack.UI.Models;

namespace QualTrack.UI.Services
{
    public class CrewServedWeaponPdfFieldMap
    {
        // Session-level fields
        public string? ShipStation { get; set; }
        public string? DivisionActivity { get; set; }
        public string? Weapon { get; set; }
        public string? RangeNameLocation { get; set; }
        public string? DateOfFiring { get; set; }
        public string? PageNumber { get; set; }
        public string? PageTotal { get; set; }
        public string? InstructorName { get; set; }
        public string? InstructorRankRate { get; set; }
        public string? Signature { get; set; }
        public string? SignatureDate { get; set; }

        // Entry-row fields (pattern supports "{row}" placeholder)
        public string? NamePattern { get; set; }
        public string? RankPattern { get; set; }

        public string? LightFreeP1Pattern { get; set; }
        public string? LightFreeP2Pattern { get; set; }
        public string? LightFreeP3Pattern { get; set; }
        public string? LightFreeP4Pattern { get; set; }
        public string? LightFreeP5Pattern { get; set; }
        public string? LightFreeP6Pattern { get; set; }
        public string? LightTeP1Pattern { get; set; }
        public string? LightTeP2Pattern { get; set; }
        public string? LightTeP3Pattern { get; set; }
        public string? LightTeP4Pattern { get; set; }
        public string? LightTeP5Pattern { get; set; }

        public string? HeavyFreeP1Pattern { get; set; }
        public string? HeavyFreeP2Pattern { get; set; }
        public string? HeavyFreeP3Pattern { get; set; }
        public string? HeavyFreeP4Pattern { get; set; }
        public string? HeavyFreeP5Pattern { get; set; }
        public string? HeavyFreeP6Pattern { get; set; }
        public string? HeavyTeP1Pattern { get; set; }
        public string? HeavyTeP2Pattern { get; set; }
        public string? HeavyTeP3Pattern { get; set; }
        public string? HeavyTeP4Pattern { get; set; }
        public string? HeavyTeP5Pattern { get; set; }
    }

    public class CrewServedWeaponPdfGenerationService
    {
        private readonly string _templatePath;
        private readonly string _outputDirectory;
        private readonly CrewServedWeaponPdfFieldMap _fieldMap;

        public CrewServedWeaponPdfGenerationService(
            string? templatePath = null,
            string? outputDirectory = null,
            CrewServedWeaponPdfFieldMap? fieldMap = null)
        {
            _templatePath = ResolveTemplatePath(templatePath);
            _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedForms");
            Directory.CreateDirectory(_outputDirectory);
            _fieldMap = fieldMap ?? new CrewServedWeaponPdfFieldMap();
        }

        public string Generate3591_2Pdf(CrewServedWeaponSession session, List<CrewServedWeaponEntry> entries)
        {
            if (!File.Exists(_templatePath))
            {
                throw new FileNotFoundException("Could not find 3591_2QualTrack.pdf template. Please ensure the file is in the application directory.");
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var shipToken = string.IsNullOrWhiteSpace(session.ShipStation) ? "Unknown" : session.ShipStation.Replace(" ", "");
            var outputPath = Path.Combine(_outputDirectory, $"3591_2_Filled_{shipToken}_{timestamp}.pdf");

            using (var reader = new PdfReader(_templatePath))
            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var stamper = new PdfStamper(reader, fs))
            {
                var form = stamper.AcroFields;

                // Session-level fields (field names to be supplied later)
                SetFieldValueIfMapped(form, _fieldMap.ShipStation, session.ShipStation);
                SetFieldValueIfMapped(form, _fieldMap.DivisionActivity, session.DivisionActivity);
                SetFieldValueIfMapped(form, _fieldMap.Weapon, session.Weapon);
                SetFieldValueIfMapped(form, _fieldMap.RangeNameLocation, session.RangeNameLocation);
                SetFieldValueIfMapped(form, _fieldMap.DateOfFiring, session.DateOfFiring?.ToString("MM/dd/yyyy") ?? string.Empty);
                var totalPages = Math.Max(1, (int)Math.Ceiling(entries.Count / 20.0));
                SetFieldValueIfMapped(form, _fieldMap.PageNumber, "1");
                SetFieldValueIfMapped(form, _fieldMap.PageTotal, totalPages.ToString());
                SetFieldStyleIfMapped(form, _fieldMap.PageNumber, 12f);
                SetFieldStyleIfMapped(form, _fieldMap.PageTotal, 12f);
                SetFieldValueIfMapped(form, _fieldMap.InstructorName, session.InstructorName ?? string.Empty);
                SetFieldValueIfMapped(form, _fieldMap.InstructorRankRate, session.InstructorRankRate ?? string.Empty);
                SetFieldValueIfMapped(form, _fieldMap.Signature, session.RsoSignature ?? string.Empty);
                SetFieldValueIfMapped(form, _fieldMap.SignatureDate, session.RsoSignatureDate?.ToString("MM/dd/yyyy") ?? string.Empty);

                // Entry rows
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    int rowNumber = i + 1;

                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.NamePattern, rowNumber), entry.GunnerName);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.RankPattern, rowNumber), entry.GunnerRankRate);

                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP1Pattern, rowNumber), entry.LightFreeP1?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP2Pattern, rowNumber), entry.LightFreeP2?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP3Pattern, rowNumber), entry.LightFreeP3?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP4Pattern, rowNumber), entry.LightFreeP4?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP5Pattern, rowNumber), entry.LightFreeP5?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightFreeP6Pattern, rowNumber), entry.LightFreeP6?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightTeP1Pattern, rowNumber), entry.LightTeP1?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightTeP2Pattern, rowNumber), entry.LightTeP2?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightTeP3Pattern, rowNumber), entry.LightTeP3?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightTeP4Pattern, rowNumber), entry.LightTeP4?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.LightTeP5Pattern, rowNumber), entry.LightTeP5?.ToString() ?? string.Empty);

                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP1Pattern, rowNumber), entry.HeavyFreeP1?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP2Pattern, rowNumber), entry.HeavyFreeP2?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP3Pattern, rowNumber), entry.HeavyFreeP3?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP4Pattern, rowNumber), entry.HeavyFreeP4?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP5Pattern, rowNumber), entry.HeavyFreeP5?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyFreeP6Pattern, rowNumber), entry.HeavyFreeP6?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyTeP1Pattern, rowNumber), entry.HeavyTeP1?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyTeP2Pattern, rowNumber), entry.HeavyTeP2?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyTeP3Pattern, rowNumber), entry.HeavyTeP3?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyTeP4Pattern, rowNumber), entry.HeavyTeP4?.ToString() ?? string.Empty);
                    SetFieldValueIfMapped(form, ResolvePattern(_fieldMap.HeavyTeP5Pattern, rowNumber), entry.HeavyTeP5?.ToString() ?? string.Empty);
                }

                stamper.FormFlattening = true;
            }

            return outputPath;
        }

        public List<string> GetPdfFieldNames()
        {
            var fieldNames = new List<string>();
            using (var reader = new PdfReader(_templatePath))
            {
                var form = reader.AcroFields;
                if (form != null && form.Fields != null)
                {
                    fieldNames.AddRange(form.Fields.Keys);
                }
            }
            return fieldNames;
        }

        private static string? ResolvePattern(string? pattern, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            return pattern.Replace("{row}", rowNumber.ToString());
        }

        private static void SetFieldValueIfMapped(AcroFields form, string? fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            SetFieldValue(form, value, fieldName);
        }

        private static void SetFieldStyleIfMapped(AcroFields form, string? fieldName, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            try
            {
                form.SetFieldProperty(fieldName, "alignment", iTextSharp.text.Element.ALIGN_CENTER, null);
                form.SetFieldProperty(fieldName, "textsize", fontSize, null);
            }
            catch
            {
                // Best-effort styling; ignore if field doesn't support it.
            }
        }

        private static void SetFieldValue(AcroFields form, string value, params string[] fieldNames)
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

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3591_2QualTrack.pdf");
        }
    }
}
