using System.Data.SQLite;
using QualTrack.Data.Database;

namespace QualTrack.Data.Services
{
    /// <summary>
    /// Service for logging audit events for security compliance
    /// </summary>
    public class AuditService
    {
        private readonly DatabaseContext _dbContext;

        public AuditService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Logs an audit event
        /// </summary>
        /// <param name="action">The action performed</param>
        /// <param name="details">Additional details about the action</param>
        /// <param name="userId">Optional user ID (for future role-based access)</param>
        public async Task LogEventAsync(string action, string? details = null, int? userId = null)
        {
            try
            {
                var connection = _dbContext.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO audit_log (timestamp, user_id, action, details) 
                    VALUES (@timestamp, @userId, @action, @details)";
                
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@action", action);
                command.Parameters.AddWithValue("@details", details ?? (object)DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log to console for now - in production this would go to a proper logging system
                Console.WriteLine($"Audit logging failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs personnel-related actions
        /// </summary>
        public async Task LogPersonnelActionAsync(string action, string personnelName, string rate, int? userId = null)
        {
            var details = $"Personnel: {personnelName} ({rate})";
            await LogEventAsync(action, details, userId);
        }

        /// <summary>
        /// Logs qualification-related actions
        /// </summary>
        public async Task LogQualificationActionAsync(string action, string personnel, string weapon, int category)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO audit_log (action, personnel, weapon, category, timestamp) 
                VALUES (@action, @personnel, @weapon, @category, @timestamp)";
            command.Parameters.AddWithValue("@action", action);
            command.Parameters.AddWithValue("@personnel", personnel);
            command.Parameters.AddWithValue("@weapon", weapon);
            command.Parameters.AddWithValue("@category", category);
            command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            await command.ExecuteNonQueryAsync();
        }

        public async Task LogQualificationAddedAsync(int personnelId, string weapon, int category, DateTime dateQualified)
        {
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO audit_log (action, personnel_id, weapon, category, qualification_date, timestamp) 
                VALUES (@action, @personnelId, @weapon, @category, @qualificationDate, @timestamp)";
            command.Parameters.AddWithValue("@action", "Qualification Added");
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@weapon", weapon);
            command.Parameters.AddWithValue("@category", category);
            command.Parameters.AddWithValue("@qualificationDate", dateQualified.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Logs data export actions
        /// </summary>
        public async Task LogExportActionAsync(string fileName, int recordCount, int? userId = null)
        {
            var details = $"File: {fileName}, Records: {recordCount}";
            await LogEventAsync("Data Export", details, userId);
        }

        /// <summary>
        /// Gets audit log entries for a date range
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAuditLogAsync(DateTime startDate, DateTime endDate)
        {
            var entries = new List<AuditLogEntry>();
            
            var connection = _dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT timestamp, user_id, action, details 
                FROM audit_log 
                WHERE timestamp BETWEEN @startDate AND @endDate 
                ORDER BY timestamp DESC";
            
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var entry = new AuditLogEntry
                {
                    Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp"))),
                    UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32(reader.GetOrdinal("user_id")),
                    Action = reader.GetString(reader.GetOrdinal("action")),
                    Details = reader.IsDBNull(reader.GetOrdinal("details")) ? null : reader.GetString(reader.GetOrdinal("details"))
                };
                
                entries.Add(entry);
            }
            
            return entries;
        }
    }

    /// <summary>
    /// Represents an audit log entry
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
} 