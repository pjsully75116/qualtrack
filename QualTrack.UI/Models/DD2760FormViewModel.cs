using System;

namespace QualTrack.UI.Models
{
    public class DD2760FormViewModel
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        public string PersonnelName { get; set; } = "";
        public DateTime DateCompleted { get; set; }
        public DateTime DateExpires { get; set; }
        public string Status { get; set; } = "";
    }
}
