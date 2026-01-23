using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public interface IDD2760FormRepository
    {
        Task<DD2760Form?> GetByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
        Task<List<DD2760Form>> GetAllByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
        Task<DD2760Form?> GetByIdAsync(DatabaseContext dbContext, int id);
        Task<List<DD2760Form>> GetAllAsync(DatabaseContext dbContext);
        Task<List<DD2760Form>> GetExpiringFormsAsync(DatabaseContext dbContext, int daysThreshold);
        Task<int> AddAsync(DatabaseContext dbContext, DD2760Form form);
        Task<bool> UpdateAsync(DatabaseContext dbContext, DD2760Form form);
        Task<bool> DeleteAsync(DatabaseContext dbContext, int id);
        Task<bool> DeleteByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
    }
}
