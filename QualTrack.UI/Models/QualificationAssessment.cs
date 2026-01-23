using System.Collections.Generic;

namespace QualTrack.UI.Models
{
    public class QualificationAssessment
    {
        public string Weapon { get; set; } = "";
        public bool IsQualified { get; set; }
        public List<string> MissingFields { get; set; } = new List<string>();
        public List<string> PresentFields { get; set; } = new List<string>();
    }
} 