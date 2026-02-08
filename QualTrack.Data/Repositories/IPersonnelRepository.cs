using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Interface for personnel data access operations
    /// </summary>
    public interface IPersonnelRepository
    {
        Task<List<Personnel>> GetAllPersonnelAsync(DatabaseContext dbContext);
        Task<Personnel?> GetPersonnelByIdAsync(DatabaseContext dbContext, int id);
        Task<Personnel?> GetPersonnelByDODIdAsync(DatabaseContext dbContext, string dodId);
        Task<Personnel?> GetPersonnelByNameAndRateAsync(DatabaseContext dbContext, string lastName, string firstName, string rate);
        Task<int> AddPersonnelAsync(DatabaseContext dbContext, Personnel personnel);
        Task<bool> UpdatePersonnelAsync(DatabaseContext dbContext, Personnel personnel);
        Task<bool> UpdatePersonnelFieldsAsync(DatabaseContext dbContext, Personnel personnel);
        Task<bool> DeletePersonnelAsync(DatabaseContext dbContext, int id);
        Task<bool> PersonnelExistsAsync(DatabaseContext dbContext, string dodId);
        Task<List<Personnel>> GetPersonnelWithQualificationsAsync(DatabaseContext dbContext);
        Task<List<Personnel>> GetPersonnelWithQualificationsFilteredAsync(
            DatabaseContext dbContext,
            string? statusFilter = null,
            string? dutySectionType = null,
            string? dutySectionNumber = null,
            string? weaponFilter = null);
        Task<(List<Personnel> Personnel, int TotalCount)> GetPersonnelWithQualificationsPaginatedAsync(
            DatabaseContext dbContext,
            int pageNumber = 1,
            int pageSize = 50,
            string? statusFilter = null,
            string? dutySectionType = null,
            string? dutySectionNumber = null,
            string? weaponFilter = null);
        Task<List<(string Type, string Section)>> GetDutySectionsForPersonnelAsync(DatabaseContext dbContext, int personnelId);
        Task<bool> AddDutySectionToPersonnelAsync(DatabaseContext dbContext, int personnelId, string dutySectionType, string sectionNumber);
        Task<bool> RemoveDutySectionFromPersonnelAsync(DatabaseContext dbContext, int personnelId, string dutySectionType, string sectionNumber);
        Task ClearAllDataAsync(DatabaseContext dbContext);
    }
} 