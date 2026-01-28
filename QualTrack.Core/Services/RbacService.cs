using System.Collections.Generic;
using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public class RbacService : IRbacService
    {
        private static readonly Dictionary<RbacRole, HashSet<RbacPermission>> PermissionsByRole =
            new Dictionary<RbacRole, HashSet<RbacPermission>>
            {
                [RbacRole.Admin] = new HashSet<RbacPermission>
                {
                    RbacPermission.ViewDashboard,
                    RbacPermission.ViewTrainingJacket,
                    RbacPermission.ManagePersonnel,
                    RbacPermission.ManageQualifications,
                    RbacPermission.ManageAdminForms,
                    RbacPermission.ManageCrewServed,
                    RbacPermission.GeneratePdf,
                    RbacPermission.ConfigureDashboard,
                    RbacPermission.ManageSystem
                },
                [RbacRole.SAMI] = new HashSet<RbacPermission>
                {
                    RbacPermission.ViewDashboard,
                    RbacPermission.ViewTrainingJacket,
                    RbacPermission.ManagePersonnel,
                    RbacPermission.ManageQualifications,
                    RbacPermission.ManageAdminForms,
                    RbacPermission.GeneratePdf,
                    RbacPermission.ConfigureDashboard
                },
                [RbacRole.CSWI] = new HashSet<RbacPermission>
                {
                    RbacPermission.ViewDashboard,
                    RbacPermission.ViewTrainingJacket,
                    RbacPermission.ManageCrewServed,
                    RbacPermission.GeneratePdf
                },
                [RbacRole.Viewer] = new HashSet<RbacPermission>
                {
                    RbacPermission.ViewDashboard,
                    RbacPermission.ViewTrainingJacket
                }
            };

        public bool HasPermission(RbacRole role, RbacPermission permission)
        {
            return PermissionsByRole.TryGetValue(role, out var permissions)
                && permissions.Contains(permission);
        }
    }
}
