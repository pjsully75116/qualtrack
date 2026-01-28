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
            var aaeFormRepo = new AAEScreeningFormRepository();

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

            // Define all weapon systems (representative shipboard sample)
            var weapons = new[] { "M9", "M4/M16", "M500", "M240", "M2", "M2A1" };

            // Define name pools for random sailors
            string[] firstNames =
            {
                "Michael", "David", "John", "James", "Robert", "William", "Richard", "Joseph", "Thomas", "Christopher",
                "Charles", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua",
                "Kevin", "Brian", "Eric", "Adam", "Justin", "Ryan", "Kyle", "Brandon", "Sean", "Jason",
                "Nicholas", "Tyler", "Zachary", "Aaron", "Jeremy", "Jordan", "Austin", "Logan", "Ethan", "Noah",
                "Samantha", "Jessica", "Emily", "Sarah", "Ashley", "Lauren", "Brittany", "Megan", "Rachel", "Kayla"
            };
            string[] lastNames =
            {
                "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
                "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
                "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
                "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
                "Green", "Adams", "Baker", "Nelson", "Carter", "Mitchell", "Perez", "Roberts", "Turner", "Phillips"
            };
            string[] rates = { "BM", "GM", "OS", "QM", "ET", "FC", "IT", "EN", "DC", "HM", "MA" };
            string[] ranks = { "E-2", "E-3", "E-4", "E-5", "E-6", "E-7" };

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Create 50 sailors: keep Cole & Coen, plus 48 random
            for (int i = 1; i <= 50; i++)
            {
                string lastName, firstName, dodId, rate, rank;
                List<(string, string)> dutySections;

                if (i == 1)
                {
                    lastName = "Cole";
                    firstName = "Jermaine";
                    dodId = "0100000001";
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
                else
                {
                    string nameKey;
                    do
                    {
                        firstName = firstNames[rand.Next(firstNames.Length)];
                        lastName = lastNames[rand.Next(lastNames.Length)];
                        nameKey = $"{lastName},{firstName}";
                    } while (usedNames.Contains(nameKey) || nameKey.Equals("Cole,Jermaine", StringComparison.OrdinalIgnoreCase)
                             || nameKey.Equals("Coen,Patrick", StringComparison.OrdinalIgnoreCase));

                    usedNames.Add(nameKey);
                    dodId = $"010000{(i + 1000):D4}";
                    rate = rates[rand.Next(rates.Length)] + rand.Next(1, 4);
                    rank = ranks[rand.Next(ranks.Length)];
                    dutySections = new List<(string, string)>
                    {
                        ("3", rand.Next(1, 4).ToString()),
                        ("6", rand.Next(1, 7).ToString())
                    };
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
                    // Each sailor gets 1-4 random weapons
                    sailorWeapons = weapons.OrderBy(x => rand.Next()).Take(rand.Next(1, 5)).ToList();
                    
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
                DateTime? aaeCompletedDate = null;

                if (idx == 0 || idx == 1) // Cole, Jermaine (index 0) and Coen, Patrick (index 1)
                {
                    adminRequirements.Form2760Date = today.AddDays(-30); // 30 days ago
                    adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                    adminRequirements.Form2760SignedDate = today.AddDays(-30);
                    adminRequirements.Form2760Witness = "Chief Smith";

                    aaeCompletedDate = today.AddDays(-60); // 60 days ago

                    adminRequirements.DeadlyForceTrainingDate = today.AddDays(-45); // 45 days ago
                    adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                    adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                }
                else
                {
                    // Majority current admin, some partial, some expired/missing
                    var adminRoll = rand.Next(100);

                    if (adminRoll < 70) // 70% fully current
                    {
                        adminRequirements.Form2760Date = today.AddDays(-rand.Next(1, 200));
                        adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                        adminRequirements.Form2760SignedDate = adminRequirements.Form2760Date;
                        adminRequirements.Form2760Witness = "Chief Smith";

                        aaeCompletedDate = today.AddDays(-rand.Next(1, 200));

                        adminRequirements.DeadlyForceTrainingDate = today.AddDays(-rand.Next(1, 80));
                        adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                        adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                    }
                    else if (adminRoll < 90) // 20% partial admin
                    {
                        if (rand.Next(100) < 70)
                        {
                            adminRequirements.Form2760Date = today.AddDays(-rand.Next(1, 365));
                            adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                            adminRequirements.Form2760SignedDate = adminRequirements.Form2760Date;
                            adminRequirements.Form2760Witness = "Chief Smith";
                        }
                        if (rand.Next(100) < 50)
                        {
                            aaeCompletedDate = today.AddDays(-rand.Next(1, 365));
                        }
                        if (rand.Next(100) < 50)
                        {
                            adminRequirements.DeadlyForceTrainingDate = today.AddDays(-rand.Next(1, 120));
                            adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                            adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                        }
                    }
                    else // 10% expired
                    {
                        adminRequirements.Form2760Date = today.AddDays(-rand.Next(370, 600));
                        adminRequirements.Form2760Number = $"2760-{sailor.DODId}";
                        adminRequirements.Form2760SignedDate = adminRequirements.Form2760Date;
                        adminRequirements.Form2760Witness = "Chief Smith";

                        aaeCompletedDate = today.AddDays(-rand.Next(370, 600));

                        adminRequirements.DeadlyForceTrainingDate = today.AddDays(-rand.Next(120, 200));
                        adminRequirements.DeadlyForceInstructor = "Gunny Johnson";
                        adminRequirements.DeadlyForceRemarks = "Quarterly training completed";
                    }
                }

                // Save admin requirements if any exist
                if (adminRequirements.Form2760Date.HasValue || adminRequirements.DeadlyForceTrainingDate.HasValue)
                {
                    await additionalRequirementsRepo.SaveAsync(sailor.Id, adminRequirements);
                }

                if (aaeCompletedDate.HasValue)
                {
                    var completedDate = aaeCompletedDate.Value.Date;
                    var aaeForm = new AAEScreeningForm
                    {
                        PersonnelId = sailor.Id,
                        DateCompleted = completedDate,
                        DateExpires = completedDate.AddYears(1),
                        DateCreated = today,
                        NameScreened = $"{sailor.LastName}, {sailor.FirstName}",
                        RankScreened = sailor.Rank,
                        DODIDScreened = sailor.DODId,
                        NameScreener = "Cole",
                        RankScreener = "E-6",
                        DODIDScreener = "0100000001",
                        Question1Response = "N",
                        Question2Response = "N",
                        Question3Response = "N",
                        Question4Response = "N",
                        Question5Response = "N",
                        Question6Response = "N",
                        Question7Response = "N",
                        Qualified = true,
                        Unqualified = false,
                        ReviewLater = false,
                        IsValid = true
                    };

                    await aaeFormRepo.AddAsync(dbContext, aaeForm);
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