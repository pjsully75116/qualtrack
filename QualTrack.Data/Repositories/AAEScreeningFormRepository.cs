using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public class AAEScreeningFormRepository : IAAEScreeningFormRepository
    {
        public Task<AAEScreeningForm?> GetByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            return Task.FromResult(GetByPersonnelId(dbContext, personnelId));
        }

        public Task<List<AAEScreeningForm>> GetAllByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            return Task.FromResult(GetAllByPersonnelId(dbContext, personnelId));
        }

        private List<AAEScreeningForm> GetAllByPersonnelId(DatabaseContext dbContext, int personnelId)
        {
            var forms = new List<AAEScreeningForm>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                       name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                       question1_response, question2_response, question3_response, question4_response,
                       question5_response, question6_response, question7_response,
                       remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                       qualified, unqualified, review_later, other_qualified_field,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM aae_screening_forms 
                WHERE personnel_id = @personnelId AND is_valid = 1
                ORDER BY date_completed DESC";
            
            var personnelIdParameter = command.CreateParameter();
            personnelIdParameter.ParameterName = "@personnelId";
            personnelIdParameter.Value = personnelId;
            command.Parameters.Add(personnelIdParameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forms.Add(MapReaderToForm(reader));
            }

            return forms;
        }

        private AAEScreeningForm? GetByPersonnelId(DatabaseContext dbContext, int personnelId)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                       name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                       question1_response, question2_response, question3_response, question4_response,
                       question5_response, question6_response, question7_response,
                       remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                       qualified, unqualified, review_later, other_qualified_field,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM aae_screening_forms 
                WHERE personnel_id = @personnelId AND is_valid = 1
                ORDER BY date_completed DESC
                LIMIT 1";
            
            var personnelIdParameter = command.CreateParameter();
            personnelIdParameter.ParameterName = "@personnelId";
            personnelIdParameter.Value = personnelId;
            command.Parameters.Add(personnelIdParameter);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToForm(reader);
            }

            return null;
        }

        public Task<AAEScreeningForm?> GetByIdAsync(DatabaseContext dbContext, int id)
        {
            return Task.FromResult(GetById(dbContext, id));
        }

        private AAEScreeningForm? GetById(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                       name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                       question1_response, question2_response, question3_response, question4_response,
                       question5_response, question6_response, question7_response,
                       remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                       qualified, unqualified, review_later, other_qualified_field,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM aae_screening_forms 
                WHERE id = @id";
            
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@id";
            idParameter.Value = id;
            command.Parameters.Add(idParameter);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToForm(reader);
            }

            return null;
        }

        public Task<List<AAEScreeningForm>> GetAllAsync(DatabaseContext dbContext)
        {
            return Task.FromResult(GetAll(dbContext));
        }

        private List<AAEScreeningForm> GetAll(DatabaseContext dbContext)
        {
            var forms = new List<AAEScreeningForm>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                       name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                       question1_response, question2_response, question3_response, question4_response,
                       question5_response, question6_response, question7_response,
                       remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                       qualified, unqualified, review_later, other_qualified_field,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM aae_screening_forms 
                WHERE is_valid = 1
                ORDER BY date_completed DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forms.Add(MapReaderToForm(reader));
            }

            return forms;
        }

        public Task<List<AAEScreeningForm>> GetExpiringFormsAsync(DatabaseContext dbContext, int daysThreshold)
        {
            return Task.FromResult(GetExpiringForms(dbContext, daysThreshold));
        }

        private List<AAEScreeningForm> GetExpiringForms(DatabaseContext dbContext, int daysThreshold)
        {
            var forms = new List<AAEScreeningForm>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                       name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                       question1_response, question2_response, question3_response, question4_response,
                       question5_response, question6_response, question7_response,
                       remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                       qualified, unqualified, review_later, other_qualified_field,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM aae_screening_forms 
                WHERE is_valid = 1 
                AND date_expires <= @expiryDate
                ORDER BY date_expires ASC";

            var expiryDate = DateTime.Now.AddDays(daysThreshold).ToString("yyyy-MM-dd");
            var expiryDateParameter = command.CreateParameter();
            expiryDateParameter.ParameterName = "@expiryDate";
            expiryDateParameter.Value = expiryDate;
            command.Parameters.Add(expiryDateParameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forms.Add(MapReaderToForm(reader));
            }

            return forms;
        }

        public Task<int> AddAsync(DatabaseContext dbContext, AAEScreeningForm form)
        {
            return Task.FromResult(Add(dbContext, form));
        }

        private int Add(DatabaseContext dbContext, AAEScreeningForm form)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO aae_screening_forms (
                    personnel_id, date_completed, date_expires, date_created, date_modified,
                    name_screened, rank_screened, dodid_screened, signature_screened, date_screened,
                    name_screener, rank_screener, dodid_screener, signature_screener, date_screener,
                    question1_response, question2_response, question3_response, question4_response,
                    question5_response, question6_response, question7_response,
                    remarks1, remarks2, remarks3, remarks4, remarks5, remarks6, remarks7,
                    qualified, unqualified, review_later, other_qualified_field,
                    pdf_file_path, pdf_file_name, is_valid, status_notes
                ) VALUES (
                    @personnelId, @dateCompleted, @dateExpires, @dateCreated, @dateModified,
                    @nameScreened, @rankScreened, @dodidScreened, @signatureScreened, @dateScreened,
                    @nameScreener, @rankScreener, @dodidScreener, @signatureScreener, @dateScreener,
                    @question1Response, @question2Response, @question3Response, @question4Response,
                    @question5Response, @question6Response, @question7Response,
                    @remarks1, @remarks2, @remarks3, @remarks4, @remarks5, @remarks6, @remarks7,
                    @qualified, @unqualified, @reviewLater, @otherQualifiedField,
                    @pdfFilePath, @pdfFileName, @isValid, @statusNotes
                );
                SELECT last_insert_rowid();";

            // Add parameters
            AddParameter(command, "@personnelId", form.PersonnelId);
            AddParameter(command, "@dateCompleted", form.DateCompleted.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateExpires", form.DateExpires.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateCreated", form.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
            AddParameter(command, "@dateModified", form.DateModified?.ToString("yyyy-MM-dd HH:mm:ss"));
            AddParameter(command, "@nameScreened", form.NameScreened);
            AddParameter(command, "@rankScreened", form.RankScreened);
            AddParameter(command, "@dodidScreened", form.DODIDScreened);
            AddParameter(command, "@signatureScreened", form.SignatureScreened);
            AddParameter(command, "@dateScreened", form.DateScreened?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@nameScreener", form.NameScreener);
            AddParameter(command, "@rankScreener", form.RankScreener);
            AddParameter(command, "@dodidScreener", form.DODIDScreener);
            AddParameter(command, "@signatureScreener", form.SignatureScreener);
            AddParameter(command, "@dateScreener", form.DateScreener?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@question1Response", form.Question1Response);
            AddParameter(command, "@question2Response", form.Question2Response);
            AddParameter(command, "@question3Response", form.Question3Response);
            AddParameter(command, "@question4Response", form.Question4Response);
            AddParameter(command, "@question5Response", form.Question5Response);
            AddParameter(command, "@question6Response", form.Question6Response);
            AddParameter(command, "@question7Response", form.Question7Response);
            AddParameter(command, "@remarks1", form.Remarks1);
            AddParameter(command, "@remarks2", form.Remarks2);
            AddParameter(command, "@remarks3", form.Remarks3);
            AddParameter(command, "@remarks4", form.Remarks4);
            AddParameter(command, "@remarks5", form.Remarks5);
            AddParameter(command, "@remarks6", form.Remarks6);
            AddParameter(command, "@remarks7", form.Remarks7);
            AddParameter(command, "@qualified", form.Qualified ? 1 : 0);
            AddParameter(command, "@unqualified", form.Unqualified ? 1 : 0);
            AddParameter(command, "@reviewLater", form.ReviewLater ? 1 : 0);
            AddParameter(command, "@otherQualifiedField", form.OtherQualifiedField);
            AddParameter(command, "@pdfFilePath", form.PdfFilePath);
            AddParameter(command, "@pdfFileName", form.PdfFileName);
            AddParameter(command, "@isValid", form.IsValid ? 1 : 0);
            AddParameter(command, "@statusNotes", form.StatusNotes);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public Task<bool> UpdateAsync(DatabaseContext dbContext, AAEScreeningForm form)
        {
            return Task.FromResult(Update(dbContext, form));
        }

        private bool Update(DatabaseContext dbContext, AAEScreeningForm form)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE aae_screening_forms SET
                    date_completed = @dateCompleted,
                    date_expires = @dateExpires,
                    date_modified = @dateModified,
                    name_screened = @nameScreened,
                    rank_screened = @rankScreened,
                    dodid_screened = @dodidScreened,
                    signature_screened = @signatureScreened,
                    date_screened = @dateScreened,
                    name_screener = @nameScreener,
                    rank_screener = @rankScreener,
                    dodid_screener = @dodidScreener,
                    signature_screener = @signatureScreener,
                    date_screener = @dateScreener,
                    question1_response = @question1Response,
                    question2_response = @question2Response,
                    question3_response = @question3Response,
                    question4_response = @question4Response,
                    question5_response = @question5Response,
                    question6_response = @question6Response,
                    question7_response = @question7Response,
                    remarks1 = @remarks1,
                    remarks2 = @remarks2,
                    remarks3 = @remarks3,
                    remarks4 = @remarks4,
                    remarks5 = @remarks5,
                    remarks6 = @remarks6,
                    remarks7 = @remarks7,
                    qualified = @qualified,
                    unqualified = @unqualified,
                    review_later = @reviewLater,
                    other_qualified_field = @otherQualifiedField,
                    pdf_file_path = @pdfFilePath,
                    pdf_file_name = @pdfFileName,
                    is_valid = @isValid,
                    status_notes = @statusNotes
                WHERE id = @id";

            // Add parameters
            AddParameter(command, "@id", form.Id);
            AddParameter(command, "@dateCompleted", form.DateCompleted.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateExpires", form.DateExpires.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateModified", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            AddParameter(command, "@nameScreened", form.NameScreened);
            AddParameter(command, "@rankScreened", form.RankScreened);
            AddParameter(command, "@dodidScreened", form.DODIDScreened);
            AddParameter(command, "@signatureScreened", form.SignatureScreened);
            AddParameter(command, "@dateScreened", form.DateScreened?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@nameScreener", form.NameScreener);
            AddParameter(command, "@rankScreener", form.RankScreener);
            AddParameter(command, "@dodidScreener", form.DODIDScreener);
            AddParameter(command, "@signatureScreener", form.SignatureScreener);
            AddParameter(command, "@dateScreener", form.DateScreener?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@question1Response", form.Question1Response);
            AddParameter(command, "@question2Response", form.Question2Response);
            AddParameter(command, "@question3Response", form.Question3Response);
            AddParameter(command, "@question4Response", form.Question4Response);
            AddParameter(command, "@question5Response", form.Question5Response);
            AddParameter(command, "@question6Response", form.Question6Response);
            AddParameter(command, "@question7Response", form.Question7Response);
            AddParameter(command, "@remarks1", form.Remarks1);
            AddParameter(command, "@remarks2", form.Remarks2);
            AddParameter(command, "@remarks3", form.Remarks3);
            AddParameter(command, "@remarks4", form.Remarks4);
            AddParameter(command, "@remarks5", form.Remarks5);
            AddParameter(command, "@remarks6", form.Remarks6);
            AddParameter(command, "@remarks7", form.Remarks7);
            AddParameter(command, "@qualified", form.Qualified ? 1 : 0);
            AddParameter(command, "@unqualified", form.Unqualified ? 1 : 0);
            AddParameter(command, "@reviewLater", form.ReviewLater ? 1 : 0);
            AddParameter(command, "@otherQualifiedField", form.OtherQualifiedField);
            AddParameter(command, "@pdfFilePath", form.PdfFilePath);
            AddParameter(command, "@pdfFileName", form.PdfFileName);
            AddParameter(command, "@isValid", form.IsValid ? 1 : 0);
            AddParameter(command, "@statusNotes", form.StatusNotes);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public Task<bool> DeleteAsync(DatabaseContext dbContext, int id)
        {
            return Task.FromResult(Delete(dbContext, id));
        }

        private bool Delete(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE aae_screening_forms SET is_valid = 0 WHERE id = @id";

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@id";
            idParameter.Value = id;
            command.Parameters.Add(idParameter);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public Task<bool> DeleteByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            return Task.FromResult(DeleteByPersonnelId(dbContext, personnelId));
        }

        private bool DeleteByPersonnelId(DatabaseContext dbContext, int personnelId)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE aae_screening_forms SET is_valid = 0 WHERE personnel_id = @personnelId";

            var personnelIdParameter = command.CreateParameter();
            personnelIdParameter.ParameterName = "@personnelId";
            personnelIdParameter.Value = personnelId;
            command.Parameters.Add(personnelIdParameter);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        private AAEScreeningForm MapReaderToForm(IDataReader reader)
        {
            return new AAEScreeningForm
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                PersonnelId = reader.GetInt32(reader.GetOrdinal("personnel_id")),
                DateCompleted = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_completed"))),
                DateExpires = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_expires"))),
                DateCreated = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_created"))),
                DateModified = reader.IsDBNull(reader.GetOrdinal("date_modified")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("date_modified"))),
                NameScreened = reader.IsDBNull(reader.GetOrdinal("name_screened")) ? null : reader.GetString(reader.GetOrdinal("name_screened")),
                RankScreened = reader.IsDBNull(reader.GetOrdinal("rank_screened")) ? null : reader.GetString(reader.GetOrdinal("rank_screened")),
                DODIDScreened = reader.IsDBNull(reader.GetOrdinal("dodid_screened")) ? null : reader.GetString(reader.GetOrdinal("dodid_screened")),
                SignatureScreened = reader.IsDBNull(reader.GetOrdinal("signature_screened")) ? null : reader.GetString(reader.GetOrdinal("signature_screened")),
                DateScreened = reader.IsDBNull(reader.GetOrdinal("date_screened")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("date_screened"))),
                NameScreener = reader.IsDBNull(reader.GetOrdinal("name_screener")) ? null : reader.GetString(reader.GetOrdinal("name_screener")),
                RankScreener = reader.IsDBNull(reader.GetOrdinal("rank_screener")) ? null : reader.GetString(reader.GetOrdinal("rank_screener")),
                DODIDScreener = reader.IsDBNull(reader.GetOrdinal("dodid_screener")) ? null : reader.GetString(reader.GetOrdinal("dodid_screener")),
                SignatureScreener = reader.IsDBNull(reader.GetOrdinal("signature_screener")) ? null : reader.GetString(reader.GetOrdinal("signature_screener")),
                DateScreener = reader.IsDBNull(reader.GetOrdinal("date_screener")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("date_screener"))),
                Question1Response = reader.IsDBNull(reader.GetOrdinal("question1_response")) ? null : reader.GetString(reader.GetOrdinal("question1_response")),
                Question2Response = reader.IsDBNull(reader.GetOrdinal("question2_response")) ? null : reader.GetString(reader.GetOrdinal("question2_response")),
                Question3Response = reader.IsDBNull(reader.GetOrdinal("question3_response")) ? null : reader.GetString(reader.GetOrdinal("question3_response")),
                Question4Response = reader.IsDBNull(reader.GetOrdinal("question4_response")) ? null : reader.GetString(reader.GetOrdinal("question4_response")),
                Question5Response = reader.IsDBNull(reader.GetOrdinal("question5_response")) ? null : reader.GetString(reader.GetOrdinal("question5_response")),
                Question6Response = reader.IsDBNull(reader.GetOrdinal("question6_response")) ? null : reader.GetString(reader.GetOrdinal("question6_response")),
                Question7Response = reader.IsDBNull(reader.GetOrdinal("question7_response")) ? null : reader.GetString(reader.GetOrdinal("question7_response")),
                Remarks1 = reader.IsDBNull(reader.GetOrdinal("remarks1")) ? null : reader.GetString(reader.GetOrdinal("remarks1")),
                Remarks2 = reader.IsDBNull(reader.GetOrdinal("remarks2")) ? null : reader.GetString(reader.GetOrdinal("remarks2")),
                Remarks3 = reader.IsDBNull(reader.GetOrdinal("remarks3")) ? null : reader.GetString(reader.GetOrdinal("remarks3")),
                Remarks4 = reader.IsDBNull(reader.GetOrdinal("remarks4")) ? null : reader.GetString(reader.GetOrdinal("remarks4")),
                Remarks5 = reader.IsDBNull(reader.GetOrdinal("remarks5")) ? null : reader.GetString(reader.GetOrdinal("remarks5")),
                Remarks6 = reader.IsDBNull(reader.GetOrdinal("remarks6")) ? null : reader.GetString(reader.GetOrdinal("remarks6")),
                Remarks7 = reader.IsDBNull(reader.GetOrdinal("remarks7")) ? null : reader.GetString(reader.GetOrdinal("remarks7")),
                Qualified = !reader.IsDBNull(reader.GetOrdinal("qualified")) && reader.GetInt32(reader.GetOrdinal("qualified")) == 1,
                Unqualified = !reader.IsDBNull(reader.GetOrdinal("unqualified")) && reader.GetInt32(reader.GetOrdinal("unqualified")) == 1,
                ReviewLater = !reader.IsDBNull(reader.GetOrdinal("review_later")) && reader.GetInt32(reader.GetOrdinal("review_later")) == 1,
                OtherQualifiedField = reader.IsDBNull(reader.GetOrdinal("other_qualified_field")) ? null : reader.GetString(reader.GetOrdinal("other_qualified_field")),
                PdfFilePath = reader.IsDBNull(reader.GetOrdinal("pdf_file_path")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_path")),
                PdfFileName = reader.IsDBNull(reader.GetOrdinal("pdf_file_name")) ? null : reader.GetString(reader.GetOrdinal("pdf_file_name")),
                IsValid = reader.GetInt32(reader.GetOrdinal("is_valid")) == 1,
                StatusNotes = reader.IsDBNull(reader.GetOrdinal("status_notes")) ? null : reader.GetString(reader.GetOrdinal("status_notes"))
            };
        }

        private void AddParameter(IDbCommand command, string parameterName, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
