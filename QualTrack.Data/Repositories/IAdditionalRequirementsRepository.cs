using QualTrack.Core.Models;

namespace QualTrack.Data.Repositories
{
    public interface IAdditionalRequirementsRepository
    {
        Task<AdditionalRequirements?> GetByPersonnelIdAsync(int personnelId);
        Task<bool> SaveAsync(int personnelId, AdditionalRequirements requirements);
        Task<bool> DeleteByPersonnelIdAsync(int personnelId);
    }
} 