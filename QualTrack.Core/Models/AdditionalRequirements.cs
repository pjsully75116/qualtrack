using System.ComponentModel.DataAnnotations;

namespace QualTrack.Core.Models
{
    public enum RequirementType
    {
        DeadlyForceTraining,
        LautenbergActForm,
        AAEScreening
    }

    public enum RequirementStatus
    {
        NotRequired,
        Required,
        Completed,
        Expired,
        Overdue
    }

    public class AdditionalRequirement
    {
        public int Id { get; set; }
        public int PersonnelId { get; set; }
        public RequirementType Type { get; set; }
        public RequirementStatus Status { get; set; }
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateExpires { get; set; }
        public DateTime? DateRequired { get; set; }
        public string? Instructor { get; set; }
        public string? Remarks { get; set; }
        public string? DocumentPath { get; set; } // Path to stored form/document
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Navigation property
        public Personnel? Personnel { get; set; }
    }

    public class LautenbergActForm : AdditionalRequirement
    {
        public LautenbergActForm()
        {
            Type = RequirementType.LautenbergActForm;
        }

        // Annual requirement
        public static TimeSpan ValidityPeriod => TimeSpan.FromDays(365); // 1 year
        public static TimeSpan WarningPeriod => TimeSpan.FromDays(60); // 60 days warning
        
        public string? FormNumber { get; set; } // NAVPERS 2760
        public bool IsSigned { get; set; }
        public DateTime? DateSigned { get; set; }
        public string? WitnessName { get; set; }
    }

    public class AdditionalRequirementStatus
    {
        public RequirementType Type { get; set; }
        public RequirementStatus Status { get; set; }
        public int DaysUntilExpiration { get; set; }
        public int DaysUntilRequired { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsWarning { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container class for all admin requirements for a personnel member
    /// </summary>
    public class AdditionalRequirements
    {
        public int PersonnelId { get; set; }
        
        // Form 2760 (Lautenberg Act Form) - Annual requirement
        public DateTime? Form2760Date { get; set; }
        public string? Form2760Number { get; set; }
        public DateTime? Form2760SignedDate { get; set; }
        public string? Form2760Witness { get; set; }
        
        // AA&E Screening - Annual requirement
        public DateTime? AAEScreeningDate { get; set; }
        public string? AAEScreeningLevel { get; set; }
        public string? AAEInvestigationType { get; set; }
        public DateTime? AAEInvestigationDate { get; set; }
        public string? AAEInvestigationAgency { get; set; }
        
        // Deadly Force Training - Quarterly requirement
        public DateTime? DeadlyForceTrainingDate { get; set; }
        public string? DeadlyForceInstructor { get; set; }
        public string? DeadlyForceRemarks { get; set; }
        
        // Navigation property
        public Personnel? Personnel { get; set; }
    }
} 