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
        public async Task<List<Qualification>> GetQualificationsForPersonnelAsync(DatabaseContext dbContext, int personnelId)
        {
            var qualifications = new List<Qualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified, q.qualification_session_id, q.crew_served_weapon_session_id,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                WHERE q.personnel_id = @personnelId
                ORDER BY q.date_qualified DESC";
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
                    DateQualified = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_qualified"))),
                    QualificationSessionId = reader.IsDBNull(reader.GetOrdinal("qualification_session_id")) ? null : 
                                            reader.GetInt32(reader.GetOrdinal("qualification_session_id")),
                    CrewServedWeaponSessionId = reader.IsDBNull(reader.GetOrdinal("crew_served_weapon_session_id")) ? null :
                                                reader.GetInt32(reader.GetOrdinal("crew_served_weapon_session_id"))
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
                
                qualifications.Add(qualification);
            }
            
            return qualifications;
        }

        public async Task<List<Qualification>> GetAllQualificationsAsync(DatabaseContext dbContext)
        {
            var qualifications = new List<Qualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified, q.qualification_session_id, q.crew_served_weapon_session_id,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                ORDER BY q.date_qualified DESC";
            
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
                
                qualifications.Add(qualification);
            }
            
            return qualifications;
        }

        public async Task<Qualification?> GetQualificationByIdAsync(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified, q.qualification_session_id, q.crew_served_weapon_session_id,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                WHERE q.id = @id";
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
                
                return qualification;
            }
            
            return null;
        }

                public async Task<int> AddQualificationAsync(DatabaseContext dbContext, Qualification qualification)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO qualifications (personnel_id, weapon, category, date_qualified, qualification_session_id, crew_served_weapon_session_id)
                VALUES (@personnelId, @weapon, @category, @dateQualified, @sessionId, @crewServedSessionId);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@personnelId", qualification.PersonnelId);
            command.Parameters.AddWithValue("@weapon", qualification.Weapon);
            command.Parameters.AddWithValue("@category", qualification.Category);
            command.Parameters.AddWithValue("@dateQualified", qualification.DateQualified.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@sessionId", qualification.QualificationSessionId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@crewServedSessionId", qualification.CrewServedWeaponSessionId ?? (object)DBNull.Value);
            
            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Add qualification details if present
            if (qualification.Details != null)
            {
                await AddQualificationDetailsAsync(dbContext, id, qualification.Details);
            }
            
            return id;
        }

        public async Task<bool> UpdateQualificationAsync(DatabaseContext dbContext, Qualification qualification)
        {
            using var connection = dbContext.GetConnection();
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
            
            // Update qualification details if present
            if (rowsAffected > 0 && qualification.Details != null)
            {
                await UpdateQualificationDetailsAsync(dbContext, qualification.Id, qualification.Details);
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteQualificationAsync(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();
            
            // Delete qualification details first
            var deleteDetailsCommand = connection.CreateCommand();
            deleteDetailsCommand.CommandText = "DELETE FROM qualification_details WHERE qualification_id = @qualificationId";
            deleteDetailsCommand.Parameters.AddWithValue("@qualificationId", id);
            await deleteDetailsCommand.ExecuteNonQueryAsync();
            
            // Delete qualification
            var deleteQualCommand = connection.CreateCommand();
            deleteQualCommand.CommandText = "DELETE FROM qualifications WHERE id = @id";
            deleteQualCommand.Parameters.AddWithValue("@id", id);
            
            var rowsAffected = await deleteQualCommand.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> QualificationExistsAsync(DatabaseContext dbContext, int personnelId, string weapon)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM qualifications WHERE personnel_id = @personnelId AND weapon = @weapon";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@weapon", weapon);
            
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<List<Qualification>> GetExpiringQualificationsAsync(DatabaseContext dbContext, int daysThreshold)
        {
            var qualifications = new List<Qualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified, q.qualification_session_id, q.crew_served_weapon_session_id,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                WHERE q.date_qualified <= @expiryDate
                ORDER BY q.date_qualified ASC";
            
            var expiryDate = DateTime.Today.AddDays(-daysThreshold).ToString("yyyy-MM-dd");
            command.Parameters.AddWithValue("@expiryDate", expiryDate);
            
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
                
                qualifications.Add(qualification);
            }
            
            return qualifications;
        }

        public async Task<List<Qualification>> GetQualificationsNeedingSustainmentAsync(DatabaseContext dbContext)
        {
            var qualifications = new List<Qualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                WHERE q.date_qualified <= @sustainmentDate
                ORDER BY q.date_qualified ASC";
            
            // Sustainment is due after 120 days for CAT II
            var sustainmentDate = DateTime.Today.AddDays(-120).ToString("yyyy-MM-dd");
            command.Parameters.AddWithValue("@sustainmentDate", sustainmentDate);
            
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
                
                qualifications.Add(qualification);
            }
            
            return qualifications;
        }

        public async Task<List<Qualification>> GetQualificationsByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            var qualifications = new List<Qualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT q.id, q.personnel_id, q.weapon, q.category, q.date_qualified,
                       qd.hqc_score, qd.nhqc_score, qd.hllc_score, qd.hpwc_score,
                       qd.rqc_score, qd.rlc_score, qd.spwc_score, qd.cof_score,
                       qd.cswi, qd.instructor, qd.remarks, qd.qualified_underway,
                       qd.sustainment_date, qd.sustainment_score
                FROM qualifications q
                LEFT JOIN qualification_details qd ON q.id = qd.qualification_id
                WHERE q.personnel_id = @personnelId
                ORDER BY q.date_qualified DESC";
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
                
                qualifications.Add(qualification);
            }
            
            return qualifications;
        }

        public async Task<bool> DeleteQualificationsByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            using var connection = dbContext.GetConnection();
            
            // Get all qualification IDs for this personnel
            var getQualIdsCommand = connection.CreateCommand();
            getQualIdsCommand.CommandText = "SELECT id FROM qualifications WHERE personnel_id = @personnelId";
            getQualIdsCommand.Parameters.AddWithValue("@personnelId", personnelId);
            
            var qualIds = new List<int>();
            using (var reader = await getQualIdsCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    qualIds.Add(reader.GetInt32(0));
                }
            }
            
            // Delete qualification details for all qualifications
            if (qualIds.Any())
            {
                var deleteDetailsCommand = connection.CreateCommand();
                deleteDetailsCommand.CommandText = "DELETE FROM qualification_details WHERE qualification_id IN (" + string.Join(",", qualIds) + ")";
                await deleteDetailsCommand.ExecuteNonQueryAsync();
            }
            
            // Delete qualifications
            var deleteQualCommand = connection.CreateCommand();
            deleteQualCommand.CommandText = "DELETE FROM qualifications WHERE personnel_id = @personnelId";
            deleteQualCommand.Parameters.AddWithValue("@personnelId", personnelId);
            
            var rowsAffected = await deleteQualCommand.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> AddQualificationDetailsAsync(DatabaseContext dbContext, int qualificationId, QualificationDetails details)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO qualification_details (
                    qualification_id, hqc_score, nhqc_score, hllc_score, hpwc_score,
                    rqc_score, rlc_score, spwc_score, cof_score, cswi, instructor,
                    remarks, qualified_underway, sustainment_date, sustainment_score
                ) VALUES (
                    @qualificationId, @hqcScore, @nhqcScore, @hllcScore, @hpwcScore,
                    @rqcScore, @rlcScore, @spwcScore, @cofScore, @cswi, @instructor,
                    @remarks, @qualifiedUnderway, @sustainmentDate, @sustainmentScore
                )";
            
            command.Parameters.AddWithValue("@qualificationId", qualificationId);
            command.Parameters.AddWithValue("@hqcScore", details.HQCScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@nhqcScore", details.NHQCScore);
            command.Parameters.AddWithValue("@hllcScore", details.HLLCScore);
            command.Parameters.AddWithValue("@hpwcScore", details.HPWCScore);
            command.Parameters.AddWithValue("@rqcScore", details.RQCScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@rlcScore", details.RLCScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@spwcScore", details.SPWCScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@cofScore", details.COFScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@cswi", details.CSWI ?? string.Empty);
            command.Parameters.AddWithValue("@instructor", details.Instructor ?? string.Empty);
            command.Parameters.AddWithValue("@remarks", details.Remarks ?? string.Empty);
            command.Parameters.AddWithValue("@qualifiedUnderway", details.QualifiedUnderway ? 1 : 0);
            command.Parameters.AddWithValue("@sustainmentDate", details.SustainmentDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@sustainmentScore", details.SustainmentScore ?? (object)DBNull.Value);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateQualificationDetailsAsync(DatabaseContext dbContext, int qualificationId, QualificationDetails details)
        {
            using var connection = dbContext.GetConnection();
            
            // Check if details exist
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM qualification_details WHERE qualification_id = @qualificationId";
            checkCommand.Parameters.AddWithValue("@qualificationId", qualificationId);
            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            if (exists)
            {
                // Update existing details
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE qualification_details SET
                        hqc_score = @hqcScore, nhqc_score = @nhqcScore, hllc_score = @hllcScore, hpwc_score = @hpwcScore,
                        rqc_score = @rqcScore, rlc_score = @rlcScore, spwc_score = @spwcScore, cof_score = @cofScore,
                        cswi = @cswi, instructor = @instructor, remarks = @remarks, qualified_underway = @qualifiedUnderway,
                        sustainment_date = @sustainmentDate, sustainment_score = @sustainmentScore
                    WHERE qualification_id = @qualificationId";
                
                command.Parameters.AddWithValue("@qualificationId", qualificationId);
                command.Parameters.AddWithValue("@hqcScore", details.HQCScore ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@nhqcScore", details.NHQCScore);
                command.Parameters.AddWithValue("@hllcScore", details.HLLCScore);
                command.Parameters.AddWithValue("@hpwcScore", details.HPWCScore);
                command.Parameters.AddWithValue("@rqcScore", details.RQCScore ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@rlcScore", details.RLCScore ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@spwcScore", details.SPWCScore ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@cofScore", details.COFScore ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@cswi", details.CSWI ?? string.Empty);
                command.Parameters.AddWithValue("@instructor", details.Instructor ?? string.Empty);
                command.Parameters.AddWithValue("@remarks", details.Remarks ?? string.Empty);
                command.Parameters.AddWithValue("@qualifiedUnderway", details.QualifiedUnderway ? 1 : 0);
                command.Parameters.AddWithValue("@sustainmentDate", details.SustainmentDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@sustainmentScore", details.SustainmentScore ?? (object)DBNull.Value);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            else
            {
                // Insert new details
                return await AddQualificationDetailsAsync(dbContext, qualificationId, details);
            }
        }

        public async Task ClearAllDataAsync(DatabaseContext dbContext)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM qualification_details;
                DELETE FROM qualifications;";
            await command.ExecuteNonQueryAsync();
        }
    }
} 