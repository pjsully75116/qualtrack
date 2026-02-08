using System;

namespace QualTrack.Core.Models
{
    public class SignatureQueueItem
    {
        public int Id { get; set; }
        public int? DocumentId { get; set; }
        public string DocumentPath { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public int? PersonnelId { get; set; }
        public string Status { get; set; } = "Pending";
        public string CurrentRole { get; set; } = string.Empty;
        public string RequiredRoles { get; set; } = string.Empty;
        public string? CompletedRoles { get; set; }
        public string? ClaimedBy { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public string? LastAction { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
