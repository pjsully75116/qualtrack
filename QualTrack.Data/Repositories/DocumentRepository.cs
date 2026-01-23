using System.Data.SQLite;
using System.IO;
using QualTrack.Core.Models;
using QualTrack.Data.Database;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Repository implementation for document operations
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DatabaseContext _context;

        public DocumentRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Document> AddDocumentAsync(Document document)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO documents (personnel_id, document_type, original_filename, file_path, file_size, upload_date, ocr_processed, ocr_confidence, date_created, date_modified)
                VALUES (@personnelId, @documentType, @originalFilename, @filePath, @fileSize, @uploadDate, @ocrProcessed, @ocrConfidence, @dateCreated, @dateModified);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@personnelId", document.PersonnelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@documentType", document.DocumentType);
            command.Parameters.AddWithValue("@originalFilename", document.OriginalFilename);
            command.Parameters.AddWithValue("@filePath", document.FilePath);
            command.Parameters.AddWithValue("@fileSize", document.FileSize);
            command.Parameters.AddWithValue("@uploadDate", document.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ocrProcessed", document.OcrProcessed);
            command.Parameters.AddWithValue("@ocrConfidence", document.OcrConfidence ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateCreated", document.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@dateModified", document.DateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            document.Id = id;
            return document;
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, document_type, original_filename, file_path, file_size, upload_date, ocr_processed, ocr_confidence, date_created, date_modified
                FROM documents WHERE id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapDocumentFromReader((SQLiteDataReader)reader);
            }

            return null;
        }

        public async Task<List<Document>> GetDocumentsByPersonnelIdAsync(int personnelId)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, document_type, original_filename, file_path, file_size, upload_date, ocr_processed, ocr_confidence, date_created, date_modified
                FROM documents WHERE personnel_id = @personnelId
                ORDER BY upload_date DESC";

            command.Parameters.AddWithValue("@personnelId", personnelId);

            var documents = new List<Document>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapDocumentFromReader((SQLiteDataReader)reader));
            }

            return documents;
        }

        public async Task<List<Document>> GetDocumentsByTypeAsync(string documentType)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, document_type, original_filename, file_path, file_size, upload_date, ocr_processed, ocr_confidence, date_created, date_modified
                FROM documents WHERE document_type = @documentType
                ORDER BY upload_date DESC";

            command.Parameters.AddWithValue("@documentType", documentType);

            var documents = new List<Document>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapDocumentFromReader((SQLiteDataReader)reader));
            }

            return documents;
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, personnel_id, document_type, original_filename, file_path, file_size, upload_date, ocr_processed, ocr_confidence, date_created, date_modified
                FROM documents
                ORDER BY upload_date DESC";

            var documents = new List<Document>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapDocumentFromReader((SQLiteDataReader)reader));
            }

            return documents;
        }

        public async Task<Document> UpdateDocumentAsync(Document document)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE documents 
                SET personnel_id = @personnelId, document_type = @documentType, original_filename = @originalFilename, 
                    file_path = @filePath, file_size = @fileSize, upload_date = @uploadDate, ocr_processed = @ocrProcessed, 
                    ocr_confidence = @ocrConfidence, date_modified = @dateModified
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", document.Id);
            command.Parameters.AddWithValue("@personnelId", document.PersonnelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@documentType", document.DocumentType);
            command.Parameters.AddWithValue("@originalFilename", document.OriginalFilename);
            command.Parameters.AddWithValue("@filePath", document.FilePath);
            command.Parameters.AddWithValue("@fileSize", document.FileSize);
            command.Parameters.AddWithValue("@uploadDate", document.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ocrProcessed", document.OcrProcessed);
            command.Parameters.AddWithValue("@ocrConfidence", document.OcrConfidence ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateModified", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
            document.DateModified = DateTime.Now;
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM documents WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<OcrExtraction> AddOcrExtractionAsync(OcrExtraction extraction)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ocr_extractions (document_id, field_name, extracted_value, confidence, bounding_box, reviewed, approved, corrected_value, date_created, date_modified)
                VALUES (@documentId, @fieldName, @extractedValue, @confidence, @boundingBox, @reviewed, @approved, @correctedValue, @dateCreated, @dateModified);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@documentId", extraction.DocumentId);
            command.Parameters.AddWithValue("@fieldName", extraction.FieldName);
            command.Parameters.AddWithValue("@extractedValue", extraction.ExtractedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@confidence", extraction.Confidence ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@boundingBox", extraction.BoundingBox ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@reviewed", extraction.Reviewed);
            command.Parameters.AddWithValue("@approved", extraction.Approved);
            command.Parameters.AddWithValue("@correctedValue", extraction.CorrectedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateCreated", extraction.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@dateModified", extraction.DateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            extraction.Id = id;
            return extraction;
        }

        public async Task<List<OcrExtraction>> GetOcrExtractionsByDocumentIdAsync(int documentId)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, document_id, field_name, extracted_value, confidence, bounding_box, reviewed, approved, corrected_value, date_created, date_modified
                FROM ocr_extractions WHERE document_id = @documentId
                ORDER BY field_name";

            command.Parameters.AddWithValue("@documentId", documentId);

            var extractions = new List<OcrExtraction>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                extractions.Add(MapOcrExtractionFromReader((SQLiteDataReader)reader));
            }

            return extractions;
        }

        public async Task<OcrExtraction> UpdateOcrExtractionAsync(OcrExtraction extraction)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ocr_extractions 
                SET field_name = @fieldName, extracted_value = @extractedValue, confidence = @confidence, 
                    bounding_box = @boundingBox, reviewed = @reviewed, approved = @approved, 
                    corrected_value = @correctedValue, date_modified = @dateModified
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", extraction.Id);
            command.Parameters.AddWithValue("@fieldName", extraction.FieldName);
            command.Parameters.AddWithValue("@extractedValue", extraction.ExtractedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@confidence", extraction.Confidence ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@boundingBox", extraction.BoundingBox ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@reviewed", extraction.Reviewed);
            command.Parameters.AddWithValue("@approved", extraction.Approved);
            command.Parameters.AddWithValue("@correctedValue", extraction.CorrectedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateModified", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
            extraction.DateModified = DateTime.Now;
            return extraction;
        }

        public async Task<int> DeleteOcrExtractionsByDocumentIdAsync(int documentId)
        {
            using var connection = _context.GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ocr_extractions WHERE document_id = @documentId";
            command.Parameters.AddWithValue("@documentId", documentId);

            return await command.ExecuteNonQueryAsync();
        }

        private static Document MapDocumentFromReader(SQLiteDataReader reader)
        {
            return new Document
            {
                Id = reader.GetInt32(0),
                PersonnelId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                DocumentType = reader.GetString(2),
                OriginalFilename = reader.GetString(3),
                FilePath = reader.GetString(4),
                FileSize = reader.GetInt64(5),
                UploadDate = DateTime.Parse(reader.GetString(6)),
                OcrProcessed = reader.GetBoolean(7),
                OcrConfidence = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                DateCreated = DateTime.Parse(reader.GetString(9)),
                DateModified = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
            };
        }

        private static OcrExtraction MapOcrExtractionFromReader(SQLiteDataReader reader)
        {
            return new OcrExtraction
            {
                Id = reader.GetInt32(0),
                DocumentId = reader.GetInt32(1),
                FieldName = reader.GetString(2),
                ExtractedValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                Confidence = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                BoundingBox = reader.IsDBNull(5) ? null : reader.GetString(5),
                Reviewed = reader.GetBoolean(6),
                Approved = reader.GetBoolean(7),
                CorrectedValue = reader.IsDBNull(8) ? null : reader.GetString(8),
                DateCreated = DateTime.Parse(reader.GetString(9)),
                DateModified = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
            };
        }
    }
} 