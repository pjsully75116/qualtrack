using System;

namespace QualTrack.Core.Models
{
    public class SignatureRequest
    {
        public string Purpose { get; set; } = string.Empty;
        public string DocumentPath { get; set; } = string.Empty;
        public string SignerDisplayName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public string? OutputPath { get; set; }
        public string? SignatureFieldName { get; set; }
        public int? PageNumber { get; set; }
        public string? VisibleStampText { get; set; }
    }
}
