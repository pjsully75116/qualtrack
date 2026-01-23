using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    public class AdditionalRequirementsService
    {
        public AdditionalRequirementStatus CalculateStatus(AdditionalRequirement requirement)
        {
            var status = new AdditionalRequirementStatus
            {
                Type = requirement.Type
            };

            if (requirement.Status == RequirementStatus.NotRequired)
            {
                status.Status = RequirementStatus.NotRequired;
                status.StatusDescription = "Not Required";
                status.StatusColor = "Gray";
                return status;
            }

            if (requirement.Status == RequirementStatus.Required)
            {
                status.Status = RequirementStatus.Required;
                status.StatusDescription = "Required";
                status.StatusColor = "Orange";
                
                if (requirement.DateRequired.HasValue)
                {
                    var daysUntilRequired = (requirement.DateRequired.Value - DateTime.Today).Days;
                    status.DaysUntilRequired = daysUntilRequired;
                    
                    if (daysUntilRequired < 0)
                    {
                        status.Status = RequirementStatus.Overdue;
                        status.StatusDescription = "Overdue";
                        status.StatusColor = "Red";
                        status.IsOverdue = true;
                    }
                }
                return status;
            }

            if (requirement.Status == RequirementStatus.Completed && requirement.DateExpires.HasValue)
            {
                var daysUntilExpiration = (requirement.DateExpires.Value - DateTime.Today).Days;
                status.DaysUntilExpiration = daysUntilExpiration;

                if (daysUntilExpiration < 0)
                {
                    status.Status = RequirementStatus.Expired;
                    status.StatusDescription = "Expired";
                    status.StatusColor = "Red";
                }
                else if (daysUntilExpiration <= GetWarningPeriod(requirement.Type))
                {
                    status.Status = RequirementStatus.Completed;
                    status.StatusDescription = $"Expires in {daysUntilExpiration} days";
                    status.StatusColor = "Orange";
                    status.IsWarning = true;
                }
                else
                {
                    status.Status = RequirementStatus.Completed;
                    status.StatusDescription = $"Valid for {daysUntilExpiration} days";
                    status.StatusColor = "Green";
                }
            }

            return status;
        }

        public DateTime CalculateExpirationDate(DateTime completionDate, RequirementType type)
        {
            return completionDate.Add(GetValidityPeriod(type));
        }

        public DateTime CalculateRequiredDate(DateTime? lastCompletionDate, RequirementType type)
        {
            if (!lastCompletionDate.HasValue)
            {
                // If never completed, require immediately
                return DateTime.Today;
            }

            // Calculate when the requirement becomes due again
            return lastCompletionDate.Value.Add(GetValidityPeriod(type));
        }

        public bool IsRequirementDue(AdditionalRequirement requirement)
        {
            if (requirement.Status == RequirementStatus.NotRequired)
                return false;

            if (requirement.Status == RequirementStatus.Required)
            {
                return !requirement.DateRequired.HasValue || requirement.DateRequired.Value <= DateTime.Today;
            }

            if (requirement.Status == RequirementStatus.Completed)
            {
                return requirement.DateExpires.HasValue && requirement.DateExpires.Value <= DateTime.Today;
            }

            return false;
        }

        public List<AdditionalRequirementStatus> GetPersonnelRequirements(int personnelId, List<AdditionalRequirement> requirements)
        {
            var personnelRequirements = requirements.Where(r => r.PersonnelId == personnelId).ToList();
            var statuses = new List<AdditionalRequirementStatus>();

            // Check each requirement type
            foreach (RequirementType type in Enum.GetValues(typeof(RequirementType)))
            {
                var requirement = personnelRequirements.FirstOrDefault(r => r.Type == type);
                if (requirement != null)
                {
                    statuses.Add(CalculateStatus(requirement));
                }
                else
                {
                    // No requirement record exists - determine if one should be created
                    statuses.Add(new AdditionalRequirementStatus
                    {
                        Type = type,
                        Status = RequirementStatus.Required,
                        StatusDescription = "Required - Not Started",
                        StatusColor = "Orange",
                        DaysUntilRequired = 0,
                        IsOverdue = true
                    });
                }
            }

            return statuses;
        }

        private TimeSpan GetValidityPeriod(RequirementType type)
        {
            return type switch
            {
                RequirementType.DeadlyForceTraining => TimeSpan.FromDays(90),
                RequirementType.LautenbergActForm => TimeSpan.FromDays(365),
                RequirementType.AAEScreening => TimeSpan.FromDays(365),
                _ => TimeSpan.FromDays(365)
            };
        }

        private int GetWarningPeriod(RequirementType type)
        {
            return type switch
            {
                RequirementType.DeadlyForceTraining => 7, // 1 week
                RequirementType.LautenbergActForm => 30, // 30 days
                RequirementType.AAEScreening => 30, // 30 days
                _ => 30
            };
        }

        public string GetRequirementDescription(RequirementType type)
        {
            return type switch
            {
                RequirementType.DeadlyForceTraining => "Quarterly Deadly Force Training",
                RequirementType.LautenbergActForm => "Annual Lautenberg Act Form (NAVPERS 2760)",
                RequirementType.AAEScreening => "Annual AA&E Screening",
                _ => "Unknown Requirement"
            };
        }

        public string GetRequirementFrequency(RequirementType type)
        {
            return type switch
            {
                RequirementType.DeadlyForceTraining => "Quarterly (90 days)",
                RequirementType.LautenbergActForm => "Annual (365 days)",
                RequirementType.AAEScreening => "Annual (365 days)",
                _ => "Unknown"
            };
        }

        public bool IsForm2760Valid(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.Form2760Date == null)
                return false;

            var expirationDate = adminRequirements.Form2760Date.Value.AddYears(1);
            return DateTime.Today <= expirationDate;
        }

        public bool IsForm2760ExpiringSoon(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.Form2760Date == null)
                return false;

            var expirationDate = adminRequirements.Form2760Date.Value.AddYears(1);
            var warningDate = expirationDate.AddDays(-30);
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }

        public bool IsAAEScreeningValid(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.AAEScreeningDate == null)
                return false;

            var expirationDate = adminRequirements.AAEScreeningDate.Value.AddYears(1);
            return DateTime.Today <= expirationDate;
        }

        public bool IsAAEScreeningExpiringSoon(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.AAEScreeningDate == null)
                return false;

            var expirationDate = adminRequirements.AAEScreeningDate.Value.AddYears(1);
            var warningDate = expirationDate.AddDays(-30);
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }

        public bool IsDeadlyForceTrainingValid(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.DeadlyForceTrainingDate == null)
                return false;

            var expirationDate = adminRequirements.DeadlyForceTrainingDate.Value.AddMonths(3); // Quarterly
            return DateTime.Today <= expirationDate;
        }

        public bool IsDeadlyForceTrainingExpiringSoon(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.DeadlyForceTrainingDate == null)
                return false;

            var expirationDate = adminRequirements.DeadlyForceTrainingDate.Value.AddMonths(3);
            var warningDate = expirationDate.AddDays(-7); // 1 week warning
            return DateTime.Today >= warningDate && DateTime.Today <= expirationDate;
        }

        public bool IsAdminFullyQualified(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements == null)
                return false;

            return IsForm2760Valid(adminRequirements) &&
                   IsAAEScreeningValid(adminRequirements) &&
                   IsDeadlyForceTrainingValid(adminRequirements);
        }

        public int GetDaysUntilForm2760Expiration(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.Form2760Date == null)
                return -1;

            var expirationDate = adminRequirements.Form2760Date.Value.AddYears(1);
            return (expirationDate - DateTime.Today).Days;
        }

        public int GetDaysUntilAAEScreeningExpiration(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.AAEScreeningDate == null)
                return -1;

            var expirationDate = adminRequirements.AAEScreeningDate.Value.AddYears(1);
            return (expirationDate - DateTime.Today).Days;
        }

        public int GetDaysUntilDeadlyForceTrainingExpiration(AdditionalRequirements? adminRequirements)
        {
            if (adminRequirements?.DeadlyForceTrainingDate == null)
                return -1;

            var expirationDate = adminRequirements.DeadlyForceTrainingDate.Value.AddMonths(3);
            return (expirationDate - DateTime.Today).Days;
        }
    }
} 