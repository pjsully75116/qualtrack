using System.Data.SQLite;
using QualTrack.Core.Models;
using QualTrack.Data.Database;
using System;
using System.Threading.Tasks;

namespace QualTrack.Data.Repositories
{
    public class AdditionalRequirementsRepository : IAdditionalRequirementsRepository
    {
        private readonly DatabaseContext _context;

        public AdditionalRequirementsRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<AdditionalRequirements?> GetByPersonnelIdAsync(int personnelId)
        {
            using var connection = _context.GetConnection();
            using var command = new SQLiteCommand(
                $@"SELECT personnel_id, form2760_date, form2760_number, form2760_signed_date, form2760_witness,
                         aae_screening_date, aae_screening_level, aae_investigation_type, aae_investigation_date, aae_investigation_agency,
                         deadly_force_training_date, deadly_force_instructor, deadly_force_remarks
                  FROM additional_requirements 
                  WHERE personnel_id = {personnelId}", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AdditionalRequirements
                {
                    PersonnelId = personnelId,
                    Form2760Date = reader.IsDBNull(reader.GetOrdinal("form2760_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("form2760_date"))),
                    Form2760Number = reader.IsDBNull(reader.GetOrdinal("form2760_number")) ? null : reader.GetString(reader.GetOrdinal("form2760_number")),
                    Form2760SignedDate = reader.IsDBNull(reader.GetOrdinal("form2760_signed_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("form2760_signed_date"))),
                    Form2760Witness = reader.IsDBNull(reader.GetOrdinal("form2760_witness")) ? null : reader.GetString(reader.GetOrdinal("form2760_witness")),
                    AAEScreeningDate = reader.IsDBNull(reader.GetOrdinal("aae_screening_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("aae_screening_date"))),
                    AAEScreeningLevel = reader.IsDBNull(reader.GetOrdinal("aae_screening_level")) ? null : reader.GetString(reader.GetOrdinal("aae_screening_level")),
                    AAEInvestigationType = reader.IsDBNull(reader.GetOrdinal("aae_investigation_type")) ? null : reader.GetString(reader.GetOrdinal("aae_investigation_type")),
                    AAEInvestigationDate = reader.IsDBNull(reader.GetOrdinal("aae_investigation_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("aae_investigation_date"))),
                    AAEInvestigationAgency = reader.IsDBNull(reader.GetOrdinal("aae_investigation_agency")) ? null : reader.GetString(reader.GetOrdinal("aae_investigation_agency")),
                    DeadlyForceTrainingDate = reader.IsDBNull(reader.GetOrdinal("deadly_force_training_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("deadly_force_training_date"))),
                    DeadlyForceInstructor = reader.IsDBNull(reader.GetOrdinal("deadly_force_instructor")) ? null : reader.GetString(reader.GetOrdinal("deadly_force_instructor")),
                    DeadlyForceRemarks = reader.IsDBNull(reader.GetOrdinal("deadly_force_remarks")) ? null : reader.GetString(reader.GetOrdinal("deadly_force_remarks"))
                };
            }
            
            return null;
        }

        public async Task<bool> SaveAsync(int personnelId, AdditionalRequirements requirements)
        {
            using var connection = _context.GetConnection();
            
            // Delete existing requirements for this personnel
            using var deleteCommand = new SQLiteCommand(
                $"DELETE FROM additional_requirements WHERE personnel_id = {personnelId}", connection);
            await deleteCommand.ExecuteNonQueryAsync();
            
            // Insert new requirements
            using var insertCommand = new SQLiteCommand(
                $@"INSERT INTO additional_requirements 
                  (personnel_id, form2760_date, form2760_number, form2760_signed_date, form2760_witness,
                   aae_screening_date, aae_screening_level, aae_investigation_type, aae_investigation_date, aae_investigation_agency,
                   deadly_force_training_date, deadly_force_instructor, deadly_force_remarks, date_created)
                  VALUES ({personnelId}, 
                          {(requirements.Form2760Date.HasValue ? $"'{requirements.Form2760Date.Value:yyyy-MM-dd}'" : "NULL")},
                          {(requirements.Form2760Number != null ? $"'{requirements.Form2760Number.Replace("'", "''")}'" : "NULL")},
                          {(requirements.Form2760SignedDate.HasValue ? $"'{requirements.Form2760SignedDate.Value:yyyy-MM-dd}'" : "NULL")},
                          {(requirements.Form2760Witness != null ? $"'{requirements.Form2760Witness.Replace("'", "''")}'" : "NULL")},
                          {(requirements.AAEScreeningDate.HasValue ? $"'{requirements.AAEScreeningDate.Value:yyyy-MM-dd}'" : "NULL")},
                          {(requirements.AAEScreeningLevel != null ? $"'{requirements.AAEScreeningLevel.Replace("'", "''")}'" : "NULL")},
                          {(requirements.AAEInvestigationType != null ? $"'{requirements.AAEInvestigationType.Replace("'", "''")}'" : "NULL")},
                          {(requirements.AAEInvestigationDate.HasValue ? $"'{requirements.AAEInvestigationDate.Value:yyyy-MM-dd}'" : "NULL")},
                          {(requirements.AAEInvestigationAgency != null ? $"'{requirements.AAEInvestigationAgency.Replace("'", "''")}'" : "NULL")},
                          {(requirements.DeadlyForceTrainingDate.HasValue ? $"'{requirements.DeadlyForceTrainingDate.Value:yyyy-MM-dd}'" : "NULL")},
                          {(requirements.DeadlyForceInstructor != null ? $"'{requirements.DeadlyForceInstructor.Replace("'", "''")}'" : "NULL")},
                          {(requirements.DeadlyForceRemarks != null ? $"'{requirements.DeadlyForceRemarks.Replace("'", "''")}'" : "NULL")},
                          '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')", connection);
            
            await insertCommand.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> DeleteByPersonnelIdAsync(int personnelId)
        {
            using var connection = _context.GetConnection();
            using var command = new SQLiteCommand(
                $"DELETE FROM additional_requirements WHERE personnel_id = {personnelId}", connection);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
} 