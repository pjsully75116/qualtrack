using System.Data.SQLite;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Implementation of personnel data access operations
    /// </summary>
    public class PersonnelRepository : IPersonnelRepository
    {
        public async Task<List<Personnel>> GetAllPersonnelAsync(DatabaseContext dbContext)
        {
            var personnel = new List<Personnel>();
            
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, dod_id, last_name, first_name, rate, rank, is_sami, is_cswi FROM personnel ORDER BY last_name, first_name";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var person = new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate")),
                    Rank = reader.GetString(reader.GetOrdinal("rank")),
                    IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                    IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1
                };
                
                // Load duty sections for this personnel
                person.DutySections = await GetDutySectionsForPersonnelAsync(dbContext, person.Id);
                
                personnel.Add(person);
            }
            
            return personnel;
        }

        public async Task<Personnel?> GetPersonnelByIdAsync(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, dod_id, last_name, first_name, rate, rank, is_sami, is_cswi
                FROM personnel 
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate")),
                    Rank = reader.GetString(reader.GetOrdinal("rank")),
                    IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                    IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1
                };
            }
            return null;
        }

        public async Task<Personnel?> GetPersonnelByDODIdAsync(DatabaseContext dbContext, string dodId)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, dod_id, last_name, first_name, rate, rank, is_sami, is_cswi
                FROM personnel 
                WHERE dod_id = @dodId";
            command.Parameters.AddWithValue("@dodId", dodId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate")),
                    Rank = reader.GetString(reader.GetOrdinal("rank")),
                    IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                    IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1
                };
            }
            return null;
        }

        public async Task<Personnel?> GetPersonnelByNameAndRateAsync(DatabaseContext dbContext, string lastName, string firstName, string rate)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, dod_id, last_name, first_name, rate, rank, is_sami, is_cswi FROM personnel WHERE last_name = @lastName AND first_name = @firstName AND rate = @rate";
            command.Parameters.AddWithValue("@lastName", lastName);
            command.Parameters.AddWithValue("@firstName", firstName);
            command.Parameters.AddWithValue("@rate", rate);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var personnel = new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate")),
                    Rank = reader.GetString(reader.GetOrdinal("rank")),
                    IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                    IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1
                };
                
                // Load duty sections
                personnel.DutySections = await GetDutySectionsForPersonnelAsync(dbContext, personnel.Id);
                
                return personnel;
            }
            
            return null;
        }

        public async Task<int> AddPersonnelAsync(DatabaseContext dbContext, Personnel personnel)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO personnel (dod_id, last_name, first_name, rate, rank, is_sami, is_cswi) VALUES (@dod_id, @lastName, @firstName, @rate, @rank, @isSami, @isCswi); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@dod_id", personnel.DODId);
            command.Parameters.AddWithValue("@lastName", personnel.LastName);
            command.Parameters.AddWithValue("@firstName", personnel.FirstName);
            command.Parameters.AddWithValue("@rate", personnel.Rate);
            command.Parameters.AddWithValue("@rank", personnel.Rank);
            command.Parameters.AddWithValue("@isSami", personnel.IsSami ? 1 : 0);
            command.Parameters.AddWithValue("@isCswi", personnel.IsCswi ? 1 : 0);
            
            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Add duty sections if any
            if (personnel.DutySections.Any())
            {
                foreach (var dutySection in personnel.DutySections)
                {
                    await AddDutySectionToPersonnelAsync(dbContext, id, dutySection.Type, dutySection.Section);
                }
            }
            
            return id;
        }

        public async Task<bool> UpdatePersonnelAsync(DatabaseContext dbContext, Personnel personnel)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE personnel SET dod_id = @dod_id, last_name = @lastName, first_name = @firstName, rate = @rate, rank = @rank, is_sami = @isSami, is_cswi = @isCswi WHERE id = @id";
            command.Parameters.AddWithValue("@dod_id", personnel.DODId);
            command.Parameters.AddWithValue("@lastName", personnel.LastName);
            command.Parameters.AddWithValue("@firstName", personnel.FirstName);
            command.Parameters.AddWithValue("@rate", personnel.Rate);
            command.Parameters.AddWithValue("@rank", personnel.Rank);
            command.Parameters.AddWithValue("@isSami", personnel.IsSami ? 1 : 0);
            command.Parameters.AddWithValue("@isCswi", personnel.IsCswi ? 1 : 0);
            command.Parameters.AddWithValue("@id", personnel.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            // Update duty sections
            if (rowsAffected > 0)
            {
                // Remove existing duty sections
                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM personnel_duty WHERE personnel_id = @personnelId";
                deleteCommand.Parameters.AddWithValue("@personnelId", personnel.Id);
                await deleteCommand.ExecuteNonQueryAsync();
                
                // Add new duty sections
                foreach (var dutySection in personnel.DutySections)
                {
                    await AddDutySectionToPersonnelAsync(dbContext, personnel.Id, dutySection.Type, dutySection.Section);
                }
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> UpdatePersonnelFieldsAsync(DatabaseContext dbContext, Personnel personnel)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE personnel SET dod_id = @dod_id, last_name = @lastName, first_name = @firstName, rate = @rate, rank = @rank, is_sami = @isSami, is_cswi = @isCswi WHERE id = @id";
            command.Parameters.AddWithValue("@dod_id", personnel.DODId);
            command.Parameters.AddWithValue("@lastName", personnel.LastName);
            command.Parameters.AddWithValue("@firstName", personnel.FirstName);
            command.Parameters.AddWithValue("@rate", personnel.Rate);
            command.Parameters.AddWithValue("@rank", personnel.Rank);
            command.Parameters.AddWithValue("@isSami", personnel.IsSami ? 1 : 0);
            command.Parameters.AddWithValue("@isCswi", personnel.IsCswi ? 1 : 0);
            command.Parameters.AddWithValue("@id", personnel.Id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeletePersonnelAsync(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();
            
            // Delete duty sections first
            var deleteDutyCommand = connection.CreateCommand();
            deleteDutyCommand.CommandText = "DELETE FROM personnel_duty WHERE personnel_id = @personnelId";
            deleteDutyCommand.Parameters.AddWithValue("@personnelId", id);
            await deleteDutyCommand.ExecuteNonQueryAsync();
            
            // Delete qualifications first
            var deleteQualCommand = connection.CreateCommand();
            deleteQualCommand.CommandText = "DELETE FROM qualifications WHERE personnel_id = @personnelId";
            deleteQualCommand.Parameters.AddWithValue("@personnelId", id);
            await deleteQualCommand.ExecuteNonQueryAsync();
            
            // Delete personnel
            var deletePersonnelCommand = connection.CreateCommand();
            deletePersonnelCommand.CommandText = "DELETE FROM personnel WHERE id = @id";
            deletePersonnelCommand.Parameters.AddWithValue("@id", id);
            
            var rowsAffected = await deletePersonnelCommand.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<List<Personnel>> GetPersonnelWithQualificationsAsync(DatabaseContext dbContext)
        {
            var personnel = new List<Personnel>();
            var personnelDict = new Dictionary<int, Personnel>();
            
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    p.id, p.dod_id, p.last_name, p.first_name, p.rate, p.rank, p.is_sami, p.is_cswi,
                    q.id as qual_id, q.weapon, q.category, q.date_qualified,
                    qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                    qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                    qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                    qd.sustainment_date, qd.sustainment_score
                FROM personnel p
                LEFT JOIN qualifications q ON p.id = q.personnel_id
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                ORDER BY p.last_name, p.first_name, q.date_qualified DESC";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var personnelId = reader.GetInt32(reader.GetOrdinal("id"));
                
                // Get or create personnel
                if (!personnelDict.ContainsKey(personnelId))
                {
                    var person = new Personnel
                    {
                        Id = personnelId,
                        DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        Rate = reader.GetString(reader.GetOrdinal("rate")),
                        Rank = reader.GetString(reader.GetOrdinal("rank")),
                        IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                        IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1,
                        Qualifications = new List<Qualification>()
                    };
                    
                    // Load duty sections for this personnel
                    person.DutySections = await GetDutySectionsForPersonnelAsync(dbContext, personnelId);
                    
                    // Load admin requirements for this personnel
                    person.AdminRequirements = await LoadAdminRequirementsAsync(dbContext, personnelId);
                    
                    personnelDict[personnelId] = person;
                    personnel.Add(person);
                }
                
                // Add qualification if present (LEFT JOIN means qual_id might be null)
                if (!reader.IsDBNull(reader.GetOrdinal("qual_id")))
                {
                    var qualification = new Qualification
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("qual_id")),
                        PersonnelId = personnelId,
                        Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                        Category = reader.GetInt32(reader.GetOrdinal("category")),
                        DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                    };
                    
                    // Load qualification details if present
                    if (!reader.IsDBNull(reader.GetOrdinal("nhqc_score")))
                    {
                        qualification.Details = new QualificationDetails
                        {
                            HQCScore = reader.IsDBNull(reader.GetOrdinal("hqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("hqc_score")),
                            NHQCScore = reader.GetInt32(reader.GetOrdinal("nhqc_score")),
                            HLLCScore = reader.GetInt32(reader.GetOrdinal("hllc_score")),
                            HPWCScore = reader.GetInt32(reader.GetOrdinal("hpwc_score")),
                            RQCScore = reader.IsDBNull(reader.GetOrdinal("rqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rqc_score")),
                            RLCScore = reader.IsDBNull(reader.GetOrdinal("rlc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rlc_score")),
                            SPWCScore = reader.IsDBNull(reader.GetOrdinal("spwc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("spwc_score")),
                            COFScore = reader.IsDBNull(reader.GetOrdinal("cof_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("cof_score")),
                            CSWI = reader.IsDBNull(reader.GetOrdinal("cswi")) ? string.Empty : reader.GetString(reader.GetOrdinal("cswi")),
                            Instructor = reader.IsDBNull(reader.GetOrdinal("instructor")) ? string.Empty : reader.GetString(reader.GetOrdinal("instructor")),
                            Remarks = reader.IsDBNull(reader.GetOrdinal("remarks")) ? string.Empty : reader.GetString(reader.GetOrdinal("remarks")),
                            QualifiedUnderway = !reader.IsDBNull(reader.GetOrdinal("qualified_underway")) && reader.GetInt32(reader.GetOrdinal("qualified_underway")) == 1,
                            SustainmentDate = reader.IsDBNull(reader.GetOrdinal("sustainment_date")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("sustainment_date"))),
                            SustainmentScore = reader.IsDBNull(reader.GetOrdinal("sustainment_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("sustainment_score"))
                        };
                    }
                    
                    // Calculate status using QualificationService
                    var qualificationService = new QualTrack.Core.Services.QualificationService();
                    qualification.Status = qualificationService.EvaluateQualification(
                        qualification.DateQualified,
                        qualification.Category,
                        qualification.Details,
                        null,
                        qualification.Weapon);
                    
                    personnelDict[personnelId].Qualifications.Add(qualification);
                }
            }
            
            return personnel;
        }

        private async Task<AdditionalRequirements?> LoadAdminRequirementsAsync(DatabaseContext dbContext, int personnelId)
        {
            using var connection = dbContext.GetConnection();
            using var command = new SQLiteCommand(
                @"SELECT personnel_id, form2760_date, form2760_number, form2760_signed_date, form2760_witness,
                         aae_screening_date, aae_screening_level, aae_investigation_type, aae_investigation_date, aae_investigation_agency,
                         deadly_force_training_date, deadly_force_instructor, deadly_force_remarks
                  FROM additional_requirements 
                  WHERE personnel_id = @personnelId", connection);
            
            command.Parameters.AddWithValue("@personnelId", personnelId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AdditionalRequirements
                {
                    PersonnelId = personnelId,
                    Form2760Date = reader.IsDBNull(reader.GetOrdinal("form2760_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("form2760_date"))),
                    Form2760Number = reader.IsDBNull(reader.GetOrdinal("form2760_number")) ? null : reader.GetString(reader.GetOrdinal("form2760_number")),
                    Form2760SignedDate = reader.IsDBNull(reader.GetOrdinal("form2760_signed_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("form2760_signed_date"))),
                    Form2760Witness = reader.IsDBNull(reader.GetOrdinal("form2760_witness")) ? null : reader.GetString(reader.GetOrdinal("form2760_witness")),
                    AAEScreeningDate = reader.IsDBNull(reader.GetOrdinal("aae_screening_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("aae_screening_date"))),
                    AAEScreeningLevel = reader.IsDBNull(reader.GetOrdinal("aae_screening_level")) ? null : reader.GetString(reader.GetOrdinal("aae_screening_level")),
                    AAEInvestigationType = reader.IsDBNull(reader.GetOrdinal("aae_investigation_type")) ? null : reader.GetString(reader.GetOrdinal("aae_investigation_type")),
                    AAEInvestigationDate = reader.IsDBNull(reader.GetOrdinal("aae_investigation_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("aae_investigation_date"))),
                    AAEInvestigationAgency = reader.IsDBNull(reader.GetOrdinal("aae_investigation_agency")) ? null : reader.GetString(reader.GetOrdinal("aae_investigation_agency")),
                    DeadlyForceTrainingDate = reader.IsDBNull(reader.GetOrdinal("deadly_force_training_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("deadly_force_training_date"))),
                    DeadlyForceInstructor = reader.IsDBNull(reader.GetOrdinal("deadly_force_instructor")) ? null : reader.GetString(reader.GetOrdinal("deadly_force_instructor")),
                    DeadlyForceRemarks = reader.IsDBNull(reader.GetOrdinal("deadly_force_remarks")) ? null : reader.GetString(reader.GetOrdinal("deadly_force_remarks"))
                };
            }
            
            return null;
        }

        public async Task<List<(string Type, string Section)>> GetDutySectionsForPersonnelAsync(DatabaseContext dbContext, int personnelId)
        {
            var dutySections = new List<(string, string)>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT duty_section_type, section_number
                FROM personnel_duty
                WHERE personnel_id = @personnelId";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var type = reader.GetString(reader.GetOrdinal("duty_section_type"));
                var section = reader.GetString(reader.GetOrdinal("section_number"));
                dutySections.Add((type, section));
            }
            return dutySections;
        }

        public async Task<bool> AddDutySectionToPersonnelAsync(DatabaseContext dbContext, int personnelId, string dutySectionType, string sectionNumber)
        {
            using var connection = dbContext.GetConnection();
            // Check if the personnel-duty section relationship already exists
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM personnel_duty WHERE personnel_id = @personnelId AND duty_section_type = @dutySectionType AND section_number = @sectionNumber";
            checkCommand.Parameters.AddWithValue("@personnelId", personnelId);
            checkCommand.Parameters.AddWithValue("@dutySectionType", dutySectionType);
            checkCommand.Parameters.AddWithValue("@sectionNumber", sectionNumber);
            var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (existingCount > 0)
            {
                return true; // Already exists
            }
            // Add the relationship
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO personnel_duty (personnel_id, duty_section_type, section_number) VALUES (@personnelId, @dutySectionType, @sectionNumber)";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@dutySectionType", dutySectionType);
            command.Parameters.AddWithValue("@sectionNumber", sectionNumber);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<List<Personnel>> GetPersonnelWithQualificationsFilteredAsync(
            DatabaseContext dbContext,
            string? statusFilter = null,
            string? dutySectionType = null,
            string? dutySectionNumber = null,
            string? weaponFilter = null)
        {
            var personnel = new List<Personnel>();
            var personnelDict = new Dictionary<int, Personnel>();
            
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            
            // Build dynamic query with filters
            var query = @"
                SELECT DISTINCT
                    p.id, p.dod_id, p.last_name, p.first_name, p.rate, p.rank, p.is_sami, p.is_cswi,
                    q.id as qual_id, q.weapon, q.category, q.date_qualified,
                    qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                    qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                    qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                    qd.sustainment_date, qd.sustainment_score
                FROM personnel p
                LEFT JOIN qualifications q ON p.id = q.personnel_id
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                LEFT JOIN personnel_duty pd ON p.id = pd.personnel_id";
            
            var whereConditions = new List<string>();
            var parameters = new List<System.Data.SQLite.SQLiteParameter>();
            
            // Duty section type filter
            if (!string.IsNullOrEmpty(dutySectionType) && dutySectionType != "All")
            {
                whereConditions.Add("pd.duty_section_type = @dutySectionType");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@dutySectionType", 
                    dutySectionType == "3 Section" ? "3" : "6"));
            }
            
            // Duty section number filter
            if (!string.IsNullOrEmpty(dutySectionNumber) && dutySectionNumber != "All")
            {
                whereConditions.Add("pd.section_number = @dutySectionNumber");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@dutySectionNumber", dutySectionNumber));
            }
            
            // Weapon filter
            if (!string.IsNullOrEmpty(weaponFilter) && weaponFilter != "All")
            {
                whereConditions.Add("q.weapon = @weaponFilter");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@weaponFilter", weaponFilter));
            }
            
            if (whereConditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", whereConditions);
            }
            
            query += " ORDER BY p.last_name, p.first_name, q.date_qualified DESC";
            
            command.CommandText = query;
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var personnelId = reader.GetInt32(reader.GetOrdinal("id"));
                
                // Get or create personnel
                if (!personnelDict.ContainsKey(personnelId))
                {
                    var person = new Personnel
                    {
                        Id = personnelId,
                        DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        Rate = reader.GetString(reader.GetOrdinal("rate")),
                        Rank = reader.GetString(reader.GetOrdinal("rank")),
                        IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                        IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1,
                        Qualifications = new List<Qualification>()
                    };
                    
                    // Load duty sections for this personnel
                    person.DutySections = await GetDutySectionsForPersonnelAsync(dbContext, personnelId);
                    
                    personnelDict[personnelId] = person;
                    personnel.Add(person);
                }
                
                // Add qualification if present (LEFT JOIN means qual_id might be null)
                if (!reader.IsDBNull(reader.GetOrdinal("qual_id")))
                {
                    var qualification = new Qualification
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("qual_id")),
                        PersonnelId = personnelId,
                        Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                        Category = reader.GetInt32(reader.GetOrdinal("category")),
                        DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                    };
                    
                    // Load qualification details if present
                    if (!reader.IsDBNull(reader.GetOrdinal("nhqc_score")))
                    {
                        qualification.Details = new QualificationDetails
                        {
                            HQCScore = reader.IsDBNull(reader.GetOrdinal("hqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("hqc_score")),
                            NHQCScore = reader.GetInt32(reader.GetOrdinal("nhqc_score")),
                            HLLCScore = reader.GetInt32(reader.GetOrdinal("hllc_score")),
                            HPWCScore = reader.GetInt32(reader.GetOrdinal("hpwc_score")),
                            RQCScore = reader.IsDBNull(reader.GetOrdinal("rqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rqc_score")),
                            RLCScore = reader.IsDBNull(reader.GetOrdinal("rlc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rlc_score")),
                            SPWCScore = reader.IsDBNull(reader.GetOrdinal("spwc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("spwc_score")),
                            COFScore = reader.IsDBNull(reader.GetOrdinal("cof_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("cof_score")),
                            CSWI = reader.IsDBNull(reader.GetOrdinal("cswi")) ? string.Empty : reader.GetString(reader.GetOrdinal("cswi")),
                            Instructor = reader.IsDBNull(reader.GetOrdinal("instructor")) ? string.Empty : reader.GetString(reader.GetOrdinal("instructor")),
                            Remarks = reader.IsDBNull(reader.GetOrdinal("remarks")) ? string.Empty : reader.GetString(reader.GetOrdinal("remarks")),
                            QualifiedUnderway = !reader.IsDBNull(reader.GetOrdinal("qualified_underway")) && reader.GetInt32(reader.GetOrdinal("qualified_underway")) == 1,
                            SustainmentDate = reader.IsDBNull(reader.GetOrdinal("sustainment_date")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("sustainment_date"))),
                            SustainmentScore = reader.IsDBNull(reader.GetOrdinal("sustainment_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("sustainment_score"))
                        };
                    }
                    
                    // Calculate status using QualificationService
                    var qualificationService = new QualTrack.Core.Services.QualificationService();
                    qualification.Status = qualificationService.EvaluateQualification(
                        qualification.DateQualified,
                        qualification.Category,
                        qualification.Details,
                        null,
                        qualification.Weapon);
                    
                    personnelDict[personnelId].Qualifications.Add(qualification);
                }
            }
            
            // Apply status filter in memory (since it requires complex calculations)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                personnel = personnel.Where(p => 
                {
                    var viewModel = new PersonnelViewModel(p);
                    return statusFilter switch
                    {
                        "Lapsed" => !string.IsNullOrWhiteSpace(viewModel.LapsedQualificationsDisplay),
                        "Qualified" => viewModel.StatusDisplay.StartsWith("Qualified") && !viewModel.StatusDisplay.StartsWith("Sustainment Due"),
                        "Sustainment Due" => viewModel.StatusDisplay.StartsWith("Sustainment Due"),
                        _ => true
                    };
                }).ToList();
            }
            
            return personnel;
        }

        public async Task<bool> PersonnelExistsAsync(DatabaseContext dbContext, string dodId)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM personnel WHERE dod_id = @dodId";
            command.Parameters.AddWithValue("@dodId", dodId);
            
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<bool> RemoveDutySectionFromPersonnelAsync(DatabaseContext dbContext, int personnelId, string dutySectionType, string sectionNumber)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM personnel_duty 
                WHERE personnel_id = @personnelId 
                AND duty_section_type = @dutySectionType 
                AND section_number = @sectionNumber";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@dutySectionType", dutySectionType);
            command.Parameters.AddWithValue("@sectionNumber", sectionNumber);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<(List<Personnel> Personnel, int TotalCount)> GetPersonnelWithQualificationsPaginatedAsync(
            DatabaseContext dbContext,
            int pageNumber = 1,
            int pageSize = 50,
            string? statusFilter = null,
            string? dutySectionType = null,
            string? dutySectionNumber = null,
            string? weaponFilter = null)
        {
            // First get total count
            using var connection = dbContext.GetConnection();
            var countCommand = connection.CreateCommand();
            
            var countQuery = @"
                SELECT COUNT(DISTINCT p.id)
                FROM personnel p
                LEFT JOIN qualifications q ON p.id = q.personnel_id
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                LEFT JOIN personnel_duty pd ON p.id = pd.personnel_id";
            
            var whereConditions = new List<string>();
            var parameters = new List<System.Data.SQLite.SQLiteParameter>();
            
            // Apply filters for count
            if (!string.IsNullOrEmpty(dutySectionType) && dutySectionType != "All")
            {
                whereConditions.Add("pd.duty_section_type = @dutySectionType");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@dutySectionType", 
                    dutySectionType == "3 Section" ? "3" : "6"));
            }
            
            if (!string.IsNullOrEmpty(dutySectionNumber) && dutySectionNumber != "All")
            {
                whereConditions.Add("pd.section_number = @dutySectionNumber");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@dutySectionNumber", dutySectionNumber));
            }
            
            if (!string.IsNullOrEmpty(weaponFilter) && weaponFilter != "All")
            {
                whereConditions.Add("q.weapon = @weaponFilter");
                parameters.Add(new System.Data.SQLite.SQLiteParameter("@weaponFilter", weaponFilter));
            }
            
            if (whereConditions.Count > 0)
            {
                countQuery += " WHERE " + string.Join(" AND ", whereConditions);
            }
            
            countCommand.CommandText = countQuery;
            foreach (var param in parameters)
            {
                countCommand.Parameters.Add(param);
            }
            
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            
            // Now get paginated data
            var offset = (pageNumber - 1) * pageSize;
            var dataCommand = connection.CreateCommand();
            
            var dataQuery = @"
                SELECT DISTINCT
                    p.id, p.dod_id, p.last_name, p.first_name, p.rate, p.rank, p.is_sami, p.is_cswi,
                    q.id as qual_id, q.weapon, q.category, q.date_qualified,
                    qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                    qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                    qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                    qd.sustainment_date, qd.sustainment_score
                FROM personnel p
                LEFT JOIN qualifications q ON p.id = q.personnel_id
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                LEFT JOIN personnel_duty pd ON p.id = pd.personnel_id";
            
            if (whereConditions.Count > 0)
            {
                dataQuery += " WHERE " + string.Join(" AND ", whereConditions);
            }
            
            dataQuery += " ORDER BY p.last_name, p.first_name, q.date_qualified DESC LIMIT @pageSize OFFSET @offset";
            
            dataCommand.CommandText = dataQuery;
            foreach (var param in parameters)
            {
                dataCommand.Parameters.Add(param);
            }
            dataCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@pageSize", pageSize));
            dataCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@offset", offset));
            
            var personnel = new List<Personnel>();
            var personnelDict = new Dictionary<int, Personnel>();
            
            using var reader = await dataCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var personnelId = reader.GetInt32(reader.GetOrdinal("id"));
                
                // Get or create personnel
                if (!personnelDict.ContainsKey(personnelId))
                {
                    var person = new Personnel
                    {
                        Id = personnelId,
                        DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        Rate = reader.GetString(reader.GetOrdinal("rate")),
                        Rank = reader.GetString(reader.GetOrdinal("rank")),
                        IsSami = !reader.IsDBNull(reader.GetOrdinal("is_sami")) && reader.GetInt32(reader.GetOrdinal("is_sami")) == 1,
                        IsCswi = !reader.IsDBNull(reader.GetOrdinal("is_cswi")) && reader.GetInt32(reader.GetOrdinal("is_cswi")) == 1,
                        Qualifications = new List<Qualification>()
                    };
                    
                    // Load duty sections for this personnel
                    person.DutySections = await GetDutySectionsForPersonnelAsync(dbContext, personnelId);
                    
                    personnelDict[personnelId] = person;
                    personnel.Add(person);
                }
                
                // Add qualification if present
                if (!reader.IsDBNull(reader.GetOrdinal("qual_id")))
                {
                    var qualification = new Qualification
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("qual_id")),
                        PersonnelId = personnelId,
                        Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                        Category = reader.GetInt32(reader.GetOrdinal("category")),
                        DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                    };
                    
                    // Load qualification details if present
                    if (!reader.IsDBNull(reader.GetOrdinal("nhqc_score")))
                    {
                        qualification.Details = new QualificationDetails
                        {
                            HQCScore = reader.IsDBNull(reader.GetOrdinal("hqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("hqc_score")),
                            NHQCScore = reader.GetInt32(reader.GetOrdinal("nhqc_score")),
                            HLLCScore = reader.GetInt32(reader.GetOrdinal("hllc_score")),
                            HPWCScore = reader.GetInt32(reader.GetOrdinal("hpwc_score")),
                            RQCScore = reader.IsDBNull(reader.GetOrdinal("rqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rqc_score")),
                            RLCScore = reader.IsDBNull(reader.GetOrdinal("rlc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rlc_score")),
                            SPWCScore = reader.IsDBNull(reader.GetOrdinal("spwc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("spwc_score")),
                            COFScore = reader.IsDBNull(reader.GetOrdinal("cof_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("cof_score")),
                            CSWI = reader.IsDBNull(reader.GetOrdinal("cswi")) ? string.Empty : reader.GetString(reader.GetOrdinal("cswi")),
                            Instructor = reader.IsDBNull(reader.GetOrdinal("instructor")) ? string.Empty : reader.GetString(reader.GetOrdinal("instructor")),
                            Remarks = reader.IsDBNull(reader.GetOrdinal("remarks")) ? string.Empty : reader.GetString(reader.GetOrdinal("remarks")),
                            QualifiedUnderway = !reader.IsDBNull(reader.GetOrdinal("qualified_underway")) && reader.GetInt32(reader.GetOrdinal("qualified_underway")) == 1,
                            SustainmentDate = reader.IsDBNull(reader.GetOrdinal("sustainment_date")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("sustainment_date"))),
                            SustainmentScore = reader.IsDBNull(reader.GetOrdinal("sustainment_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("sustainment_score"))
                        };
                    }
                    
                    // Calculate status using QualificationService
                    var qualificationService = new QualTrack.Core.Services.QualificationService();
                    qualification.Status = qualificationService.EvaluateQualification(
                        qualification.DateQualified,
                        qualification.Category,
                        qualification.Details,
                        null,
                        qualification.Weapon);
                    
                    personnelDict[personnelId].Qualifications.Add(qualification);
                }
            }
            
            // Apply status filter in memory (since it requires complex calculations)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                personnel = personnel.Where(p => 
                {
                    var viewModel = new PersonnelViewModel(p);
                    return statusFilter switch
                    {
                        "Lapsed" => !string.IsNullOrWhiteSpace(viewModel.LapsedQualificationsDisplay),
                        "Qualified" => viewModel.StatusDisplay.StartsWith("Qualified") && !viewModel.StatusDisplay.StartsWith("Sustainment Due"),
                        "Sustainment Due" => viewModel.StatusDisplay.StartsWith("Sustainment Due"),
                        _ => true
                    };
                }).ToList();
            }
            
            return (personnel, totalCount);
        }

        public async Task ClearAllDataAsync(DatabaseContext dbContext)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM qualification_details;
                DELETE FROM qualifications;
                DELETE FROM personnel_duty;
                DELETE FROM personnel;
                DELETE FROM audit_log;";
            await command.ExecuteNonQueryAsync();
        }
    }
} 