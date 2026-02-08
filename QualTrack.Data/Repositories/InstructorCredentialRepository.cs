using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public class InstructorCredentialRepository
    {
        public async Task<InstructorDesignation?> GetDesignationAsync(DatabaseContext dbContext, int personnelId, string role)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, role, designation_date, pdf_file_path, pdf_file_name, date_created, date_modified
                FROM instructor_designations
                WHERE personnel_id = @personnelId AND role = @role
                LIMIT 1";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@role", role);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InstructorDesignation
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                    Role = reader.GetString(reader.GetOrdinal("role")),
                    DesignationDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("designation_date"))),
                    PdfFilePath = reader.IsDBNull(reader.GetOrdinal("pdf_file_path")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_path")),
                    PdfFileName = reader.IsDBNull(reader.GetOrdinal("pdf_file_name")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_name")),
                    DateCreated = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_created"))),
                    DateModified = reader.IsDBNull(reader.GetOrdinal("date_modified")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("date_modified")))
                };
            }

            return null;
        }

        public async Task<int> AddDesignationAsync(DatabaseContext dbContext, InstructorDesignation designation)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO instructor_designations (
                    personnel_id, role, designation_date, pdf_file_path, pdf_file_name, date_created, date_modified
                ) VALUES (
                    @personnelId, @role, @designationDate, @pdfFilePath, @pdfFileName, @dateCreated, @dateModified
                );
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@personnelId", designation.PersonnelId);
            command.Parameters.AddWithValue("@role", designation.Role);
            command.Parameters.AddWithValue("@designationDate", designation.DesignationDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@pdfFilePath", designation.PdfFilePath);
            command.Parameters.AddWithValue("@pdfFileName", designation.PdfFileName);
            command.Parameters.AddWithValue("@dateCreated", designation.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@dateModified", designation.DateModified?.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateDesignationAsync(DatabaseContext dbContext, InstructorDesignation designation)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE instructor_designations SET
                    designation_date = @designationDate,
                    pdf_file_path = @pdfFilePath,
                    pdf_file_name = @pdfFileName,
                    date_modified = @dateModified
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", designation.Id);
            command.Parameters.AddWithValue("@designationDate", designation.DesignationDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@pdfFilePath", designation.PdfFilePath);
            command.Parameters.AddWithValue("@pdfFileName", designation.PdfFileName);
            command.Parameters.AddWithValue("@dateModified", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> AddQualificationAsync(DatabaseContext dbContext, InstructorQualification qualification)
        {
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO instructor_qualifications (
                    personnel_id, role, qualification_date, qualification_type, pdf_file_path, pdf_file_name, date_created
                ) VALUES (
                    @personnelId, @role, @qualificationDate, @qualificationType, @pdfFilePath, @pdfFileName, @dateCreated
                );
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@personnelId", qualification.PersonnelId);
            command.Parameters.AddWithValue("@role", qualification.Role);
            command.Parameters.AddWithValue("@qualificationDate", qualification.QualificationDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@qualificationType", qualification.QualificationType);
            command.Parameters.AddWithValue("@pdfFilePath", qualification.PdfFilePath);
            command.Parameters.AddWithValue("@pdfFileName", qualification.PdfFileName);
            command.Parameters.AddWithValue("@dateCreated", qualification.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<InstructorQualification>> GetQualificationsAsync(DatabaseContext dbContext, int personnelId, string role)
        {
            var qualifications = new List<InstructorQualification>();
            using var connection = dbContext.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, role, qualification_date, qualification_type, pdf_file_path, pdf_file_name, date_created
                FROM instructor_qualifications
                WHERE personnel_id = @personnelId AND role = @role
                ORDER BY qualification_date DESC";
            command.Parameters.AddWithValue("@personnelId", personnelId);
            command.Parameters.AddWithValue("@role", role);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                qualifications.Add(new InstructorQualification
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                    Role = reader.GetString(reader.GetOrdinal("role")),
                    QualificationDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("qualification_date"))),
                    QualificationType = reader.GetString(reader.GetOrdinal("qualification_type")),
                    PdfFilePath = reader.IsDBNull(reader.GetOrdinal("pdf_file_path")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_path")),
                    PdfFileName = reader.IsDBNull(reader.GetOrdinal("pdf_file_name")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_name")),
                    DateCreated = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_created")))
                });
            }

            return qualifications;
        }
    }
}
