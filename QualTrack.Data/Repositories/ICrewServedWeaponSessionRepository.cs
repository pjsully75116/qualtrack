using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Repository interface for crew served weapon sessions (3591/2 forms)
    /// </summary>
    public interface ICrewServedWeaponSessionRepository
    {
        Task<int> AddSessionAsync(DatabaseContext dbContext, CrewServedWeaponSession session);
        Task<CrewServedWeaponSession?> GetSessionByIdAsync(DatabaseContext dbContext, int sessionId);
        Task<List<CrewServedWeaponSession>> GetAllSessionsAsync(DatabaseContext dbContext);
        Task<List<CrewServedWeaponSession>> GetSessionsByPersonnelIdAsync(DatabaseContext dbContext, int personnelId);
        Task<List<CrewServedWeaponSession>> GetSessionsByWeaponAsync(DatabaseContext dbContext, string weapon);
        Task UpdateSessionPdfFilePathAsync(DatabaseContext dbContext, int sessionId, string pdfFilePath);
        Task<bool> DeleteSessionAsync(DatabaseContext dbContext, int sessionId);
    }
}
