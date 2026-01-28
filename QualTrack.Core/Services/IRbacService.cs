using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public interface IRbacService
    {
        bool HasPermission(RbacRole role, RbacPermission permission);
    }
}
