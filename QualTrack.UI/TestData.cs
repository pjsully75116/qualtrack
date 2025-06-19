using QualTrack.Core.Models;
using QualTrack.Core.Services;
using QualTrack.Data.Database;
using QualTrack.Data.Repositories;

namespace QualTrack.UI
{
    /// <summary>
    /// Helper class for populating test data
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Populates the database with sample Navy personnel and qualifications
        /// </summary>
        public static async Task PopulateTestDataAsync()
        {
            using var dbContext = new DatabaseContext();
            var personnelRepo = new PersonnelRepository(dbContext);
            var qualificationRepo = new QualificationRepository(dbContext);
            var qualificationService = new QualificationService();

            // Sample personnel data
            var personnel = new[]
            {
                new Personnel("John Smith", "GM2") { DutySections = new List<(string, string)> { ("6", "1"), ("6", "2") } },
                new Personnel("Sarah Johnson", "BM1") { DutySections = new List<(string, string)> { ("3", "1"), ("3", "2") } },
                new Personnel("Michael Davis", "FC3") { DutySections = new List<(string, string)> { ("6", "3"), ("3", "2") } },
                new Personnel("Emily Wilson", "OS2") { DutySections = new List<(string, string)> { ("6", "4"), ("3", "3") } },
                new Personnel("David Brown", "QM1") { DutySections = new List<(string, string)> { ("6", "5"), ("6", "6") } }
            };

            // Add personnel
            foreach (var person in personnel)
            {
                person.Id = await personnelRepo.AddPersonnelAsync(person);
            }

            // Sample qualifications
            var qualifications = new[]
            {
                // Recent qualifications (still valid)
                new Qualification("M9", 1, DateTime.Today.AddDays(-30)) { PersonnelId = personnel[0].Id },
                new Qualification("M4/M16", 2, DateTime.Today.AddDays(-60)) { PersonnelId = personnel[0].Id },
                new Qualification("M9", 1, DateTime.Today.AddDays(-45)) { PersonnelId = personnel[1].Id },
                new Qualification("M240", 3, DateTime.Today.AddDays(-90)) { PersonnelId = personnel[2].Id },
                new Qualification("M2", 4, DateTime.Today.AddDays(-120)) { PersonnelId = personnel[3].Id },
                
                // Qualifications needing sustainment
                new Qualification("M500", 1, DateTime.Today.AddDays(-200)) { PersonnelId = personnel[1].Id },
                new Qualification("M249", 3, DateTime.Today.AddDays(-400)) { PersonnelId = personnel[2].Id },
                
                // Expired qualifications
                new Qualification("M9", 1, DateTime.Today.AddDays(-400)) { PersonnelId = personnel[4].Id },
                new Qualification("M4/M16", 2, DateTime.Today.AddDays(-450)) { PersonnelId = personnel[4].Id }
            };

            // Add qualifications
            foreach (var qualification in qualifications)
            {
                await qualificationRepo.AddQualificationAsync(qualification);
            }
        }

        /// <summary>
        /// Clears all test data from the database
        /// </summary>
        public static async Task ClearTestDataAsync()
        {
            using var dbContext = new DatabaseContext();
            var personnelRepo = new PersonnelRepository(dbContext);
            var qualificationRepo = new QualificationRepository(dbContext);

            // Get all personnel and delete them (this will cascade delete qualifications)
            var allPersonnel = await personnelRepo.GetAllPersonnelAsync();
            foreach (var person in allPersonnel)
            {
                await personnelRepo.DeletePersonnelAsync(person.Id);
            }
        }
    }
} 