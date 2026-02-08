using System;
using System.Collections.Generic;
using System.Linq;
using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public class SignatureQueueService
    {
        public void AdvanceAfterSignature(SignatureQueueItem item, string completedRole)
        {
            var requiredRoles = SplitRoles(item.RequiredRoles);
            var completedRoles = SplitRoles(item.CompletedRoles);

            if (!completedRoles.Contains(completedRole))
            {
                completedRoles.Add(completedRole);
            }

            item.CompletedRoles = string.Join("|", completedRoles);
            var nextRole = requiredRoles.FirstOrDefault(role => !completedRoles.Contains(role));

            if (string.IsNullOrWhiteSpace(nextRole))
            {
                item.Status = "Completed";
                item.CurrentRole = string.Empty;
            }
            else
            {
                item.Status = "Pending";
                item.CurrentRole = nextRole;
            }

            item.UpdatedAt = DateTime.Now;
        }

        public void ReturnToQueue(SignatureQueueItem item, string? returnRole = null)
        {
            if (!string.IsNullOrWhiteSpace(returnRole))
            {
                item.CurrentRole = returnRole;
            }

            item.Status = "Returned";
            item.UpdatedAt = DateTime.Now;
        }

        public string? GetPreviousRole(SignatureQueueItem item)
        {
            var requiredRoles = SplitRoles(item.RequiredRoles);
            var completedRoles = SplitRoles(item.CompletedRoles);

            if (completedRoles.Count == 0)
            {
                return null;
            }

            var lastCompleted = completedRoles.Last();
            var index = requiredRoles.IndexOf(lastCompleted);
            return index >= 0 ? requiredRoles[index] : null;
        }

        private static List<string> SplitRoles(string? roles)
        {
            return roles?
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new List<string>();
        }
    }
}
