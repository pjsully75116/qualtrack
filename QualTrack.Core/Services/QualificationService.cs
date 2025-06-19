using QualTrack.Core.Models;

namespace QualTrack.Core.Services
{
    /// <summary>
    /// Service for evaluating qualification status based on OPNAVINST 3591.1G rules
    /// </summary>
    public class QualificationService
    {
        /// <summary>
        /// Evaluates qualification status based on OPNAVINST 3591.1G rules for Cat I–IV
        /// </summary>
        /// <param name="dateQualified">Date when qualification was achieved</param>
        /// <param name="category">Category I (1) through IV (4)</param>
        /// <param name="evaluationDate">Date to evaluate against (defaults to today)</param>
        /// <returns>QualificationStatus object with evaluation results</returns>
        public QualificationStatus EvaluateQualification(DateTime dateQualified, int category, DateTime? evaluationDate = null)
        {
            var today = evaluationDate ?? DateTime.Today;
            var status = new QualificationStatus();

            // Validate category
            if (category < 1 || category > 4)
            {
                throw new ArgumentException("Category must be between 1 and 4", nameof(category));
            }

            // Calculate validity periods based on category
            var (fullValidityDays, sustainmentDays) = GetValidityPeriods(category);
            
            var fullValidityDate = dateQualified.AddDays(fullValidityDays);
            var sustainmentDueDate = dateQualified.AddDays(sustainmentDays);

            // Calculate status
            status.ExpiresOn = fullValidityDate;
            status.DaysUntilExpiration = (fullValidityDate - today).Days;
            status.DaysUntilSustainment = (sustainmentDueDate - today).Days;

            // Determine qualification status
            if (today > fullValidityDate)
            {
                status.IsDisqualified = true;
                status.IsQualified = false;
                status.SustainmentDue = false;
            }
            else if (today >= sustainmentDueDate && today <= fullValidityDate)
            {
                status.SustainmentDue = true;
                status.IsQualified = true;
                status.IsDisqualified = false;
            }
            else
            {
                status.IsQualified = true;
                status.SustainmentDue = false;
                status.IsDisqualified = false;
            }

            return status;
        }

        /// <summary>
        /// Gets validity periods in days for each category based on OPNAVINST 3591.1G
        /// </summary>
        /// <param name="category">Qualification category (1-4)</param>
        /// <returns>Tuple of (full validity days, sustainment due days)</returns>
        private (int fullValidityDays, int sustainmentDays) GetValidityPeriods(int category)
        {
            return category switch
            {
                1 => (365, 180),  // CAT I: 1 year full validity, 6 months sustainment
                2 => (365, 180),  // CAT II: 1 year full validity, 6 months sustainment
                3 => (730, 365),  // CAT III: 2 years full validity, 1 year sustainment
                4 => (1095, 547), // CAT IV: 3 years full validity, 1.5 years sustainment
                _ => throw new ArgumentException("Invalid category", nameof(category))
            };
        }

        /// <summary>
        /// Gets a list of allowed weapons per OPNAVINST 3591.1G
        /// </summary>
        /// <returns>List of allowed weapon types</returns>
        public List<string> GetAllowedWeapons()
        {
            return new List<string>
            {
                "M9",
                "M4/M16", 
                "M500",
                "M240",
                "M249",
                "M2"
            };
        }

        /// <summary>
        /// Validates if a weapon is in the allowed list
        /// </summary>
        /// <param name="weapon">Weapon to validate</param>
        /// <returns>True if weapon is allowed</returns>
        public bool IsWeaponAllowed(string weapon)
        {
            return GetAllowedWeapons().Contains(weapon, StringComparer.OrdinalIgnoreCase);
        }
    }
} 