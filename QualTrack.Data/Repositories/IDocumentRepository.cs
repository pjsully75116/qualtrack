using QualTrack.Core.Models;

namespace QualTrack.Data.Repositories
{
    /// <summary>
    /// Repository interface for document operations
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// Adds a new document to the database
        /// </summary>
        /// <param name="document">Document to add</param>
        /// <returns>Added document with ID</returns>
        Task<Document> AddDocumentAsync(Document document);

        /// <summary>
        /// Gets a document by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document or null if not found</returns>
        Task<Document?> GetDocumentByIdAsync(int id);

        /// <summary>
        /// Gets all documents for a specific personnel
        /// </summary>
        /// <param name="personnelId">Personnel ID</param>
        /// <returns>List of documents</returns>
        Task<List<Document>> GetDocumentsByPersonnelIdAsync(int personnelId);

        /// <summary>
        /// Gets documents by type
        /// </summary>
        /// <param name="documentType">Document type</param>
        /// <returns>List of documents</returns>
        Task<List<Document>> GetDocumentsByTypeAsync(string documentType);

        /// <summary>
        /// Gets all documents
        /// </summary>
        /// <returns>List of all documents</returns>
        Task<List<Document>> GetAllDocumentsAsync();

        /// <summary>
        /// Updates a document
        /// </summary>
        /// <param name="document">Document to update</param>
        /// <returns>Updated document</returns>
        Task<Document> UpdateDocumentAsync(Document document);

        /// <summary>
        /// Deletes a document
        /// </summary>
        /// <param name="id">Document ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteDocumentAsync(int id);

        /// <summary>
        /// Adds OCR extraction results
        /// </summary>
        /// <param name="extraction">OCR extraction to add</param>
        /// <returns>Added extraction with ID</returns>
        Task<OcrExtraction> AddOcrExtractionAsync(OcrExtraction extraction);

        /// <summary>
        /// Gets OCR extractions for a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>List of OCR extractions</returns>
        Task<List<OcrExtraction>> GetOcrExtractionsByDocumentIdAsync(int documentId);

        /// <summary>
        /// Updates an OCR extraction
        /// </summary>
        /// <param name="extraction">OCR extraction to update</param>
        /// <returns>Updated extraction</returns>
        Task<OcrExtraction> UpdateOcrExtractionAsync(OcrExtraction extraction);

        /// <summary>
        /// Deletes OCR extractions for a document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>Number of extractions deleted</returns>
        Task<int> DeleteOcrExtractionsByDocumentIdAsync(int documentId);
    }
} 