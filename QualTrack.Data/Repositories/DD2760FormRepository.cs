using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    public class DD2760FormRepository : IDD2760FormRepository
    {
        public Task<DD2760Form?> GetByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            return Task.FromResult(GetByPersonnelId(dbContext, personnelId));
        }

        public Task<List<DD2760Form>> GetAllByPersonnelIdAsync(DatabaseContext dbContext, int personnelId)
        {
            return Task.FromResult(GetAllByPersonnelId(dbContext, personnelId));
        }

        private List<DD2760Form> GetAllByPersonnelId(DatabaseContext dbContext, int personnelId)
        {
            var forms = new List<DD2760Form>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                       court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                       certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM dd2760_forms 
                WHERE personnel_id = @personnelId AND is_valid = 1
                ORDER BY date_completed DESC";
            
            var personnelIdParameter = command.CreateParameter();
            personnelIdParameter.ParameterName = "@personnelId";
            personnelIdParameter.Value = personnelId;
            command.Parameters.Add(personnelIdParameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forms.Add(new DD2760Form
                {
                    Id = reader.GetInt32("id"),
                    PersonnelId = reader.GetInt32("personnel_id"),
                    DateCompleted = DateTime.Parse(reader.GetString("date_completed")),
                    DateExpires = DateTime.Parse(reader.GetString("date_expires")),
                    DateCreated = DateTime.Parse(reader.GetString("date_created")),
                    DateModified = reader.IsDBNull("date_modified") ? null : DateTime.Parse(reader.GetString("date_modified")),
                    DomesticViolenceResponse = reader.IsDBNull("domestic_violence_response") ? null : reader.GetString("domestic_violence_response"),
                    DomesticViolenceInitials = reader.IsDBNull("domestic_violence_initials") ? null : reader.GetString("domestic_violence_initials"),
                    DomesticViolenceDate = reader.IsDBNull("domestic_violence_date") ? null : DateTime.Parse(reader.GetString("domestic_violence_date")),
                    CourtJurisdiction = reader.IsDBNull("court_jurisdiction") ? null : reader.GetString("court_jurisdiction"),
                    DocketCaseNumber = reader.IsDBNull("docket_case_number") ? null : reader.GetString("docket_case_number"),
                    StatuteCharge = reader.IsDBNull("statute_charge") ? null : reader.GetString("statute_charge"),
                    DateSentenced = reader.IsDBNull("date_sentenced") ? null : DateTime.Parse(reader.GetString("date_sentenced")),
                    CertifierName = reader.IsDBNull("certifier_name") ? null : reader.GetString("certifier_name"),
                    CertifierRank = reader.IsDBNull("certifier_rank") ? null : reader.GetString("certifier_rank"),
                    CertifierSSN = reader.IsDBNull("certifier_ssn") ? null : reader.GetString("certifier_ssn"),
                    CertifierOrganization = reader.IsDBNull("certifier_organization") ? null : reader.GetString("certifier_organization"),
                    IsCertified = !reader.IsDBNull("is_certified") && reader.GetInt32("is_certified") == 1,
                    CertifierSignatureDate = reader.IsDBNull("certifier_signature_date") ? null : DateTime.Parse(reader.GetString("certifier_signature_date")),
                    PdfFilePath = reader.IsDBNull("pdf_file_path") ? null : reader.GetString("pdf_file_path"),
                    PdfFileName = reader.IsDBNull("pdf_file_name") ? null : reader.GetString("pdf_file_name"),
                    IsValid = reader.GetInt32("is_valid") == 1,
                    StatusNotes = reader.IsDBNull("status_notes") ? null : reader.GetString("status_notes")
                });
            }

            return forms;
        }

        private DD2760Form? GetByPersonnelId(DatabaseContext dbContext, int personnelId)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                       court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                       certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM dd2760_forms 
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
                return new DD2760Form
                {
                    Id = reader.GetInt32("id"),
                    PersonnelId = reader.GetInt32("personnel_id"),
                    DateCompleted = DateTime.Parse(reader.GetString("date_completed")),
                    DateExpires = DateTime.Parse(reader.GetString("date_expires")),
                    DateCreated = DateTime.Parse(reader.GetString("date_created")),
                    DateModified = reader.IsDBNull("date_modified") ? null : DateTime.Parse(reader.GetString("date_modified")),
                    DomesticViolenceResponse = reader.IsDBNull("domestic_violence_response") ? null : reader.GetString("domestic_violence_response"),
                    DomesticViolenceInitials = reader.IsDBNull("domestic_violence_initials") ? null : reader.GetString("domestic_violence_initials"),
                    DomesticViolenceDate = reader.IsDBNull("domestic_violence_date") ? null : DateTime.Parse(reader.GetString("domestic_violence_date")),
                    CourtJurisdiction = reader.IsDBNull("court_jurisdiction") ? null : reader.GetString("court_jurisdiction"),
                    DocketCaseNumber = reader.IsDBNull("docket_case_number") ? null : reader.GetString("docket_case_number"),
                    StatuteCharge = reader.IsDBNull("statute_charge") ? null : reader.GetString("statute_charge"),
                    DateSentenced = reader.IsDBNull("date_sentenced") ? null : DateTime.Parse(reader.GetString("date_sentenced")),
                    CertifierName = reader.IsDBNull("certifier_name") ? null : reader.GetString("certifier_name"),
                    CertifierRank = reader.IsDBNull("certifier_rank") ? null : reader.GetString("certifier_rank"),
                    CertifierSSN = reader.IsDBNull("certifier_ssn") ? null : reader.GetString("certifier_ssn"),
                    CertifierOrganization = reader.IsDBNull("certifier_organization") ? null : reader.GetString("certifier_organization"),
                    IsCertified = !reader.IsDBNull("is_certified") && reader.GetInt32("is_certified") == 1,
                    CertifierSignatureDate = reader.IsDBNull("certifier_signature_date") ? null : DateTime.Parse(reader.GetString("certifier_signature_date")),
                    PdfFilePath = reader.IsDBNull("pdf_file_path") ? null : reader.GetString("pdf_file_path"),
                    PdfFileName = reader.IsDBNull("pdf_file_name") ? null : reader.GetString("pdf_file_name"),
                    IsValid = reader.GetInt32("is_valid") == 1,
                    StatusNotes = reader.IsDBNull("status_notes") ? null : reader.GetString("status_notes")
                };
            }

            return null;
        }

        public Task<DD2760Form?> GetByIdAsync(DatabaseContext dbContext, int id)
        {
            return Task.FromResult(GetById(dbContext, id));
        }

        private DD2760Form? GetById(DatabaseContext dbContext, int id)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                       court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                       certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM dd2760_forms 
                WHERE id = @id";
            
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@id";
            idParameter.Value = id;
            command.Parameters.Add(idParameter);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new DD2760Form
                {
                    Id = reader.GetInt32("id"),
                    PersonnelId = reader.GetInt32("personnel_id"),
                    DateCompleted = DateTime.Parse(reader.GetString("date_completed")),
                    DateExpires = DateTime.Parse(reader.GetString("date_expires")),
                    DateCreated = DateTime.Parse(reader.GetString("date_created")),
                    DateModified = reader.IsDBNull("date_modified") ? null : DateTime.Parse(reader.GetString("date_modified")),
                    DomesticViolenceResponse = reader.IsDBNull("domestic_violence_response") ? null : reader.GetString("domestic_violence_response"),
                    DomesticViolenceInitials = reader.IsDBNull("domestic_violence_initials") ? null : reader.GetString("domestic_violence_initials"),
                    DomesticViolenceDate = reader.IsDBNull("domestic_violence_date") ? null : DateTime.Parse(reader.GetString("domestic_violence_date")),
                    CourtJurisdiction = reader.IsDBNull("court_jurisdiction") ? null : reader.GetString("court_jurisdiction"),
                    DocketCaseNumber = reader.IsDBNull("docket_case_number") ? null : reader.GetString("docket_case_number"),
                    StatuteCharge = reader.IsDBNull("statute_charge") ? null : reader.GetString("statute_charge"),
                    DateSentenced = reader.IsDBNull("date_sentenced") ? null : DateTime.Parse(reader.GetString("date_sentenced")),
                    CertifierName = reader.IsDBNull("certifier_name") ? null : reader.GetString("certifier_name"),
                    CertifierRank = reader.IsDBNull("certifier_rank") ? null : reader.GetString("certifier_rank"),
                    CertifierSSN = reader.IsDBNull("certifier_ssn") ? null : reader.GetString("certifier_ssn"),
                    CertifierOrganization = reader.IsDBNull("certifier_organization") ? null : reader.GetString("certifier_organization"),
                    IsCertified = !reader.IsDBNull("is_certified") && reader.GetInt32("is_certified") == 1,
                    CertifierSignatureDate = reader.IsDBNull("certifier_signature_date") ? null : DateTime.Parse(reader.GetString("certifier_signature_date")),
                    PdfFilePath = reader.IsDBNull("pdf_file_path") ? null : reader.GetString("pdf_file_path"),
                    PdfFileName = reader.IsDBNull("pdf_file_name") ? null : reader.GetString("pdf_file_name"),
                    IsValid = reader.GetInt32("is_valid") == 1,
                    StatusNotes = reader.IsDBNull("status_notes") ? null : reader.GetString("status_notes")
                };
            }

            return null;
        }

        public Task<List<DD2760Form>> GetAllAsync(DatabaseContext dbContext)
        {
            return Task.FromResult(GetAll(dbContext));
        }

        private List<DD2760Form> GetAll(DatabaseContext dbContext)
        {
            var forms = new List<DD2760Form>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                       court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                       certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM dd2760_forms 
                WHERE is_valid = 1
                ORDER BY date_completed DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forms.Add(new DD2760Form
                {
                    Id = reader.GetInt32("id"),
                    PersonnelId = reader.GetInt32("personnel_id"),
                    DateCompleted = DateTime.Parse(reader.GetString("date_completed")),
                    DateExpires = DateTime.Parse(reader.GetString("date_expires")),
                    DateCreated = DateTime.Parse(reader.GetString("date_created")),
                    DateModified = reader.IsDBNull("date_modified") ? null : DateTime.Parse(reader.GetString("date_modified")),
                    DomesticViolenceResponse = reader.IsDBNull("domestic_violence_response") ? null : reader.GetString("domestic_violence_response"),
                    DomesticViolenceInitials = reader.IsDBNull("domestic_violence_initials") ? null : reader.GetString("domestic_violence_initials"),
                    DomesticViolenceDate = reader.IsDBNull("domestic_violence_date") ? null : DateTime.Parse(reader.GetString("domestic_violence_date")),
                    CourtJurisdiction = reader.IsDBNull("court_jurisdiction") ? null : reader.GetString("court_jurisdiction"),
                    DocketCaseNumber = reader.IsDBNull("docket_case_number") ? null : reader.GetString("docket_case_number"),
                    StatuteCharge = reader.IsDBNull("statute_charge") ? null : reader.GetString("statute_charge"),
                    DateSentenced = reader.IsDBNull("date_sentenced") ? null : DateTime.Parse(reader.GetString("date_sentenced")),
                    CertifierName = reader.IsDBNull("certifier_name") ? null : reader.GetString("certifier_name"),
                    CertifierRank = reader.IsDBNull("certifier_rank") ? null : reader.GetString("certifier_rank"),
                    CertifierSSN = reader.IsDBNull("certifier_ssn") ? null : reader.GetString("certifier_ssn"),
                    CertifierOrganization = reader.IsDBNull("certifier_organization") ? null : reader.GetString("certifier_organization"),
                    IsCertified = !reader.IsDBNull("is_certified") && reader.GetInt32("is_certified") == 1,
                    CertifierSignatureDate = reader.IsDBNull("certifier_signature_date") ? null : DateTime.Parse(reader.GetString("certifier_signature_date")),
                    PdfFilePath = reader.IsDBNull("pdf_file_path") ? null : reader.GetString("pdf_file_path"),
                    PdfFileName = reader.IsDBNull("pdf_file_name") ? null : reader.GetString("pdf_file_name"),
                    IsValid = reader.GetInt32("is_valid") == 1,
                    StatusNotes = reader.IsDBNull("status_notes") ? null : reader.GetString("status_notes")
                });
            }

            return forms;
        }

        public Task<List<DD2760Form>> GetExpiringFormsAsync(DatabaseContext dbContext, int daysThreshold)
        {
            return Task.FromResult(GetExpiringForms(dbContext, daysThreshold));
        }

        private List<DD2760Form> GetExpiringForms(DatabaseContext dbContext, int daysThreshold)
        {
            var forms = new List<DD2760Form>();
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, date_completed, date_expires, date_created, date_modified,
                       domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                       court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                       certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                       pdf_file_path, pdf_file_name, is_valid, status_notes
                FROM dd2760_forms 
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
                forms.Add(new DD2760Form
                {
                    Id = reader.GetInt32("id"),
                    PersonnelId = reader.GetInt32("personnel_id"),
                    DateCompleted = DateTime.Parse(reader.GetString("date_completed")),
                    DateExpires = DateTime.Parse(reader.GetString("date_expires")),
                    DateCreated = DateTime.Parse(reader.GetString("date_created")),
                    DateModified = reader.IsDBNull("date_modified") ? null : DateTime.Parse(reader.GetString("date_modified")),
                    DomesticViolenceResponse = reader.IsDBNull("domestic_violence_response") ? null : reader.GetString("domestic_violence_response"),
                    DomesticViolenceInitials = reader.IsDBNull("domestic_violence_initials") ? null : reader.GetString("domestic_violence_initials"),
                    DomesticViolenceDate = reader.IsDBNull("domestic_violence_date") ? null : DateTime.Parse(reader.GetString("domestic_violence_date")),
                    CourtJurisdiction = reader.IsDBNull("court_jurisdiction") ? null : reader.GetString("court_jurisdiction"),
                    DocketCaseNumber = reader.IsDBNull("docket_case_number") ? null : reader.GetString("docket_case_number"),
                    StatuteCharge = reader.IsDBNull("statute_charge") ? null : reader.GetString("statute_charge"),
                    DateSentenced = reader.IsDBNull("date_sentenced") ? null : DateTime.Parse(reader.GetString("date_sentenced")),
                    CertifierName = reader.IsDBNull("certifier_name") ? null : reader.GetString("certifier_name"),
                    CertifierRank = reader.IsDBNull("certifier_rank") ? null : reader.GetString("certifier_rank"),
                    CertifierSSN = reader.IsDBNull("certifier_ssn") ? null : reader.GetString("certifier_ssn"),
                    CertifierOrganization = reader.IsDBNull("certifier_organization") ? null : reader.GetString("certifier_organization"),
                    IsCertified = !reader.IsDBNull("is_certified") && reader.GetInt32("is_certified") == 1,
                    CertifierSignatureDate = reader.IsDBNull("certifier_signature_date") ? null : DateTime.Parse(reader.GetString("certifier_signature_date")),
                    PdfFilePath = reader.IsDBNull("pdf_file_path") ? null : reader.GetString("pdf_file_path"),
                    PdfFileName = reader.IsDBNull("pdf_file_name") ? null : reader.GetString("pdf_file_name"),
                    IsValid = reader.GetInt32("is_valid") == 1,
                    StatusNotes = reader.IsDBNull("status_notes") ? null : reader.GetString("status_notes")
                });
            }

            return forms;
        }

        public Task<int> AddAsync(DatabaseContext dbContext, DD2760Form form)
        {
            return Task.FromResult(Add(dbContext, form));
        }

        private int Add(DatabaseContext dbContext, DD2760Form form)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO dd2760_forms (
                    personnel_id, date_completed, date_expires, date_created, date_modified,
                    domestic_violence_response, domestic_violence_initials, domestic_violence_date,
                    court_jurisdiction, docket_case_number, statute_charge, date_sentenced,
                    certifier_name, certifier_rank, certifier_ssn, certifier_organization, is_certified, certifier_signature_date,
                    pdf_file_path, pdf_file_name, is_valid, status_notes
                ) VALUES (
                    @personnelId, @dateCompleted, @dateExpires, @dateCreated, @dateModified,
                    @domesticViolenceResponse, @domesticViolenceInitials, @domesticViolenceDate,
                    @courtJurisdiction, @docketCaseNumber, @statuteCharge, @dateSentenced,
                    @certifierName, @certifierRank, @certifierSSN, @certifierOrganization, @isCertified, @certifierSignatureDate,
                    @pdfFilePath, @pdfFileName, @isValid, @statusNotes
                );
                SELECT last_insert_rowid();";

            // Add parameters
            AddParameter(command, "@personnelId", form.PersonnelId);
            AddParameter(command, "@dateCompleted", form.DateCompleted.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateExpires", form.DateExpires.ToString("yyyy-MM-dd"));
            AddParameter(command, "@dateCreated", form.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
            AddParameter(command, "@dateModified", form.DateModified?.ToString("yyyy-MM-dd HH:mm:ss"));
            AddParameter(command, "@domesticViolenceResponse", form.DomesticViolenceResponse);
            AddParameter(command, "@domesticViolenceInitials", form.DomesticViolenceInitials);
            AddParameter(command, "@domesticViolenceDate", form.DomesticViolenceDate?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@courtJurisdiction", form.CourtJurisdiction);
            AddParameter(command, "@docketCaseNumber", form.DocketCaseNumber);
            AddParameter(command, "@statuteCharge", form.StatuteCharge);
            AddParameter(command, "@dateSentenced", form.DateSentenced?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@certifierName", form.CertifierName);
            AddParameter(command, "@certifierRank", form.CertifierRank);
            AddParameter(command, "@certifierSSN", form.CertifierSSN);
            AddParameter(command, "@certifierOrganization", form.CertifierOrganization);
            AddParameter(command, "@isCertified", form.IsCertified ? 1 : 0);
            AddParameter(command, "@certifierSignatureDate", form.CertifierSignatureDate?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@pdfFilePath", form.PdfFilePath);
            AddParameter(command, "@pdfFileName", form.PdfFileName);
            AddParameter(command, "@isValid", form.IsValid ? 1 : 0);
            AddParameter(command, "@statusNotes", form.StatusNotes);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public Task<bool> UpdateAsync(DatabaseContext dbContext, DD2760Form form)
        {
            return Task.FromResult(Update(dbContext, form));
        }

        private bool Update(DatabaseContext dbContext, DD2760Form form)
        {
            using var connection = dbContext.GetConnection();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE dd2760_forms SET
                    date_completed = @dateCompleted,
                    date_expires = @dateExpires,
                    date_modified = @dateModified,
                    domestic_violence_response = @domesticViolenceResponse,
                    domestic_violence_initials = @domesticViolenceInitials,
                    domestic_violence_date = @domesticViolenceDate,
                    court_jurisdiction = @courtJurisdiction,
                    docket_case_number = @docketCaseNumber,
                    statute_charge = @statuteCharge,
                    date_sentenced = @dateSentenced,
                    certifier_name = @certifierName,
                    certifier_rank = @certifierRank,
                    certifier_ssn = @certifierSSN,
                    certifier_organization = @certifierOrganization,
                    is_certified = @isCertified,
                    certifier_signature_date = @certifierSignatureDate,
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
            AddParameter(command, "@domesticViolenceResponse", form.DomesticViolenceResponse);
            AddParameter(command, "@domesticViolenceInitials", form.DomesticViolenceInitials);
            AddParameter(command, "@domesticViolenceDate", form.DomesticViolenceDate?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@courtJurisdiction", form.CourtJurisdiction);
            AddParameter(command, "@docketCaseNumber", form.DocketCaseNumber);
            AddParameter(command, "@statuteCharge", form.StatuteCharge);
            AddParameter(command, "@dateSentenced", form.DateSentenced?.ToString("yyyy-MM-dd"));
            AddParameter(command, "@certifierName", form.CertifierName);
            AddParameter(command, "@certifierRank", form.CertifierRank);
            AddParameter(command, "@certifierSSN", form.CertifierSSN);
            AddParameter(command, "@certifierOrganization", form.CertifierOrganization);
            AddParameter(command, "@isCertified", form.IsCertified ? 1 : 0);
            AddParameter(command, "@certifierSignatureDate", form.CertifierSignatureDate?.ToString("yyyy-MM-dd"));
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
            command.CommandText = "UPDATE dd2760_forms SET is_valid = 0 WHERE id = @id";

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
            command.CommandText = "UPDATE dd2760_forms SET is_valid = 0 WHERE personnel_id = @personnelId";

            var personnelIdParameter = command.CreateParameter();
            personnelIdParameter.ParameterName = "@personnelId";
            personnelIdParameter.Value = personnelId;
            command.Parameters.Add(personnelIdParameter);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
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
