using QualTrack.Core.Models;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Interface for personnel data access operations
    /// </summary>
    public interface IPersonnelRepository
    {
        Task<List<Personnel>> GetAllPersonnelAsync();
        Task<Personnel?> GetPersonnelByIdAsync(int id);
        Task<Personnel?> GetPersonnelByNameAndRateAsync(string name, string rate);
        Task<int> AddPersonnelAsync(Personnel personnel);
        Task<bool> UpdatePersonnelAsync(Personnel personnel);
        Task<bool> DeletePersonnelAsync(int id);
        Task<List<Personnel>> GetPersonnelWithQualificationsAsync();
        Task<List<(string Type, string Section)>> GetDutySectionsForPersonnelAsync(int personnelId);
        Task<bool> AddDutySectionToPersonnelAsync(int personnelId, string dutySectionType, string sectionNumber);
    }
} 