namespace QualTrack.Core.Models
{
    /// <summary>
    /// Represents the qualification status of a personnel member for a specific weapon
    /// </summary>
    public class QualificationStatus
    {
        public bool IsQualified { get; set; }
        public bool SustainmentDue { get; set; }
        public bool IsDisqualified { get; set; }
        public DateTime ExpiresOn { get; set; }
        public int DaysUntilExpiration { get; set; }
        public int DaysUntilSustainment { get; set; }

        public QualificationStatus()
        {
            IsQualified = false;
            SustainmentDue = false;
            IsDisqualified = false;
            ExpiresOn = DateTime.MinValue;
            DaysUntilExpiration = 0;
            DaysUntilSustainment = 0;
        }
    }
} 