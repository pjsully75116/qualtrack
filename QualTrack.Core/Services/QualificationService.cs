// TODO: FUTURE FEATURE - Deployment/At-Sea Modes
// -----------------------------------------------
// OPNAVINST 3591.1G allows a command to enter 'Deployment' or 'At-Sea' mode.
// In these modes, qualification rules and due dates may be adjusted (e.g., extended windows, alternate courses of fire).
// This will require a system-wide mode toggle and logic to adjust qualification status calculations.
// More details to follow as requirements are clarified.

using QualTrack.Core.Models;
using System.Collections.Concurrent;

namespace QualTrack.Core.Services
{
    /// <summary>
    /// Service for evaluating qualification status based on OPNAVINST 3591.1G rules
    /// </summary>
    public class QualificationService
    {
        // Cache for qualification status calculations to avoid redundant work
        private readonly ConcurrentDictionary<string, QualificationStatus> _statusCache = new ConcurrentDictionary<string, QualificationStatus>();
        private readonly object _cacheLock = new object();
        
        // Cache expiration time (5 minutes)
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// Evaluates qualification status based on OPNAVINST 3591.1G rules for Cat I–IV
        /// </summary>
        /// <param name="dateQualified">Date when qualification was achieved</param>
        /// <param name="category">Category I (1) through IV (4)</param>
        /// <param name="details">Details of the qualification</param>
        /// <param name="evaluationDate">Date to evaluate against (defaults to today)</param>
        /// <param name="weapon">Weapon type for score validation</param>
        /// <returns>QualificationStatus object with evaluation results</returns>
        public QualificationStatus EvaluateQualification(DateTime dateQualified, int category, QualificationDetails? details = null, DateTime? evaluationDate = null, string? weapon = null)
        {
            var today = evaluationDate ?? DateTime.Today;
            
            // Create cache key
            var cacheKey = CreateCacheKey(dateQualified, category, details, today, weapon);
            
            // Check cache first
            if (_statusCache.TryGetValue(cacheKey, out var cachedStatus))
            {
                if (_cacheTimestamps.TryGetValue(cacheKey, out var timestamp))
                {
                    if (DateTime.Now - timestamp < _cacheExpiration)
                    {
                        return cachedStatus;
                    }
                    else
                    {
                        // Remove expired cache entry
                        _statusCache.TryRemove(cacheKey, out _);
                        _cacheTimestamps.TryRemove(cacheKey, out _);
                    }
                }
            }
            
            // Calculate new status
            var status = CalculateQualificationStatus(dateQualified, category, details, today, weapon);
            
            // Cache the result
            _statusCache.TryAdd(cacheKey, status);
            _cacheTimestamps.TryAdd(cacheKey, DateTime.Now);
            
            return status;
        }

        /// <summary>
        /// Creates a cache key for qualification status
        /// </summary>
        private string CreateCacheKey(DateTime dateQualified, int category, QualificationDetails? details, DateTime evaluationDate, string? weapon)
        {
            var detailsHash = details != null ? 
                $"{details.HQCScore}_{details.NHQCScore}_{details.HLLCScore}_{details.HPWCScore}_{details.RQCScore}_{details.RLCScore}_{details.SPWCScore}_{details.COFScore}_{details.SustainmentDate}_{details.SustainmentScore}" : 
                "null";
            
            return $"{dateQualified:yyyy-MM-dd}_{category}_{detailsHash}_{evaluationDate:yyyy-MM-dd}_{weapon ?? "null"}";
        }

        /// <summary>
        /// Calculates qualification status (moved from EvaluateQualification for caching)
        /// </summary>
        private QualificationStatus CalculateQualificationStatus(DateTime dateQualified, int category, QualificationDetails? details, DateTime today, string? weapon)
        {
            var status = new QualificationStatus();
            if (category < 1 || category > 4)
            {
                throw new ArgumentException("Category must be between 1 and 4", nameof(category));
            }

            // First, check if scores are passing (if details are provided)
            if (details != null && !string.IsNullOrEmpty(weapon))
            {
                bool scoresPassing = ValidateScores(weapon, category, details);
                if (!scoresPassing)
                {
                    // If scores are failing, qualification is disqualified regardless of date
                    status.IsDisqualified = true;
                    status.IsQualified = false;
                    status.SustainmentDue = false;
                    status.ExpiresOn = dateQualified.Date; // Expired immediately
                    status.DaysUntilExpiration = 0;
                    status.DaysUntilSustainment = 0;
                    return status;
                }
            }

            // If qualification is for today, always valid (assuming scores passed above)
            if (today == dateQualified.Date)
            {
                status.IsQualified = true;
                status.IsDisqualified = false;
                status.SustainmentDue = false;
                status.ExpiresOn = dateQualified.Date.AddDays((details != null && details.SustainmentDate.HasValue) ? 365 : 240);
                status.DaysUntilExpiration = (status.ExpiresOn - today).Days;
                status.DaysUntilSustainment = (dateQualified.Date.AddDays(120) - today).Days;
                return status;
            }

            // Use sustainment logic: 365 days if sustained, 240 if not
            int sustainmentWindow = (details != null && details.SustainmentDate.HasValue) ? 365 : 240;
            var baseDate = dateQualified.Date.AddDays(sustainmentWindow);
            var lapseDate = new DateTime(baseDate.Year, baseDate.Month, 1).AddMonths(1);
            status.ExpiresOn = lapseDate;
            status.DaysUntilExpiration = (lapseDate - today).Days;
            // Sustainment window: 120-240 days after qualification
            var sustainmentStart = dateQualified.Date.AddDays(120);
            var sustainmentEnd = dateQualified.Date.AddDays(240);
            status.DaysUntilSustainment = (sustainmentStart - today).Days;
            // Status logic
            if (today >= lapseDate)
            {
                status.IsDisqualified = true;
                status.IsQualified = false;
                status.SustainmentDue = false;
            }
            else if (today >= sustainmentStart && today <= sustainmentEnd && (details == null || !details.SustainmentDate.HasValue))
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
        /// Clears the status cache (useful for testing or when data changes)
        /// </summary>
        public void ClearCache()
        {
            _statusCache.Clear();
            _cacheTimestamps.Clear();
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public (int CacheSize, int ExpiredEntries) GetCacheStats()
        {
            var expiredCount = _cacheTimestamps.Count(kvp => DateTime.Now - kvp.Value >= _cacheExpiration);
            return (_statusCache.Count, expiredCount);
        }

        /// <summary>
        /// Validates if the scores meet passing criteria for the given weapon and category
        /// </summary>
        /// <param name="weapon">Weapon type</param>
        /// <param name="category">Qualification category</param>
        /// <param name="details">Qualification details with scores</param>
        /// <returns>True if all required scores are passing</returns>
        // TODO: OPNAVINST 3591.1G SUSTAINMENT COMPLIANCE
        // For M4/M16 sustainment, instruction requires 30 hits (not a score). Current logic uses full qual score (140-200).
        // For M500 sustainment, instruction requires live-fire of the SPWC. Current logic uses full qual score (90-162).
        // Revisit for strict compliance in future versions. Not MVP priority.
        private bool ValidateScores(string weapon, int category, QualificationDetails details)
        {
            return weapon switch
            {
                "M9" when category == 1 => details.HQCPass,
                "M9" when category == 2 => details.OverallHandgunPass,
                "M4/M16" when category == 2 => details.OverallRiflePass,
                "M500" when category == 2 => details.SPWCPass,
                "M240" when category == 2 => details.COFPass,
                "M2" when category == 2 => details.COFPass,
                _ => true // Unknown weapon/category combination, assume passing
            };
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