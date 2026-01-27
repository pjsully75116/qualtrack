using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Repository implementation for crew served weapon sessions (3591/2 forms)
    /// </summary>
    public class CrewServedWeaponSessionRepository : ICrewServedWeaponSessionRepository
    {
        public async Task<int> AddSessionAsync(DatabaseContext dbContext, CrewServedWeaponSession session)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO crew_served_weapon_sessions
                (ship_station, division_activity, weapon, range_name_location, date_of_firing,
                 gunner_name, gunner_rank_rate, gunner_dodid,
                 assistant_gunner_name, assistant_gunner_rank_rate, assistant_gunner_dodid,
                 ammunition_handler_name, ammunition_handler_rank_rate, ammunition_handler_dodid,
                 course_of_fire_score, is_qualified,
                 instructor_name, instructor_rank_rate,
                 rso_signature, rso_signature_rate, rso_signature_date,
                 pdf_file_path, created_date)
                VALUES (@ship_station, @division_activity, @weapon, @range_name_location, @date_of_firing,
                        @gunner_name, @gunner_rank_rate, @gunner_dodid,
                        @assistant_gunner_name, @assistant_gunner_rank_rate, @assistant_gunner_dodid,
                        @ammunition_handler_name, @ammunition_handler_rank_rate, @ammunition_handler_dodid,
                        @course_of_fire_score, @is_qualified,
                        @instructor_name, @instructor_rank_rate,
                        @rso_signature, @rso_signature_rate, @rso_signature_date,
                        @pdf_file_path, @created_date);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@ship_station", session.ShipStation);
            command.Parameters.AddWithValue("@division_activity", session.DivisionActivity);
            command.Parameters.AddWithValue("@weapon", session.Weapon);
            command.Parameters.AddWithValue("@range_name_location", session.RangeNameLocation);
            command.Parameters.AddWithValue("@date_of_firing", session.DateOfFiring?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@gunner_name", session.GunnerName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@gunner_rank_rate", session.GunnerRankRate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@gunner_dodid", session.GunnerDODID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@assistant_gunner_name", session.AssistantGunnerName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@assistant_gunner_rank_rate", session.AssistantGunnerRankRate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@assistant_gunner_dodid", session.AssistantGunnerDODID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ammunition_handler_name", session.AmmunitionHandlerName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ammunition_handler_rank_rate", session.AmmunitionHandlerRankRate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ammunition_handler_dodid", session.AmmunitionHandlerDODID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@course_of_fire_score", session.CourseOfFireScore.HasValue ? (object)session.CourseOfFireScore.Value : DBNull.Value);
            command.Parameters.AddWithValue("@is_qualified", session.IsQualified ? 1 : 0);
            command.Parameters.AddWithValue("@instructor_name", session.InstructorName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@instructor_rank_rate", session.InstructorRankRate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@rso_signature", session.RsoSignature ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@rso_signature_rate", session.RsoSignatureRate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@rso_signature_date", session.RsoSignatureDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@pdf_file_path", session.PdfFilePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@created_date", session.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<CrewServedWeaponSession?> GetSessionByIdAsync(DatabaseContext dbContext, int sessionId)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, ship_station, division_activity, weapon, range_name_location, date_of_firing,
                       gunner_name, gunner_rank_rate, gunner_dodid,
                       assistant_gunner_name, assistant_gunner_rank_rate, assistant_gunner_dodid,
                       ammunition_handler_name, ammunition_handler_rank_rate, ammunition_handler_dodid,
                       course_of_fire_score, is_qualified,
                       instructor_name, instructor_rank_rate,
                       rso_signature, rso_signature_rate, rso_signature_date,
                       pdf_file_path, created_date
                FROM crew_served_weapon_sessions
                WHERE id = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToSession(reader);
            }
            
            return null;
        }

        public async Task<List<CrewServedWeaponSession>> GetAllSessionsAsync(DatabaseContext dbContext)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, ship_station, division_activity, weapon, range_name_location, date_of_firing,
                       gunner_name, gunner_rank_rate, gunner_dodid,
                       assistant_gunner_name, assistant_gunner_rank_rate, assistant_gunner_dodid,
                       ammunition_handler_name, ammunition_handler_rank_rate, ammunition_handler_dodid,
                       course_of_fire_score, is_qualified,
                       instructor_name, instructor_rank_rate,
                       rso_signature, rso_signature_rate, rso_signature_date,
                       pdf_file_path, created_date
                FROM crew_served_weapon_sessions
                ORDER BY date_of_firing DESC, created_date DESC";
            
            var sessions = new List<CrewServedWeaponSession>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(MapReaderToSession(reader));
            }
            
            return sessions;
        }

        public async Task<List<CrewServedWeaponSession>> GetSessionsByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            // This requires joining with qualifications table to find sessions where personnel participated
            // For now, we'll search by DODID in crew member fields
            // TODO: In future, link qualifications to crew_served_weapon_session_id similar to qualification_session_id
            
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            
            // First, get the personnel DODID
            var personnelCommand = connection.CreateCommand();
            personnelCommand.CommandText = "SELECT dod_id FROM personnel WHERE id = @personnel_id";
            personnelCommand.Parameters.AddWithValue("@personnel_id", personnelId);
            var dodIdResult = await personnelCommand.ExecuteScalarAsync();
            
            if (dodIdResult == null)
                return new List<CrewServedWeaponSession>();
            
            string dodId = dodIdResult.ToString() ?? string.Empty;
            
            command.CommandText = @"
                SELECT id, ship_station, division_activity, weapon, range_name_location, date_of_firing,
                       gunner_name, gunner_rank_rate, gunner_dodid,
                       assistant_gunner_name, assistant_gunner_rank_rate, assistant_gunner_dodid,
                       ammunition_handler_name, ammunition_handler_rank_rate, ammunition_handler_dodid,
                       course_of_fire_score, is_qualified,
                       instructor_name, instructor_rank_rate,
                       rso_signature, rso_signature_rate, rso_signature_date,
                       pdf_file_path, created_date
                FROM crew_served_weapon_sessions
                WHERE gunner_dodid = @dodid 
                   OR assistant_gunner_dodid = @dodid 
                   OR ammunition_handler_dodid = @dodid
                ORDER BY date_of_firing DESC";
            command.Parameters.AddWithValue("@dodid", dodId);
            
            var sessions = new List<CrewServedWeaponSession>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(MapReaderToSession(reader));
            }
            
            return sessions;
        }

        public async Task<List<CrewServedWeaponSession>> GetSessionsByWeaponAsync(DatabaseContext dbContext, string weapon)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, ship_station, division_activity, weapon, range_name_location, date_of_firing,
                       gunner_name, gunner_rank_rate, gunner_dodid,
                       assistant_gunner_name, assistant_gunner_rank_rate, assistant_gunner_dodid,
                       ammunition_handler_name, ammunition_handler_rank_rate, ammunition_handler_dodid,
                       course_of_fire_score, is_qualified,
                       instructor_name, instructor_rank_rate,
                       rso_signature, rso_signature_rate, rso_signature_date,
                       pdf_file_path, created_date
                FROM crew_served_weapon_sessions
                WHERE weapon = @weapon
                ORDER BY date_of_firing DESC";
            command.Parameters.AddWithValue("@weapon", weapon);
            
            var sessions = new List<CrewServedWeaponSession>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(MapReaderToSession(reader));
            }
            
            return sessions;
        }

        public async Task UpdateSessionPdfFilePathAsync(DatabaseContext dbContext, int sessionId, string pdfFilePath)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE crew_served_weapon_sessions
                SET pdf_file_path = @pdf_file_path
                WHERE id = @sessionId";
            command.Parameters.AddWithValue("@pdf_file_path", pdfFilePath ?? string.Empty);
            command.Parameters.AddWithValue("@sessionId", sessionId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeleteSessionAsync(DatabaseContext dbContext, int sessionId)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM crew_served_weapon_sessions WHERE id = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private CrewServedWeaponSession MapReaderToSession(DbDataReader reader)
        {
            return new CrewServedWeaponSession
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ShipStation = reader.GetString(reader.GetOrdinal("ship_station")),
                DivisionActivity = reader.GetString(reader.GetOrdinal("division_activity")),
                Weapon = reader.GetString(reader.GetOrdinal("weapon")),
                RangeNameLocation = reader.GetString(reader.GetOrdinal("range_name_location")),
                DateOfFiring = reader.IsDBNull(reader.GetOrdinal("date_of_firing")) ? null :
                              DateTime.Parse(reader.GetString(reader.GetOrdinal("date_of_firing"))),
                GunnerName = reader.IsDBNull(reader.GetOrdinal("gunner_name")) ? null :
                            reader.GetString(reader.GetOrdinal("gunner_name")),
                GunnerRankRate = reader.IsDBNull(reader.GetOrdinal("gunner_rank_rate")) ? null :
                                reader.GetString(reader.GetOrdinal("gunner_rank_rate")),
                GunnerDODID = reader.IsDBNull(reader.GetOrdinal("gunner_dodid")) ? null :
                             reader.GetString(reader.GetOrdinal("gunner_dodid")),
                AssistantGunnerName = reader.IsDBNull(reader.GetOrdinal("assistant_gunner_name")) ? null :
                                     reader.GetString(reader.GetOrdinal("assistant_gunner_name")),
                AssistantGunnerRankRate = reader.IsDBNull(reader.GetOrdinal("assistant_gunner_rank_rate")) ? null :
                                         reader.GetString(reader.GetOrdinal("assistant_gunner_rank_rate")),
                AssistantGunnerDODID = reader.IsDBNull(reader.GetOrdinal("assistant_gunner_dodid")) ? null :
                                     reader.GetString(reader.GetOrdinal("assistant_gunner_dodid")),
                AmmunitionHandlerName = reader.IsDBNull(reader.GetOrdinal("ammunition_handler_name")) ? null :
                                       reader.GetString(reader.GetOrdinal("ammunition_handler_name")),
                AmmunitionHandlerRankRate = reader.IsDBNull(reader.GetOrdinal("ammunition_handler_rank_rate")) ? null :
                                           reader.GetString(reader.GetOrdinal("ammunition_handler_rank_rate")),
                AmmunitionHandlerDODID = reader.IsDBNull(reader.GetOrdinal("ammunition_handler_dodid")) ? null :
                                        reader.GetString(reader.GetOrdinal("ammunition_handler_dodid")),
                CourseOfFireScore = reader.IsDBNull(reader.GetOrdinal("course_of_fire_score")) ? null :
                                   (int?)reader.GetInt32(reader.GetOrdinal("course_of_fire_score")),
                IsQualified = reader.GetInt32(reader.GetOrdinal("is_qualified")) == 1,
                InstructorName = reader.IsDBNull(reader.GetOrdinal("instructor_name")) ? null :
                                reader.GetString(reader.GetOrdinal("instructor_name")),
                InstructorRankRate = reader.IsDBNull(reader.GetOrdinal("instructor_rank_rate")) ? null :
                                    reader.GetString(reader.GetOrdinal("instructor_rank_rate")),
                RsoSignature = reader.IsDBNull(reader.GetOrdinal("rso_signature")) ? null :
                              reader.GetString(reader.GetOrdinal("rso_signature")),
                RsoSignatureRate = reader.IsDBNull(reader.GetOrdinal("rso_signature_rate")) ? null :
                                  reader.GetString(reader.GetOrdinal("rso_signature_rate")),
                RsoSignatureDate = reader.IsDBNull(reader.GetOrdinal("rso_signature_date")) ? null :
                                  DateTime.Parse(reader.GetString(reader.GetOrdinal("rso_signature_date"))),
                PdfFilePath = reader.IsDBNull(reader.GetOrdinal("pdf_file_path")) ? null :
                             reader.GetString(reader.GetOrdinal("pdf_file_path")),
                CreatedDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_date")))
            };
        }
    }
}
