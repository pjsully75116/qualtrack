using System.Data.SQLite;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Implementation of qualification data access operations
    /// </summary>
    public class QualificationRepository : IQualificationRepository
    {
        private readonly DatabaseContext _dbContext;

        public QualificationRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Qualification>> GetQualificationsForPersonnelAsync(int personnelId)
        {
            var qualifications = new List<Qualification>();
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, weapon, category, date_qualified 
                FROM qualifications 
                WHERE personnel_id = @personnelId 
                ORDER BY date_qualified DESC";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var qualification = new Qualification
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                    Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                    Category = reader.GetInt32(reader.GetOrdinal("category")),
                    DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                };
                // Load details if present
                qualification.Details = await GetQualificationDetailsAsync(qualification.Id);
                // Calculate status using QualificationService
                var qualificationService = new QualTrack.Core.Services.QualificationService();
                qualification.Status = qualificationService.EvaluateQualification(
                    qualification.DateQualified, 
                    qualification.Category);
                qualifications.Add(qualification);
            }
            return qualifications;
        }

        public async Task<List<Qualification>> GetAllQualificationsAsync()
        {
            var qualifications = new List<Qualification>();
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, weapon, category, date_qualified 
                FROM qualifications 
                ORDER BY date_qualified DESC";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var qualification = new Qualification
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                    Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                    Category = reader.GetInt32(reader.GetOrdinal("category")),
                    DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                };
                qualification.Details = await GetQualificationDetailsAsync(qualification.Id);
                var qualificationService = new QualTrack.Core.Services.QualificationService();
                qualification.Status = qualificationService.EvaluateQualification(
                    qualification.DateQualified, 
                    qualification.Category);
                qualifications.Add(qualification);
            }
            return qualifications;
        }

        public async Task<Qualification?> GetQualificationByIdAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, weapon, category, date_qualified 
                FROM qualifications 
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var qualification = new Qualification
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                    Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                    Category = reader.GetInt32(reader.GetOrdinal("category")),
                    DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified")))
                };
                qualification.Details = await GetQualificationDetailsAsync(qualification.Id);
                var qualificationService = new QualTrack.Core.Services.QualificationService();
                qualification.Status = qualificationService.EvaluateQualification(
                    qualification.DateQualified, 
                    qualification.Category);
                return qualification;
            }
            return null;
        }

        public async Task<int> AddQualificationAsync(Qualification qualification)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO qualifications (personnel_id, weapon, category, date_qualified) 
                VALUES (@personnelId, @weapon, @category, @dateQualified); 
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@personnelId", qualification.PersonnelId);
            command.Parameters.AddWithValue("@weapon", qualification.Weapon);
            command.Parameters.AddWithValue("@category", qualification.Category);
            command.Parameters.AddWithValue("@dateQualified", qualification.DateQualified.ToString("yyyy-MM-dd"));
            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            // Save details if present
            if (qualification.Details != null)
            {
                await AddOrUpdateQualificationDetailsAsync(id, qualification.Details);
            }
            return id;
        }

        public async Task<bool> UpdateQualificationAsync(Qualification qualification)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE qualifications 
                SET personnel_id = @personnelId, weapon = @weapon, category = @category, date_qualified = @dateQualified 
                WHERE id = @id";
            command.Parameters.AddWithValue("@personnelId", qualification.PersonnelId);
            command.Parameters.AddWithValue("@weapon", qualification.Weapon);
            command.Parameters.AddWithValue("@category", qualification.Category);
            command.Parameters.AddWithValue("@dateQualified", qualification.DateQualified.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@id", qualification.Id);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            // Save or remove details
            if (qualification.Details != null)
            {
                await AddOrUpdateQualificationDetailsAsync(qualification.Id, qualification.Details);
            }
            else
            {
                await DeleteQualificationDetailsAsync(qualification.Id);
            }
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteQualificationAsync(int id)
        {
            await DeleteQualificationDetailsAsync(id);
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM qualifications WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> QualificationExistsAsync(int personnelId, string weapon)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM qualifications 
                WHERE personnel_id = @personnelId AND weapon = @weapon";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@weapon", weapon);
            
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<List<Qualification>> GetExpiringQualificationsAsync(int daysThreshold)
        {
            var allQualifications = await GetAllQualificationsAsync();
            var expiringQualifications = new List<Qualification>();
            var qualificationService = new QualTrack.Core.Services.QualificationService();
            
            foreach (var qualification in allQualifications)
            {
                var status = qualificationService.EvaluateQualification(
                    qualification.DateQualified, 
                    qualification.Category);
                
                if (status.DaysUntilExpiration <= daysThreshold && status.DaysUntilExpiration > 0)
                {
                    qualification.Status = status;
                    expiringQualifications.Add(qualification);
                }
            }
            
            return expiringQualifications;
        }

        public async Task<List<Qualification>> GetQualificationsNeedingSustainmentAsync()
        {
            var allQualifications = await GetAllQualificationsAsync();
            var sustainmentQualifications = new List<Qualification>();
            var qualificationService = new QualTrack.Core.Services.QualificationService();
            
            foreach (var qualification in allQualifications)
            {
                var status = qualificationService.EvaluateQualification(
                    qualification.DateQualified, 
                    qualification.Category);
                
                if (status.SustainmentDue)
                {
                    qualification.Status = status;
                    sustainmentQualifications.Add(qualification);
                }
            }
            
            return sustainmentQualifications;
        }

        private async Task<QualificationDetails?> GetQualificationDetailsAsync(int qualificationId)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT * FROM qualification_details WHERE qualification_id = @qualificationId";
            command.Parameters.AddWithValue("@qualificationId", qualificationId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new QualificationDetails
                {
                    NHQCScore = reader.GetInt32(reader.GetOrdinal("nhqc_score")),
                    HLLCScore = reader.GetInt32(reader.GetOrdinal("hllc_score")),
                    HPWCScore = reader.GetInt32(reader.GetOrdinal("hpwc_score")),
                    RQCScore = reader.IsDBNull(reader.GetOrdinal("rqc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rqc_score")),
                    RLCScore = reader.IsDBNull(reader.GetOrdinal("rlc_score")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rlc_score")),
                    Instructor = reader.GetString(reader.GetOrdinal("instructor")),
                    Remarks = reader.GetString(reader.GetOrdinal("remarks"))
                };
            }
            return null;
        }

        private async Task AddOrUpdateQualificationDetailsAsync(int qualificationId, QualificationDetails? details)
        {
            if (details == null) return;
            var connection = _dbContext.GetConnection();
            // Check if details exist
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM qualification_details WHERE qualification_id = @qualificationId";
            checkCmd.Parameters.AddWithValue("@qualificationId", qualificationId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (exists)
            {
                var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"UPDATE qualification_details SET nhqc_score=@nhqc, hllc_score=@hllc, hpwc_score=@hpwc, rqc_score=@rqc, rlc_score=@rlc, instructor=@instr, remarks=@remarks WHERE qualification_id=@qualificationId";
                updateCmd.Parameters.AddWithValue("@nhqc", details.NHQCScore);
                updateCmd.Parameters.AddWithValue("@hllc", details.HLLCScore);
                updateCmd.Parameters.AddWithValue("@hpwc", details.HPWCScore);
                updateCmd.Parameters.AddWithValue("@rqc", (object?)details.RQCScore ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@rlc", (object?)details.RLCScore ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@instr", details.Instructor);
                updateCmd.Parameters.AddWithValue("@remarks", details.Remarks);
                updateCmd.Parameters.AddWithValue("@qualificationId", qualificationId);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"INSERT INTO qualification_details (qualification_id, nhqc_score, hllc_score, hpwc_score, rqc_score, rlc_score, instructor, remarks) VALUES (@qualificationId, @nhqc, @hllc, @hpwc, @rqc, @rlc, @instr, @remarks)";
                insertCmd.Parameters.AddWithValue("@qualificationId", qualificationId);
                insertCmd.Parameters.AddWithValue("@nhqc", details.NHQCScore);
                insertCmd.Parameters.AddWithValue("@hllc", details.HLLCScore);
                insertCmd.Parameters.AddWithValue("@hpwc", details.HPWCScore);
                insertCmd.Parameters.AddWithValue("@rqc", (object?)details.RQCScore ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@rlc", (object?)details.RLCScore ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@instr", details.Instructor);
                insertCmd.Parameters.AddWithValue("@remarks", details.Remarks);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task DeleteQualificationDetailsAsync(int qualificationId)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM qualification_details WHERE qualification_id = @qualificationId";
            command.Parameters.AddWithValue("@qualificationId", qualificationId);
            await command.ExecuteNonQueryAsync();
        }
    }
} 