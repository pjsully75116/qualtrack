using System.Threading.Tasks;
using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public interface ISignatureProvider
    {
        string ProviderName { get; }
        bool IsAvailable { get; }
        Task<SignatureResult> RequestSignatureAsync(SignatureRequest request);
    }
}
