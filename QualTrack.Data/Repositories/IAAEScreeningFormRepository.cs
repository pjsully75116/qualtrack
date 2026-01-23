using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public interface IAAEScreeningFormRepository
    {
        Task<AAEScreeningForm?> GetByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
        Task<List<AAEScreeningForm>> GetAllByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
        Task<AAEScreeningForm?> GetByIdAsync(DatabaseContext dbContext, int id);
        Task<List<AAEScreeningForm>> GetAllAsync(DatabaseContext dbContext);
        Task<List<AAEScreeningForm>> GetExpiringFormsAsync(DatabaseContext dbContext, int daysThreshold);
        Task<int> AddAsync(DatabaseContext dbContext, AAEScreeningForm form);
        Task<bool> UpdateAsync(DatabaseContext dbContext, AAEScreeningForm form);
        Task<bool> DeleteAsync(DatabaseContext dbContext, int id);
        Task<bool> DeleteByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
    }
}
