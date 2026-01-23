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
            dbContext.InitializeDatabase();
            
            var personnelRepo = new PersonnelRepository();
            var qualificationRepo = new QualificationRepository();
            var qualificationService = new QualificationService();
            var additionalRequirementsRepo = new AdditionalRequirementsRepository(dbContext);

            // Check if database already has data
            var existingPersonnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
            if (existingPersonnel.Any())
            {
                throw new InvalidOperationException("Database already contains data. Please clear the database first using the 'Clear Database' button.");
            }

            var testSailors = new List<Personnel>();
            var today = DateTime.Today;
            var oneWeekAgo = today.AddDays(-7);
            var rand = new Random();

            // Define all weapon systems
            var weapons = new[] { "M9", "M4/M16", "M500", "M240", "M2" };

            // Create 21 sailors with specific names
            for (int i = 1; i <= 21; i++)
            {
                string lastName, firstName, dodId, rate, rank;
                List<(string, string)> dutySections;
                
                // Define arrays of common names
                string[] firstNames = { "Michael", "David", "John", "James", "Robert", "William", "Richard", "Joseph", "Thomas", "Christopher", 
                                       "Charles", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua", "Kevin" };
                string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
                                      "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee" };
                
                if (i == 1)
                {
                    lastName = "Cole";
                    firstName = "Jermaine";
                    dodId = $"0100000001"; // 10 digits
                    rate = "GM1";
                    rank = "E-6";
                    dutySections = new List<(string, string)> { ("3", "1"), ("6", "1") };
                }
                else if (i == 2)
                {
                    lastName = "Coen";
                    firstName = "Patrick";
                    dodId = "1501025301";
                    rate = "GM2";
                    rank = "E-5";
                    dutySections = new List<(string, string)> { ("3", "2"), ("6", "2") };
                }
                else if (i == 21)
                {
                    lastName = "Harper";
                    firstName = "Elise";
                    dodId = "0100000021";
                    rate = "ENS";
                    rank = "O-1";
                    dutySections = new List<(string, string)> { ("3", "1"), ("6", "1") };
                }
                else
                {
                    // Use unique common names for the remaining 18 sailors
                    int nameIndex = i - 3; // Start from index 0 for the name arrays
                    firstName = firstNames[nameIndex];
                    lastName = lastNames[nameIndex];
                    dodId = $"10000000{i:D2}";
                    string[] ranks = { "E-1", "E-2", "E-3", "E-4", "E-5", "E-6", "E-7", "E-8", "E-9" };
                    rank = ranks[(i - 1) % ranks.Length];
                    string rateSuffix = ((i - 1) % 3 + 1).ToString();
                    rate = $"GM{rateSuffix}";
                    dutySections = new List<(string, string)> { ("3", ((i - 1) % 3 + 1).ToString()), ("6", ((i - 1) % 6 + 1).ToString()) };
                }

                var sailor = new Personnel(lastName, firstName, rate, rank)
                {
                    DODId = dodId,
                    DutySections = dutySections
                };
                sailor.Id = await personnelRepo.AddPersonnelAsync(dbContext, sailor);
                testSailors.Add(sailor);
            }

            // Create qualifications with various statuses
            foreach (var (sailor, idx) in testSailors.Select((s, i) => (s, i)))
            {
                if (idx == 20) // Harper, Elise: no weapons qual data
                    continue;

                List<string> sailorWeapons;
                DateTime qualDate;

                // Special handling for Cole, Jermaine and Coen, Patrick
                if (idx == 0 || idx == 1) // Cole, Jermaine (index 0) and Coen, Patrick (index 1)
                {
                    sailorWeapons = weapons.ToList(); // All 5 weapons
                    qualDate = oneWeekAgo; // Exactly one week ago
                }
                else
                {
                    // Each sailor gets 1-3 random weapons
                    sailorWeapons = weapons.OrderBy(x => rand.Next()).Take(rand.Next(1, 4)).ToList();
                    
                    // Create different qualification statuses
                    // 40% current, 30% sustainment due, 30% expired
                    var statusRoll = rand.Next(100);
                    
                    if (statusRoll < 40) // Current qualifications
                    {
                        qualDate = today.AddDays(-rand.Next(1, 120)); // Within 120 days
                    }
                    else if (statusRoll < 70) // Sustainment due
                    {
                        qualDate = today.AddDays(-rand.Next(120, 240)); // 120-240 days ago
                    }
                    else // Expired
                    {
                        qualDate = today.AddDays(-rand.Next(366, 500)); // Over 1 year ago
                    }
                }
                
                foreach (var weapon in sailorWeapons)
                {
                    // Create weapon-specific details
                    var details = weapon switch
                    {
                        "M9" => new QualificationDetails 
                        { 
                            NHQCScore = 180 + rand.Next(0, 61), // 180-240
                            HLLCScore = 12 + rand.Next(0, 7),   // 12-18
                            HPWCScore = 12 + rand.Next(0, 7),   // 12-18
                            Instructor = "Cole", // Always Cole for SAMI
                            Remarks = "Test qualification"
                        },
                        "M4/M16" => new QualificationDetails 
                        { 
                            RQCScore = 140 + rand.Next(0, 61),  // 140-200
                            RLCScore = 14 + rand.Next(0, 7),    // 14-20
                            Instructor = "Cole", // Always Cole for SAMI
                            Remarks = "Test qualification"
                        },
                        "M500" => new QualificationDetails 
                        { 
                            SPWCScore = 90 + rand.Next(0, 73),  // 90-162
                            Instructor = "Cole", // Always Cole for SAMI
                            Remarks = "Test qualification"
                        },
                        "M240" => new QualificationDetails 
                        { 
                            COFScore = 100 + rand.Next(0, 50),  // 100-150
                            CSWI = "Jermaine", // Always Jermaine for CSWI
                            Remarks = "Test qualification"
                        },
                        "M2" => new QualificationDetails 
                        { 
                            COFScore = 100 + rand.Next(0, 50),  // 100-150
                            CSWI = "Jermaine", // Always Jermaine for CSWI
                            Remarks = "Test qualification"
                        },
                        _ => new QualificationDetails 
                        { 
                            Instructor = "Cole",
                            Remarks = "Test qualification"
                        }
                    };

                    // Add sustainment for some current qualifications (20% chance, but not for Cole/Coen)
                    if (idx > 1 && qualDate > today.AddDays(-120) && rand.Next(100) < 20)
                    {
                        details.SustainmentDate = qualDate.AddDays(120 + rand.Next(0, 120));
                        // Generate appropriate sustainment scores for each weapon type
                        details.SustainmentScore = weapon switch
                        {
                            "M9" => 180 + rand.Next(0, 61),      // 180-240
                            "M4/M16" => 140 + rand.Next(0, 61),  // 140-200
                            "M500" => 90 + rand.Next(0, 73),     // 90-162
                            "M240" => 100 + rand.Next(0, 50),    // 100-150
                            "M2" => 100 + rand.Next(0, 50),      // 100-150
                            _ => 100 + rand.Next(0, 50)          // Default fallback
                        };
                    }

                    var qual = new Qualification(weapon, 2, qualDate) 
                    { 
                        PersonnelId = sailor.Id, 
                        Details = details 
                    };
                    await qualificationRepo.AddQualificationAsync(dbContext, qual);
                }
            }

            // Add admin requirements
            foreach (var (sailor, idx) in testSailors.Select((s, i) => (s, i)))
            {
                var adminRequirements = new AdditionalRequirements
                {
                    PersonnelId = sailor.Id
                };

                // Special handling for Cole, Jermaine, Coen, Patrick, and Harper, Elise - fully qualified
                if (idx == 0 || idx == 1 || idx == 20) // Cole, Jermaine (index 0), Coen, Patrick (index 1), and Harper, Elise (index 20)
                {
                    if (idx == 20) // Harper, Elise: all admin documents valid as of today
                    {
                        adminRequirements.Form2760Date = today; // Today
                        adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                        adminRequirements.Form2760SignedDate = today;
                        adminRequirements.Form2760Witness = "Chief Smith";

                        adminRequirements.AAEScreeningDate = today; // Today
                        adminRequirements.AAEScreeningLevel = "Secret";
                        adminRequirements.AAEInvestigationType = "T5";
                        adminRequirements.AAEInvestigationDate = today;
                        adminRequirements.AAEInvestigationAgency = "DCSA";

                        adminRequirements.DeadlyForceTrainingDate = today; // Today
                        adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                        adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                    }
                    else // Cole, Jermaine and Coen, Patrick: existing logic
                    {
                        adminRequirements.Form2760Date = today.AddDays(-30); // 30 days ago
                        adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                        adminRequirements.Form2760SignedDate = today.AddDays(-30);
                        adminRequirements.Form2760Witness = "Chief Smith";

                        adminRequirements.AAEScreeningDate = today.AddDays(-60); // 60 days ago
                        adminRequirements.AAEScreeningLevel = "Secret";
                        adminRequirements.AAEInvestigationType = "T5";
                        adminRequirements.AAEInvestigationDate = today.AddDays(-90);
                        adminRequirements.AAEInvestigationAgency = "DCSA";

                        adminRequirements.DeadlyForceTrainingDate = today.AddDays(-45); // 45 days ago
                        adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                        adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                    }
                }
                else
                {
                    // Random admin requirements for other sailors
                    var adminRoll = rand.Next(100);
                    
                    if (adminRoll < 30) // 30% have Form 2760
                    {
                        adminRequirements.Form2760Date = today.AddDays(-rand.Next(1, 365));
                        adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                        adminRequirements.Form2760SignedDate = today.AddDays(-rand.Next(1, 365));
                        adminRequirements.Form2760Witness = "Chief Smith";
                    }
                    
                    if (adminRoll < 25) // 25% have AA&E screening
                    {
                        adminRequirements.AAEScreeningDate = today.AddDays(-rand.Next(1, 365));
                        adminRequirements.AAEScreeningLevel = rand.Next(100) < 70 ? "Secret" : "Top Secret";
                        adminRequirements.AAEInvestigationType = rand.Next(100) < 80 ? "T5" : "SSBI";
                        adminRequirements.AAEInvestigationDate = today.AddDays(-rand.Next(1, 365));
                        adminRequirements.AAEInvestigationAgency = "DCSA";
                    }
                    
                    if (adminRoll < 40) // 40% have Deadly Force Training
                    {
                        adminRequirements.DeadlyForceTrainingDate = today.AddDays(-rand.Next(1, 90)); // Within 90 days
                        adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                        adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                    }
                }

                // Save admin requirements if any exist
                if (adminRequirements.Form2760Date.HasValue || adminRequirements.AAEScreeningDate.HasValue || adminRequirements.DeadlyForceTrainingDate.HasValue)
                {
                    await additionalRequirementsRepo.SaveAsync(sailor.Id, adminRequirements);
                }
            }
        }

        /// <summary>
        /// Clears all test data from the database
        /// </summary>
        public static async Task ClearTestDataAsync()
        {
            using var dbContext = new DatabaseContext();
            dbContext.InitializeDatabase();
            
            var personnelRepo = new PersonnelRepository();
            var qualificationRepo = new QualificationRepository();

            // Get all personnel and delete them (this will cascade delete qualifications)
            var allPersonnel = await personnelRepo.GetAllPersonnelAsync(dbContext);
            foreach (var personnel in allPersonnel)
            {
                await personnelRepo.DeletePersonnelAsync(dbContext, personnel.Id);
            }
        }
    }
} 