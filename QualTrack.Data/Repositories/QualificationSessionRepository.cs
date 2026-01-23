using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public class QualificationSessionRepository
    {
        public async Task<int> AddSessionAsync(DatabaseContext dbContext, QualificationSession session)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO qualification_sessions
                (ship_station, division_activity, weapons_fired, range_name_location, date_of_firing, rso_signature, rso_signature_rate, rso_signature_date, pdf_file_path, created_date)
                VALUES (@ship_station, @division_activity, @weapons_fired, @range_name_location, @date_of_firing, @rso_signature, @rso_signature_rate, @rso_signature_date, @pdf_file_path, @created_date);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@ship_station", session.ShipStation);
            command.Parameters.AddWithValue("@division_activity", session.DivisionActivity);
            command.Parameters.AddWithValue("@weapons_fired", session.WeaponsFired);
            command.Parameters.AddWithValue("@range_name_location", session.RangeNameLocation);
            command.Parameters.AddWithValue("@date_of_firing", session.DateOfFiring?.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@rso_signature", session.RsoSignature ?? "");
            command.Parameters.AddWithValue("@rso_signature_rate", session.RsoSignatureRate ?? "");
            command.Parameters.AddWithValue("@rso_signature_date", session.RsoSignatureDate?.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@pdf_file_path", session.PdfFilePath ?? "");
            command.Parameters.AddWithValue("@created_date", session.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<QualificationSession?> GetSessionByIdAsync(DatabaseContext dbContext, int sessionId)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, ship_station, division_activity, weapons_fired, range_name_location, 
                       date_of_firing, rso_signature, rso_signature_rate, rso_signature_date, 
                       pdf_file_path, created_date
                FROM qualification_sessions
                WHERE id = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new QualificationSession
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ShipStation = reader.GetString(reader.GetOrdinal("ship_station")),
                    DivisionActivity = reader.GetString(reader.GetOrdinal("division_activity")),
                    WeaponsFired = reader.GetString(reader.GetOrdinal("weapons_fired")),
                    RangeNameLocation = reader.GetString(reader.GetOrdinal("range_name_location")),
                    DateOfFiring = reader.IsDBNull(reader.GetOrdinal("date_of_firing")) ? null : 
                                  DateTime.Parse(reader.GetString(reader.GetOrdinal("date_of_firing"))),
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
            
            return null;
        }

        public async Task UpdateSessionPdfFilePathAsync(DatabaseContext dbContext, int sessionId, string pdfFilePath)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE qualification_sessions
                SET pdf_file_path = @pdf_file_path
                WHERE id = @sessionId";
            command.Parameters.AddWithValue("@pdf_file_path", pdfFilePath ?? string.Empty);
            command.Parameters.AddWithValue("@sessionId", sessionId);

            await command.ExecuteNonQueryAsync();
        }
    }
} 