using System;
using QualTrack.Core.Models;
using QualTrack.Core.Services;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;

namespace QualTrack.UI.Services
{
    /// <summary>
    /// Service for handling crew served weapon qualifications (M240, M2) using 3591/2 forms
    /// </summary>
    public class CrewServedWeaponService
    {
        private readonly QualificationService _qualificationService;
        private readonly ICrewServedWeaponSessionRepository _sessionRepository;
        private readonly IQualificationRepository _qualificationRepository;
        private readonly IRbacService? _rbacService;
        private readonly ICurrentUserContext? _currentUserContext;

        public CrewServedWeaponService(
            QualificationService qualificationService,
            ICrewServedWeaponSessionRepository sessionRepository,
            IQualificationRepository qualificationRepository,
            IRbacService? rbacService = null,
            ICurrentUserContext? currentUserContext = null)
        {
            _qualificationService = qualificationService;
            _sessionRepository = sessionRepository;
            _qualificationRepository = qualificationRepository;
            _rbacService = rbacService;
            _currentUserContext = currentUserContext;
        }

        /// <summary>
        /// Validates a crew served weapon qualification score
        /// M240 and M2 require Course of Fire (COF) score >= 100
        /// </summary>
        /// <param name="weapon">Weapon type ("M240" or "M2")</param>
        /// <param name="cofScore">Course of Fire score</param>
        /// <returns>True if score is passing</returns>
        public bool ValidateCrewServedWeaponScore(string weapon, int? cofScore)
        {
            if (!cofScore.HasValue)
                return false;

            // Both M240 and M2 require COF score >= 100 per OPNAVINST 3591.1G
            return cofScore.Value >= 100;
        }

        /// <summary>
        /// Gets the correct category for a crew served weapon
        /// M240 = Category III (1 year validity)
        /// M2/M2A1 = Category IV (1 year validity)
        /// </summary>
        /// <param name="weapon">Weapon type</param>
        /// <returns>Category number (3 for M240, 4 for M2/M2A1)</returns>
        public int GetCategoryForWeapon(string weapon)
        {
            return weapon.ToUpper() switch
            {
                "M240" => 3,   // Category III
                "M2" => 4,     // Category IV
                "M2A1" => 4,   // Category IV (M2A1 is variant of M2)
                _ => throw new ArgumentException($"Unknown crew served weapon: {weapon}")
            };
        }

        /// <summary>
        /// Creates a qualification from a crew served weapon session
        /// </summary>
        /// <param name="dbContext">Database context</param>
        /// <param name="session">Crew served weapon session</param>
        /// <param name="personnelId">Personnel ID for the gunner (primary qualifier)</param>
        /// <returns>Created qualification ID</returns>
        public async Task<int> CreateQualificationFromSessionAsync(
            DatabaseContext dbContext,
            CrewServedWeaponSession session,
            int personnelId)
        {
            EnsurePermission(RbacPermission.ManageCrewServed, "Create crew-served qualification");

            if (!session.IsQualified || !session.CourseOfFireScore.HasValue)
            {
                throw new InvalidOperationException("Cannot create qualification from unqualified session");
            }

            var category = GetCategoryForWeapon(session.Weapon);
            var qualification = new Qualification
            {
                PersonnelId = personnelId,
                Weapon = session.Weapon,
                Category = category,
                DateQualified = session.DateOfFiring ?? DateTime.Now,
                CrewServedWeaponSessionId = session.Id,
                Details = new QualificationDetails
                {
                    COFScore = session.CourseOfFireScore,
                    Instructor = session.InstructorName ?? string.Empty,
                    Remarks = $"Gunner: {session.GunnerName}, Assistant: {session.AssistantGunnerName}"
                }
            };

            return await _qualificationRepository.AddQualificationAsync(dbContext, qualification);
        }

        private void EnsurePermission(RbacPermission permission, string action)
        {
            if (_rbacService == null || _currentUserContext == null)
            {
                return;
            }

            if (!_rbacService.HasPermission(_currentUserContext.Role, permission))
            {
                throw new UnauthorizedAccessException($"Access denied for action: {action} (role: {_currentUserContext.Role}).");
            }
        }

        /// <summary>
        /// Evaluates qualification status for a crew served weapon
        /// Uses QualificationService with correct category (III for M240, IV for M2)
        /// </summary>
        /// <param name="qualification">Qualification to evaluate</param>
        /// <param name="evaluationDate">Date to evaluate against (defaults to today)</param>
        /// <returns>QualificationStatus</returns>
        public QualificationStatus EvaluateQualificationStatus(
            Qualification qualification,
            DateTime? evaluationDate = null)
        {
            return _qualificationService.EvaluateQualification(
                qualification.DateQualified,
                qualification.Category,
                qualification.Details,
                evaluationDate,
                qualification.Weapon);
        }
    }
}
