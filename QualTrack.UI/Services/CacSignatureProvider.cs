using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using iText.Forms;
using iText.Forms.Fields;
using iText.Forms.Form.Element;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Signatures;
using IOPath = System.IO.Path;
using QualTrack.Core.Models;
using QualTrack.Core.Services;

namespace QualTrack.UI.Services
{
    public class CacSignatureProvider : ISignatureProvider
    {
        public string ProviderName => "CAC";
        public bool IsAvailable => GetCandidateCertificates().Count > 0;

        public Task<SignatureResult> RequestSignatureAsync(SignatureRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DocumentPath) || !File.Exists(request.DocumentPath))
                {
                    return Task.FromResult(new SignatureResult
                    {
                        Success = false,
                        Message = "Document path is invalid or file does not exist."
                    });
                }

                var cert = SelectCertificate();
                if (cert == null)
                {
                    return Task.FromResult(new SignatureResult
                    {
                        Success = false,
                        Message = "No CAC signing certificate selected."
                    });
                }

            var outputPath = string.IsNullOrWhiteSpace(request.OutputPath)
                ? GetSignedOutputPath(request.DocumentPath)
                    : request.OutputPath!;

                ApplySignature(request, cert, outputPath);

                return Task.FromResult(new SignatureResult
                {
                    Success = true,
                    Message = "Document signed successfully.",
                    SignedAt = DateTime.Now,
                    SignedDocumentPath = outputPath,
                    SignerThumbprint = cert.Thumbprint
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new SignatureResult
                {
                    Success = false,
                    Message = $"CAC signing failed: {ex.Message}"
                });
            }
        }

        private static X509Certificate2Collection GetCandidateCertificates()
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates
                .Find(X509FindType.FindByTimeValid, DateTime.Now, false)
                .Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

            return new X509Certificate2Collection(certs.Cast<X509Certificate2>()
                .Where(c => c.HasPrivateKey)
                .ToArray());
        }

        private static X509Certificate2? SelectCertificate()
        {
            var candidates = GetCandidateCertificates();
            if (candidates.Count == 0)
            {
                return null;
            }

            var selection = X509Certificate2UI.SelectFromCollection(
                candidates,
                "Select CAC Certificate",
                "Select the CAC signing certificate to apply to this document.",
                X509SelectionFlag.SingleSelection);

            return selection.Count > 0 ? selection[0] : null;
        }

        private static string GetSignedOutputPath(string inputPath)
        {
            var directory = IOPath.GetDirectoryName(inputPath) ?? string.Empty;
            var fileName = IOPath.GetFileNameWithoutExtension(inputPath);
            var extension = IOPath.GetExtension(inputPath);
            return IOPath.Combine(directory, $"{fileName}_signed{extension}");
        }

        private static void ApplySignature(SignatureRequest request, X509Certificate2 cert, string outputPath)
        {
            using var reader = new PdfReader(request.DocumentPath);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            var signer = new PdfSigner(reader, outputStream, new StampingProperties().UseAppendMode());

            var pageNumber = request.PageNumber ?? 1;
            var pageSize = signer.GetDocument().GetPage(pageNumber).GetPageSize();
            var rect = new Rectangle(pageSize.GetRight() - 220, 36, 200, 60);

            var fieldName = string.IsNullOrWhiteSpace(request.SignatureFieldName)
                ? $"Signature_{DateTime.Now:yyyyMMddHHmmss}"
                : request.SignatureFieldName;

            var candidateFieldNames = GetSignatureFieldCandidates(request.SignatureFieldName);
            if (candidateFieldNames.Count > 0 &&
                TryResolveSignatureAnchor(signer.GetDocument(), candidateFieldNames, out var anchorRect, out var anchorPage, out var isSignatureField, out var matchedFieldName))
            {
                rect = anchorRect;
                pageNumber = anchorPage;
                fieldName = isSignatureField && !string.IsNullOrWhiteSpace(matchedFieldName)
                    ? matchedFieldName
                    : $"Signature_{DateTime.Now:yyyyMMddHHmmss}";
            }

            var displayName = string.IsNullOrWhiteSpace(request.SignerDisplayName)
                ? cert.GetNameInfo(X509NameType.SimpleName, false)
                : request.SignerDisplayName;
            var stampText = string.IsNullOrWhiteSpace(request.VisibleStampText)
                ? $"{displayName}\n{DateTime.Now:yyyy-MM-dd HH:mm}\n{request.Purpose}"
                : request.VisibleStampText;

            var appearance = new SignatureFieldAppearance(SignerProperties.IGNORED_ID)
                .SetContent(stampText);

            var signerProperties = new SignerProperties()
                .SetFieldName(fieldName)
                .SetPageNumber(pageNumber)
                .SetPageRect(rect)
                .SetReason(request.Purpose)
                .SetLocation("CAC")
                .SetSignatureAppearance(appearance);

            signer.SetSignerProperties(signerProperties);

            var container = new CacSignatureContainer(cert);
            signer.SignExternalContainer(container, 8192);
        }

        private static bool TryResolveSignatureAnchor(
            PdfDocument document,
            IReadOnlyList<string> fieldNames,
            out Rectangle rect,
            out int pageNumber,
            out bool isSignatureField,
            out string? matchedFieldName)
        {
            rect = default;
            pageNumber = 1;
            isSignatureField = false;
            matchedFieldName = null;

            var form = PdfAcroForm.GetAcroForm(document, false);
            if (form == null)
            {
                return false;
            }

            var fields = form.GetAllFormFields();
            if (fields == null || fields.Count == 0)
            {
                return false;
            }

            PdfFormField? field = null;
            foreach (var name in fieldNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (fields.TryGetValue(name, out field))
                {
                    if (!IsSignedSignatureField(field))
                    {
                        matchedFieldName = name;
                        break;
                    }
                    field = null;
                    continue;
                }

                var normalizedTarget = NormalizeFieldName(name);
                if (string.IsNullOrEmpty(normalizedTarget))
                {
                    continue;
                }

                var exactKey = fields.Keys.FirstOrDefault(key => NormalizeFieldName(key) == normalizedTarget);
                if (!string.IsNullOrWhiteSpace(exactKey))
                {
                    fields.TryGetValue(exactKey, out field);
                    if (field != null && !IsSignedSignatureField(field))
                    {
                        matchedFieldName = exactKey;
                        break;
                    }
                    field = null;
                    continue;
                }

                var containsKey = fields.Keys.FirstOrDefault(key => NormalizeFieldName(key).Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(containsKey))
                {
                    fields.TryGetValue(containsKey, out field);
                    if (field != null && !IsSignedSignatureField(field))
                    {
                        matchedFieldName = containsKey;
                        break;
                    }
                    field = null;
                    continue;
                }
            }

            if (field == null)
            {
                return false;
            }

            var widgets = field.GetWidgets();
            if (widgets == null || widgets.Count == 0)
            {
                return false;
            }

            var widget = widgets[0];
            rect = widget.GetRectangle().ToRectangle();
            var widgetPage = widget.GetPage();
            pageNumber = widgetPage != null ? document.GetPageNumber(widgetPage) : 1;
            var fieldType = field.GetFormType();
            isSignatureField = PdfName.Sig.Equals(fieldType);

            return true;
        }

        private static bool IsSignedSignatureField(PdfFormField field)
        {
            if (field == null)
            {
                return false;
            }

            var fieldType = field.GetFormType();
            if (!PdfName.Sig.Equals(fieldType))
            {
                return false;
            }

            var value = field.GetValue();
            return value != null && !value.IsNull();
        }

        private static List<string> GetSignatureFieldCandidates(string? signatureFieldName)
        {
            if (string.IsNullOrWhiteSpace(signatureFieldName))
            {
                return new List<string>();
            }

            return signatureFieldName
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private sealed class CacSignatureContainer : IExternalSignatureContainer
        {
            private readonly X509Certificate2 _certificate;

            public CacSignatureContainer(X509Certificate2 certificate)
            {
                _certificate = certificate;
            }

            public byte[] Sign(Stream data)
            {
                using var ms = new MemoryStream();
                data.CopyTo(ms);
                var content = new ContentInfo(ms.ToArray());
                var signedCms = new SignedCms(content, true);
                var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, _certificate)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly
                };

                signedCms.ComputeSignature(signer, false);
                return signedCms.Encode();
            }

            public void ModifySigningDictionary(PdfDictionary signDic)
            {
                signDic.Put(PdfName.Filter, PdfName.Adobe_PPKLite);
                signDic.Put(PdfName.SubFilter, PdfName.Adbe_pkcs7_detached);
            }
        }
    }
}
