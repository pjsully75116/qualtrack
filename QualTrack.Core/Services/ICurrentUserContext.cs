using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public interface ICurrentUserContext
    {
        RbacRole Role { get; set; }
    }
}
