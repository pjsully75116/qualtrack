using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Interface for qualification data access operations
    /// </summary>
    public interface IQualificationRepository
    {
        Task<List<Qualification>> GetQualificationsForPersonnelAsync(DatabaseContext dbContext, int personnelId);
        Task<List<Qualification>> GetAllQualificationsAsync(DatabaseContext dbContext);
        Task<Qualification?> GetQualificationByIdAsync(DatabaseContext dbContext, int id);
        Task<int> AddQualificationAsync(DatabaseContext dbContext, Qualification qualification);
        Task<bool> UpdateQualificationAsync(DatabaseContext dbContext, Qualification qualification);
        Task<bool> DeleteQualificationAsync(DatabaseContext dbContext, int id);
        Task<bool> QualificationExistsAsync(DatabaseContext dbContext, int personnelId, string weapon);
        Task<List<Qualification>> GetExpiringQualificationsAsync(DatabaseContext dbContext, int daysThreshold);
        Task<List<Qualification>> GetQualificationsNeedingSustainmentAsync(DatabaseContext dbContext);
    }
} 