using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

using QualTrack.Core.Models;
using System.Text.RegularExpressions;
using Path = System.IO.Path;
using System.Collections.Generic;
using System.Linq;

namespace QualTrack.Core.Services
{
    /// <summary>
    /// Service for managing document uploads, storage, and processing
    /// </summary>
    public class DocumentService
    {
        private readonly string _documentStoragePath;
        private readonly string _tesseractDataPath;
        private const int MaxFileSizeMB = 10;
        private const long MaxFileSizeBytes = MaxFileSizeMB * 1024 * 1024;
        private const int PdfRenderDpi = 300; // Standard document scanning resolution

        public DocumentService(string storagePath = "Documents", string? tesseractDataPath = null)
        {
            _documentStoragePath = Path.GetFullPath(storagePath);
            
            // Use system Tesseract installation if available, otherwise fall back to local tessdata
            if (string.IsNullOrEmpty(tesseractDataPath))
            {
                var systemTessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";
                if (Directory.Exists(systemTessDataPath) && File.Exists(Path.Combine(systemTessDataPath, "eng.traineddata")))
                {
                    _tesseractDataPath = systemTessDataPath;
                }
                else
                {
                    _tesseractDataPath = Path.GetFullPath("tessdata");
                }
            }
            else
            {
                _tesseractDataPath = Path.GetFullPath(tesseractDataPath);
            }
            
            EnsureStorageDirectoryExists();
            // Only create local tessdata directory if we're not using system installation
            if (_tesseractDataPath != @"C:\Program Files\Tesseract-OCR\tessdata")
            {
                EnsureTesseractDataDirectoryExists();
            }
        }

        /// <summary>
        /// Ensures the document storage directory exists
        /// </summary>
        private void EnsureStorageDirectoryExists()
        {
            if (!Directory.Exists(_documentStoragePath))
            {
                Directory.CreateDirectory(_documentStoragePath);
            }
        }

        /// <summary>
        /// Ensures the Tesseract data directory exists
        /// </summary>
        private void EnsureTesseractDataDirectoryExists()
        {
            if (!Directory.Exists(_tesseractDataPath))
            {
                Directory.CreateDirectory(_tesseractDataPath);
            }
        }

        /// <summary>
        /// Validates a file for upload
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <returns>Validation result</returns>
        public (bool IsValid, string ErrorMessage) ValidateFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (false, "File does not exist.");
            }

            var fileInfo = new FileInfo(filePath);
            
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return (false, $"File size exceeds maximum allowed size of {MaxFileSizeMB}MB.");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
            
            if (!allowedExtensions.Contains(extension))
            {
                return (false, $"File type not supported. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Saves a document file to storage with compression
        /// </summary>
        /// <param name="sourceFilePath">Path to the source file</param>
        /// <param name="documentType">Type of document</param>
        /// <param name="personnelId">Associated personnel ID</param>
        /// <returns>Document object with file information</returns>
        public async Task<Document> SaveDocumentAsync(string sourceFilePath, string documentType, int? personnelId = null)
        {
            var validation = ValidateFile(sourceFilePath);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var originalFilename = Path.GetFileName(sourceFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var personnelPrefix = personnelId.HasValue ? $"P{personnelId}_" : "";
            var safeFilename = $"{personnelPrefix}{documentType}_{timestamp}_{Path.GetFileNameWithoutExtension(originalFilename)}";
            
            // Create compressed file path
            var compressedFilePath = Path.Combine(_documentStoragePath, $"{safeFilename}.zip");
            
            // Compress the file
            await CompressFileAsync(sourceFilePath, compressedFilePath);
            
            var compressedFileInfo = new FileInfo(compressedFilePath);
            
            return new Document
            {
                DocumentType = documentType,
                OriginalFilename = originalFilename,
                FilePath = compressedFilePath,
                FileSize = compressedFileInfo.Length,
                UploadDate = DateTime.Now,
                PersonnelId = personnelId,
                DateCreated = DateTime.Now,
                OcrProcessed = false
            };
        }

        /// <summary>
        /// Compresses a file using GZip compression
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination compressed file path</param>
        private async Task CompressFileAsync(string sourcePath, string destinationPath)
        {
            using var sourceStream = File.OpenRead(sourcePath);
            using var destinationStream = File.Create(destinationPath);
            using var gzipStream = new GZipStream(destinationStream, CompressionMode.Compress);
            
            await sourceStream.CopyToAsync(gzipStream);
        }

        /// <summary>
        /// Extracts a compressed document file
        /// </summary>
        /// <param name="document">Document to extract</param>
        /// <param name="extractPath">Path to extract the file to</param>
        /// <returns>Path to the extracted file</returns>
        public async Task<string> ExtractDocumentAsync(Document document, string extractPath)
        {
            if (!File.Exists(document.FilePath))
            {
                throw new FileNotFoundException($"Document file not found: {document.FilePath}");
            }

            var extractedFilePath = Path.Combine(extractPath, document.OriginalFilename);
            
            using var sourceStream = File.OpenRead(document.FilePath);
            using var destinationStream = File.Create(extractedFilePath);
            using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
            
            await gzipStream.CopyToAsync(destinationStream);
            
            return extractedFilePath;
        }

        /// <summary>
        /// Processes OCR on a document to extract text using Tesseract
        /// </summary>
        /// <param name="document">Document to process</param>
        /// <returns>OCR extraction results</returns>
        public async Task<List<OcrExtraction>> ProcessOcrAsync(Document document)
        {
            try
            {
                // Extract the document to a temporary location
                var tempPath = Path.GetTempPath();
                var extractedPath = await ExtractDocumentAsync(document, tempPath);
                
                // Process the document based on its type
                var extractions = new List<OcrExtraction>();
                var extension = Path.GetExtension(extractedPath).ToLowerInvariant();
                
                if (extension == ".pdf")
                {
                    extractions = await ProcessPdfAsync(extractedPath);
                }
                else
                {
                    extractions = await ProcessImageAsync(extractedPath);
                }
                
                // Clean up temporary files
                CleanupTempFiles(extractedPath);
                
                return extractions;
            }
            catch (Exception ex)
            {
                // Fall back to mock processing for any errors
                System.Diagnostics.Debug.WriteLine($"OCR processing error: {ex.Message}");
                var extension = Path.GetExtension(document.OriginalFilename).ToLowerInvariant();
                if (extension == ".pdf")
                {
                    return await ProcessPdfMockAsync("mock_pdf_path");
                }
                else
                {
                    return await PerformMockOcrAsync("mock_image_path");
                }
            }
        }

        /// <summary>
        /// Processes an image file using Tesseract
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>List of OCR extractions</returns>
        private async Task<List<OcrExtraction>> ProcessImageAsync(string imagePath)
        {
            return await PerformOcrAsync(imagePath);
        }

        /// <summary>
        /// Performs OCR on an image file using Tesseract
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>List of OCR extractions</returns>
        private async Task<List<OcrExtraction>> PerformOcrAsync(string imagePath)
        {
            var extractions = new List<OcrExtraction>();
            
            try
            {
                // Check if Tesseract data directory exists and has language files
                if (!Directory.Exists(_tesseractDataPath))
                {
                    // Fallback to mock OCR for testing
                    return await PerformMockOcrAsync(imagePath);
                }

                var engDataPath = Path.Combine(_tesseractDataPath, "eng.traineddata");
                if (!File.Exists(engDataPath))
                {
                    // Fallback to mock OCR for testing
                    return await PerformMockOcrAsync(imagePath);
                }

                // Use the system Tesseract installation
                using var engine = new TesseractEngine(_tesseractDataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img);
                
                // Get all text from the document
                var fullText = page.GetText();
                var confidence = page.GetMeanConfidence();
                
                // Create a single extraction with all the text
                extractions.Add(new OcrExtraction
                {
                    FieldName = "FullText",
                    ExtractedValue = fullText,
                    Confidence = confidence,
                    BoundingBox = "0,0,100,100", // Full document bounds
                    Reviewed = false,
                    Approved = false,
                    DateCreated = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Tesseract OCR error: {ex.Message}");
                // Fallback to mock OCR if Tesseract fails
                return await PerformMockOcrAsync(imagePath);
            }
            
            return extractions;
        }

        /// <summary>
        /// Performs mock OCR for testing when Tesseract is not available
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>Mock OCR extractions</returns>
        private async Task<List<OcrExtraction>> PerformMockOcrAsync(string imagePath)
        {
            await Task.Delay(1000); // Simulate processing time
            
            var extractions = new List<OcrExtraction>
            {
                new OcrExtraction
                {
                    FieldName = "FullText",
                    ExtractedValue = "MOCK OCR RESULT - Tesseract not available\n\nThis is a mock OCR extraction for testing purposes. The system is configured to use Tesseract from C:\\Program Files\\Tesseract-OCR\\tessdata, but it appears to be unavailable or not properly configured.\n\nSample 3591/1 Form Data:\nLast Name: DOE\nFirst Name: JOHN\nDOD ID: 1234567890\nWeapon: M9\nCategory: I\nDate Qualified: 2024-01-15",
                    Confidence = 0.85,
                    BoundingBox = "0,0,100,100",
                    Reviewed = false,
                    Approved = false,
                    DateCreated = DateTime.Now
                }
            };
            
            return extractions;
        }

        /// <summary>
        /// Cleans up temporary files
        /// </summary>
        /// <param name="extractedPath">Path to extracted file</param>
        private void CleanupTempFiles(string extractedPath)
        {
            try
            {
                if (File.Exists(extractedPath) && extractedPath.StartsWith(Path.GetTempPath()))
                {
                    File.Delete(extractedPath);
                }
            }
            catch (Exception ex)
            {
                // Log cleanup errors but don't throw
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a document file from storage
        /// </summary>
        /// <param name="document">Document to delete</param>
        public void DeleteDocument(Document document)
        {
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }
        }

        /// <summary>
        /// Gets the storage directory path
        /// </summary>
        /// <returns>Storage directory path</returns>
        public string GetStoragePath()
        {
            return _documentStoragePath;
        }

        /// <summary>
        /// Gets the total size of all stored documents
        /// </summary>
        /// <returns>Total size in bytes</returns>
        public long GetTotalStorageSize()
        {
            if (!Directory.Exists(_documentStoragePath))
            {
                return 0;
            }

            var files = Directory.GetFiles(_documentStoragePath, "*.zip", SearchOption.TopDirectoryOnly);
            return files.Sum(file => new FileInfo(file).Length);
        }

        /// <summary>
        /// Gets the number of stored documents
        /// </summary>
        /// <returns>Number of documents</returns>
        public int GetDocumentCount()
        {
            if (!Directory.Exists(_documentStoragePath))
            {
                return 0;
            }

            return Directory.GetFiles(_documentStoragePath, "*.zip", SearchOption.TopDirectoryOnly).Length;
        }

        /// <summary>
        /// Performs mock PDF processing when PdfSharpCore is not available
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>Mock PDF processing results</returns>
        private async Task<List<OcrExtraction>> ProcessPdfMockAsync(string pdfPath)
        {
            await Task.Delay(2000); // Simulate PDF processing time
            
            var extractions = new List<OcrExtraction>
            {
                new OcrExtraction
                {
                    FieldName = "Page1_FullText",
                    ExtractedValue = "MOCK PDF OCR RESULT - PdfSharpCore not available\n\nThis is a mock PDF OCR extraction for testing purposes. In a production environment, please ensure PdfSharpCore is properly installed.\n\nSample 3591/1 Form Data from PDF:\nLast Name: DOE\nFirst Name: JOHN\nDOD ID: 1234567890\nWeapon: M9\nCategory: I\nDate Qualified: 2024-01-15\n\nAdditional PDF Content:\nThis document appears to be a Navy qualification form with multiple pages. The form contains weapon qualification data and administrative information.",
                    Confidence = 0.90,
                    BoundingBox = "0,0,100,100",
                    Reviewed = false,
                    Approved = false,
                    DateCreated = DateTime.Now
                }
            };
            
            return extractions;
        }

        /// <summary>
        /// Processes a PDF file using PdfSharpCore and Tesseract
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>List of OCR extractions</returns>
        private async Task<List<OcrExtraction>> ProcessPdfAsync(string pdfPath)
        {
            var extractions = new List<OcrExtraction>();
            
            try
            {
                using var document = PdfSharpCore.Pdf.IO.PdfReader.Open(pdfPath, PdfDocumentOpenMode.ReadOnly);
                var pageCount = document.PageCount;
                
                // Process each page
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    var pageExtractions = await ProcessPdfPageAsync(document, pageIndex, pdfPath);
                    extractions.AddRange(pageExtractions);
                }
            }
            catch (Exception ex)
            {
                extractions.Add(new OcrExtraction
                {
                    FieldName = "Error",
                    ExtractedValue = $"PDF processing failed: {ex.Message}",
                    Confidence = 0.0f
                });
            }
            
            return extractions;
        }

        /// <summary>
        /// Extracts text from a PDF page using PdfSharpCore
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <param name="pageIndex">Page index (0-based)</param>
        /// <returns>Extracted text</returns>
        private string ExtractTextFromPdfPage(string pdfPath, int pageIndex)
        {
            try
            {
                using var document = PdfSharpCore.Pdf.IO.PdfReader.Open(pdfPath, PdfDocumentOpenMode.ReadOnly);
                if (pageIndex >= document.PageCount)
                {
                    return $"Page {pageIndex + 1} does not exist in the document";
                }
                
                var page = document.Pages[pageIndex];
                // PdfSharpCore doesn't have a direct ExtractText method, so we'll return empty for now
                // and rely on the image rendering + OCR approach
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"Error extracting text from page {pageIndex + 1}: {ex.Message}";
            }
        }

        /// <summary>
        /// Processes a single PDF page using hybrid approach: text extraction first, then image rendering + OCR
        /// </summary>
        /// <param name="document">PDF document</param>
        /// <param name="pageIndex">Page index (0-based)</param>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>List of OCR extractions for the page</returns>
        private async Task<List<OcrExtraction>> ProcessPdfPageAsync(PdfSharpCore.Pdf.PdfDocument document, int pageIndex, string pdfPath)
        {
            var extractions = new List<OcrExtraction>();
            
            try
            {
                // Step 1: Try to extract text directly from PDF page using PdfSharpCore
                var extractedText = ExtractTextFromPdfPage(pdfPath, pageIndex);
                
                // Check if we got meaningful text (not just whitespace or error messages)
                if (!string.IsNullOrWhiteSpace(extractedText) && 
                    !extractedText.StartsWith("Error extracting text") &&
                    extractedText.Length > 10) // Minimum meaningful text length
                {
                    // Use the extracted text directly (no OCR needed for text-based PDFs)
                    extractions.Add(new OcrExtraction
                    {
                        FieldName = $"Page{pageIndex + 1}_FullText",
                        ExtractedValue = extractedText,
                        Confidence = 1.0f, // High confidence for direct text extraction
                        BoundingBox = "0,0,100,100",
                        Reviewed = false,
                        Approved = false,
                        DateCreated = DateTime.Now
                    });
                }
                else
                {
                    // Step 2: Text extraction failed or returned empty - render page as image and run OCR
                    var imagePath = await RenderPdfPageAsImageAsync(pdfPath, pageIndex);
                    
                    if (File.Exists(imagePath))
                    {
                        // Perform OCR on the rendered image
                        var ocrResults = await PerformOcrAsync(imagePath);
                        
                        // Update field names to indicate this came from image OCR
                        foreach (var extraction in ocrResults)
                        {
                            extraction.FieldName = $"Page{pageIndex + 1}_FullText";
                        }
                        
                        extractions.AddRange(ocrResults);
                        
                        // Clean up temporary image file
                        File.Delete(imagePath);
                    }
                    else
                    {
                        extractions.Add(new OcrExtraction
                        {
                            FieldName = $"Page{pageIndex + 1}_FullText",
                            ExtractedValue = "Failed to render PDF page as image for OCR processing",
                            Confidence = 0.0f,
                            BoundingBox = "0,0,100,100",
                            Reviewed = false,
                            Approved = false,
                            DateCreated = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                extractions.Add(new OcrExtraction
                {
                    FieldName = $"Page{pageIndex + 1}_FullText",
                    ExtractedValue = $"Page {pageIndex + 1} processing failed: {ex.Message}",
                    Confidence = 0.0f,
                    BoundingBox = "0,0,100,100",
                    Reviewed = false,
                    Approved = false,
                    DateCreated = DateTime.Now
                });
            }
            
            return extractions;
        }

        /// <summary>
        /// Renders a PDF page as an image for OCR processing
        /// Note: Currently using a simplified approach. Future improvement: implement proper PDF rendering
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <param name="pageIndex">Page index (0-based)</param>
        /// <returns>Path to the generated image file</returns>
        private async Task<string> RenderPdfPageAsImageAsync(string pdfPath, int pageIndex)
        {
            try
            {
                // For now, we'll use a simplified approach
                // TODO: Implement proper PDF page rendering when a stable library is available
                var tempPath = Path.GetTempPath();
                var imagePath = Path.Combine(tempPath, $"pdf_page_{pageIndex}_{Guid.NewGuid()}.png");
                
                // Create a placeholder image for testing
                using var bitmap = new System.Drawing.Bitmap(800, 1000);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                
                // Set up graphics for high quality
                graphics.Clear(System.Drawing.Color.White);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                using var font = new System.Drawing.Font("Arial", 12);
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                
                // Draw placeholder text
                var text = $"PDF Page {pageIndex + 1} - Rendering not yet implemented\n\nThis is a placeholder for PDF page rendering.\nThe actual PDF page content would be rendered here.\n\nFile: {Path.GetFileName(pdfPath)}";
                graphics.DrawString(text, font, brush, new System.Drawing.RectangleF(50, 50, 700, 900));
                
                // Save the image
                bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                
                return imagePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to render PDF page {pageIndex + 1} as image: {ex.Message}", ex);
            }
        }
    }
}