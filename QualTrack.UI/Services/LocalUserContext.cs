using QualTrack.Core.Models;
using QualTrack.Core.Services;

namespace QualTrack.UI.Services
{
    public class LocalUserContext : ICurrentUserContext
    {
        public RbacRole Role { get; set; } = RbacRole.Admin;
    }
}
