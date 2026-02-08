using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public class SignatureQueueRepository : ISignatureQueueRepository
    {
        public async Task<int> AddAsync(DatabaseContext context, SignatureQueueItem item)
        {
            using var connection = context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO signature_queue
                (document_id, document_path, form_type, personnel_id, status, current_role, required_roles, completed_roles,
                 claimed_by, claimed_at, last_action, created_at, updated_at)
                VALUES (@document_id, @document_path, @form_type, @personnel_id, @status, @current_role, @required_roles, @completed_roles,
                        @claimed_by, @claimed_at, @last_action, @created_at, @updated_at);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@document_id", item.DocumentId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@document_path", item.DocumentPath);
            command.Parameters.AddWithValue("@form_type", item.FormType);
            command.Parameters.AddWithValue("@personnel_id", item.PersonnelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", item.Status);
            command.Parameters.AddWithValue("@current_role", item.CurrentRole);
            command.Parameters.AddWithValue("@required_roles", item.RequiredRoles);
            command.Parameters.AddWithValue("@completed_roles", item.CompletedRoles ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@claimed_by", item.ClaimedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@claimed_at", item.ClaimedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@last_action", item.LastAction ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@created_at", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@updated_at", item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            item.Id = id;
            return id;
        }

        public async Task UpdateAsync(DatabaseContext context, SignatureQueueItem item)
        {
            using var connection = context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE signature_queue
                SET document_id = @document_id,
                    document_path = @document_path,
                    form_type = @form_type,
                    personnel_id = @personnel_id,
                    status = @status,
                    current_role = @current_role,
                    required_roles = @required_roles,
                    completed_roles = @completed_roles,
                    claimed_by = @claimed_by,
                    claimed_at = @claimed_at,
                    last_action = @last_action,
                    updated_at = @updated_at
                WHERE id = @id;";

            command.Parameters.AddWithValue("@id", item.Id);
            command.Parameters.AddWithValue("@document_id", item.DocumentId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@document_path", item.DocumentPath);
            command.Parameters.AddWithValue("@form_type", item.FormType);
            command.Parameters.AddWithValue("@personnel_id", item.PersonnelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", item.Status);
            command.Parameters.AddWithValue("@current_role", item.CurrentRole);
            command.Parameters.AddWithValue("@required_roles", item.RequiredRoles);
            command.Parameters.AddWithValue("@completed_roles", item.CompletedRoles ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@claimed_by", item.ClaimedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@claimed_at", item.ClaimedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@last_action", item.LastAction ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@updated_at", item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<SignatureQueueItem?> GetByIdAsync(DatabaseContext context, int id)
        {
            using var connection = context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, document_id, document_path, form_type, personnel_id, status, current_role, required_roles, completed_roles,
                       claimed_by, claimed_at, last_action, created_at, updated_at
                FROM signature_queue
                WHERE id = @id;";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapItem(reader);
            }

            return null;
        }

        public async Task<List<SignatureQueueItem>> GetInboxAsync(DatabaseContext context, string? role, string? status, string? formType)
        {
            using var connection = context.GetConnection();
            var command = connection.CreateCommand();
            var conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "All", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add("current_role = @role");
                command.Parameters.AddWithValue("@role", role);
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add("status = @status");
                command.Parameters.AddWithValue("@status", status);
            }

            if (!string.IsNullOrWhiteSpace(formType) && !string.Equals(formType, "All", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add("form_type = @formType");
                command.Parameters.AddWithValue("@formType", formType);
            }

            var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;
            command.CommandText = $@"
                SELECT id, document_id, document_path, form_type, personnel_id, status, current_role, required_roles, completed_roles,
                       claimed_by, claimed_at, last_action, created_at, updated_at
                FROM signature_queue
                {whereClause}
                ORDER BY created_at DESC;";

            var results = new List<SignatureQueueItem>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapItem(reader));
            }

            return results;
        }

        private static SignatureQueueItem MapItem(DbDataReader reader)
        {
            return new SignatureQueueItem
            {
                Id = reader.GetInt32(0),
                DocumentId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                DocumentPath = reader.GetString(2),
                FormType = reader.GetString(3),
                PersonnelId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Status = reader.GetString(5),
                CurrentRole = reader.GetString(6),
                RequiredRoles = reader.GetString(7),
                CompletedRoles = reader.IsDBNull(8) ? null : reader.GetString(8),
                ClaimedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
                ClaimedAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
                LastAction = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = DateTime.Parse(reader.GetString(12)),
                UpdatedAt = DateTime.Parse(reader.GetString(13))
            };
        }
    }
}
