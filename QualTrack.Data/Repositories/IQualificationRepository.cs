using QualTrack.Core.Models;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Interface for qualification data access operations
    /// </summary>
    public interface IQualificationRepository
    {
        Task<List<Qualification>> GetQualificationsForPersonnelAsync(int personnelId);
        Task<List<Qualification>> GetAllQualificationsAsync();
        Task<Qualification?> GetQualificationByIdAsync(int id);
        Task<int> AddQualificationAsync(Qualification qualification);
        Task<bool> UpdateQualificationAsync(Qualification qualification);
        Task<bool> DeleteQualificationAsync(int id);
        Task<bool> QualificationExistsAsync(int personnelId, string weapon);
        Task<List<Qualification>> GetExpiringQualificationsAsync(int daysThreshold);
        Task<List<Qualification>> GetQualificationsNeedingSustainmentAsync();
    }
} 