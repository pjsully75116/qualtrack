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
        private readonly DatabaseContext _dbContext;

        public PersonnelRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Personnel>> GetAllPersonnelAsync()
        {
            var personnel = new List<Personnel>();
            
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, dod_id, name, rate FROM personnel ORDER BY name";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var person = new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate"))
                };
                
                // Load duty sections for this personnel
                person.DutySections = await GetDutySectionsForPersonnelAsync(person.Id);
                
                personnel.Add(person);
            }
            
            return personnel;
        }

        public async Task<Personnel?> GetPersonnelByIdAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, dod_id, name, rate FROM personnel WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var personnel = new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate"))
                };
                
                // Load duty sections
                personnel.DutySections = await GetDutySectionsForPersonnelAsync(id);
                
                return personnel;
            }
            
            return null;
        }

        public async Task<Personnel?> GetPersonnelByNameAndRateAsync(string name, string rate)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, dod_id, name, rate FROM personnel WHERE name = @name AND rate = @rate";
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@rate", rate);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var personnel = new Personnel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DODId = reader.GetString(reader.GetOrdinal("dod_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Rate = reader.GetString(reader.GetOrdinal("rate"))
                };
                
                // Load duty sections
                personnel.DutySections = await GetDutySectionsForPersonnelAsync(personnel.Id);
                
                return personnel;
            }
            
            return null;
        }

        public async Task<int> AddPersonnelAsync(Personnel personnel)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO personnel (dod_id, name, rate) VALUES (@dod_id, @name, @rate); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@dod_id", personnel.DODId);
            command.Parameters.AddWithValue("@name", personnel.Name);
            command.Parameters.AddWithValue("@rate", personnel.Rate);
            
            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Add duty sections if any
            if (personnel.DutySections.Any())
            {
                foreach (var dutySection in personnel.DutySections)
                {
                    await AddDutySectionToPersonnelAsync(id, dutySection.Type, dutySection.Section);
                }
            }
            
            return id;
        }

        public async Task<bool> UpdatePersonnelAsync(Personnel personnel)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE personnel SET dod_id = @dod_id, name = @name, rate = @rate WHERE id = @id";
            command.Parameters.AddWithValue("@dod_id", personnel.DODId);
            command.Parameters.AddWithValue("@name", personnel.Name);
            command.Parameters.AddWithValue("@rate", personnel.Rate);
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
                    await AddDutySectionToPersonnelAsync(personnel.Id, dutySection.Type, dutySection.Section);
                }
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> DeletePersonnelAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
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
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM personnel WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<List<Personnel>> GetPersonnelWithQualificationsAsync()
        {
            var personnel = await GetAllPersonnelAsync();
            
            // Load qualifications for each personnel
            var qualificationRepo = new QualificationRepository(_dbContext);
            foreach (var person in personnel)
            {
                person.Qualifications = await qualificationRepo.GetQualificationsForPersonnelAsync(person.Id);
            }
            
            return personnel;
        }

        public async Task<List<(string Type, string Section)>> GetDutySectionsForPersonnelAsync(int personnelId)
        {
            var dutySections = new List<(string, string)>();
            var connection = _dbContext.GetConnection();
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

        public async Task<bool> AddDutySectionToPersonnelAsync(int personnelId, string dutySectionType, string sectionNumber)
        {
            var connection = _dbContext.GetConnection();
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
    }
} 