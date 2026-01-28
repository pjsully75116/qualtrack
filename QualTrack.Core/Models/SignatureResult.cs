using System;

namespace QualTrack.Core.Models
{
    public class SignatureResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? SignatureData { get; set; }
        public DateTime? SignedAt { get; set; }
        public string? SignedDocumentPath { get; set; }
        public string? SignerThumbprint { get; set; }
    }
}
