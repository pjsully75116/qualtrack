using System.Collections.Generic;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public interface ISignatureQueueRepository
    {
        Task<int> AddAsync(DatabaseContext context, SignatureQueueItem item);
        Task UpdateAsync(DatabaseContext context, SignatureQueueItem item);
        Task<SignatureQueueItem?> GetByIdAsync(DatabaseContext context, int id);
        Task<List<SignatureQueueItem>> GetInboxAsync(DatabaseContext context, string? role, string? status, string? formType);
    }
}
