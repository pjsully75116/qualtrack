namespace QualTrack.UI.Models
{
    public class SailorDisplayModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string DODId { get; set; } = string.Empty;
        public string RankRate { get; set; } = string.Empty;
        
        public SailorDisplayModel(int id, string lastName, string firstName, string dodId, string rank, string rate)
        {
            Id = id;
            var rankRate = $"{rank} {rate}".Trim();
            DisplayName = $"{lastName}, {firstName} ({dodId}) {rankRate}".Trim();
            DODId = dodId;
            RankRate = $"{rank} {rate}".Trim();
        }
    }
} 