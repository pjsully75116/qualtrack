using System;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using QualTrack.Core.Models;

namespace QualTrack.Data.Database
{
    /// <summary>
    /// Manages database connections and provides access to SQLite database
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;

        public DatabaseContext(string dbPath = "qualtrack.db")
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        /// <summary>
        /// Gets a new database connection
        /// </summary>
        /// <returns>SQLite connection</returns>
        public SQLiteConnection GetConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Initializes the database with required tables
        /// </summary>
        public void InitializeDatabase()
        {
            using var connection = GetConnection();
            
            // Create tables
            CreatePersonnelTable(connection);
            CreateQualificationsTable(connection);
            CreateDutySectionsTable(connection);
            CreatePersonnelDutyTable(connection);
            CreateUsersTable(connection);
            CreateAuditLogTable(connection);
            CreateOcrRecordsTable(connection);
            CreateQualificationDetailsTable(connection);
            CreateAdditionalRequirementsTable(connection);
            CreateDocumentsTable(connection);
            CreateOcrExtractionsTable(connection);
            CreateQualificationSessionsTable(connection);
            CreateDD2760FormsTable(connection);
            
            // Run database migrations
            RunDatabaseMigrations(connection);
            
            // Create indexes for performance
            CreateIndexes(connection);
        }

        /// <summary>
        /// Runs database migrations to update existing databases to the current schema
        /// </summary>
        private void RunDatabaseMigrations(SQLiteConnection connection)
        {
            try
            {
                // Migration 1: Add rank column to personnel table if it doesn't exist
                if (!ColumnExists(connection, "personnel", "rank"))
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "ALTER TABLE personnel ADD COLUMN rank TEXT NOT NULL DEFAULT 'E-1'";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Migration: Added rank column to personnel table");
                }

                // Migration 2: Add sustainment_date and sustainment_score columns to qualification_details if they don't exist
                if (!ColumnExists(connection, "qualification_details", "sustainment_date"))
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "ALTER TABLE qualification_details ADD COLUMN sustainment_date TEXT";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Migration: Added sustainment_date column to qualification_details table");
                }

                if (!ColumnExists(connection, "qualification_details", "sustainment_score"))
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "ALTER TABLE qualification_details ADD COLUMN sustainment_score INTEGER";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Migration: Added sustainment_score column to qualification_details table");
                }

                // Migration 3: Add qualification_session_id column to qualifications table if it doesn't exist
                if (!ColumnExists(connection, "qualifications", "qualification_session_id"))
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "ALTER TABLE qualifications ADD COLUMN qualification_session_id INTEGER";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Migration: Added qualification_session_id column to qualifications table");
                }

                // Migration 4: Add additional_requirements table if it doesn't exist
                if (!TableExists(connection, "additional_requirements"))
                {
                    CreateAdditionalRequirementsTable(connection);
                    Console.WriteLine("Migration: Added additional_requirements table");
                }

                // Migration 5: Add qualification_sessions table if it doesn't exist
                if (!TableExists(connection, "qualification_sessions"))
                {
                    CreateQualificationSessionsTable(connection);
                    Console.WriteLine("Migration: Added qualification_sessions table");
                }

                // Migration 6: Add dd2760_forms table if it doesn't exist
                if (!TableExists(connection, "dd2760_forms"))
                {
                    CreateDD2760FormsTable(connection);
                    Console.WriteLine("Migration: Added dd2760_forms table");
                }
                
                // Migration 7: Add aae_screening_forms table if it doesn't exist
                if (!TableExists(connection, "aae_screening_forms"))
                {
                    CreateAAEScreeningFormsTable(connection);
                    Console.WriteLine("Migration: Added aae_screening_forms table");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Database migration failed: {ex.Message}");
                // Don't throw - allow the application to continue
            }
        }

        /// <summary>
        /// Checks if a column exists in a table
        /// </summary>
        private bool ColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info({tableName})";
                
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a table exists
        /// </summary>
        private bool TableExists(SQLiteConnection connection, string tableName)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
                command.Parameters.AddWithValue("@tableName", tableName);
                
                var result = command.ExecuteScalar();
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private void CreatePersonnelTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS personnel (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    dod_id TEXT NOT NULL UNIQUE,
                    last_name TEXT NOT NULL,
                    first_name TEXT NOT NULL,
                    rate TEXT NOT NULL,
                    rank TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        private void CreateQualificationsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS qualifications (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER,
                    weapon TEXT NOT NULL,
                    category INTEGER NOT NULL,
                    date_qualified TEXT NOT NULL,
                    qualification_session_id INTEGER,
                    FOREIGN KEY (personnel_id) REFERENCES personnel(id),
                    FOREIGN KEY (qualification_session_id) REFERENCES qualification_sessions(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateDutySectionsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS duty_sections (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    label TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        private void CreatePersonnelDutyTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS personnel_duty (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER,
                    duty_section_type TEXT NOT NULL,
                    section_number TEXT NOT NULL,
                    FOREIGN KEY (personnel_id) REFERENCES personnel(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateUsersTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    role TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        private void CreateAuditLogTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS audit_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    user_id INTEGER,
                    action TEXT NOT NULL,
                    details TEXT
                )";
            command.ExecuteNonQuery();
        }

        private void CreateOcrRecordsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ocr_text_records (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    filename TEXT NOT NULL,
                    text TEXT NOT NULL,
                    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
            command.ExecuteNonQuery();
        }

        private void CreateQualificationDetailsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS qualification_details (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    qualification_id INTEGER,
                    hqc_score INTEGER,
                    nhqc_score INTEGER,
                    hllc_score INTEGER,
                    hpwc_score INTEGER,
                    rqc_score INTEGER,
                    rlc_score INTEGER,
                    spwc_score INTEGER,
                    cof_score INTEGER,
                    cswi TEXT,
                    instructor TEXT,
                    remarks TEXT,
                    qualified_underway INTEGER,
                    sustainment_date TEXT,
                    sustainment_score INTEGER,
                    FOREIGN KEY (qualification_id) REFERENCES qualifications(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateAdditionalRequirementsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS additional_requirements (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER NOT NULL,
                    form2760_date TEXT,
                    form2760_number TEXT,
                    form2760_signed_date TEXT,
                    form2760_witness TEXT,
                    aae_screening_date TEXT,
                    aae_screening_level TEXT,
                    aae_investigation_type TEXT,
                    aae_investigation_date TEXT,
                    aae_investigation_agency TEXT,
                    deadly_force_training_date TEXT,
                    deadly_force_instructor TEXT,
                    deadly_force_remarks TEXT,
                    date_created TEXT NOT NULL,
                    date_modified TEXT,
                    FOREIGN KEY (personnel_id) REFERENCES personnel(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateDocumentsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS documents (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER,
                    document_type TEXT NOT NULL,
                    original_filename TEXT NOT NULL,
                    file_path TEXT NOT NULL,
                    file_size INTEGER NOT NULL,
                    upload_date TEXT NOT NULL,
                    ocr_processed BOOLEAN DEFAULT FALSE,
                    ocr_confidence REAL,
                    date_created TEXT NOT NULL,
                    date_modified TEXT,
                    FOREIGN KEY (personnel_id) REFERENCES personnel(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateOcrExtractionsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ocr_extractions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    document_id INTEGER NOT NULL,
                    field_name TEXT NOT NULL,
                    extracted_value TEXT,
                    confidence REAL,
                    bounding_box TEXT,
                    reviewed BOOLEAN DEFAULT FALSE,
                    approved BOOLEAN DEFAULT FALSE,
                    corrected_value TEXT,
                    date_created TEXT NOT NULL,
                    date_modified TEXT,
                    FOREIGN KEY (document_id) REFERENCES documents(id)
                )";
            command.ExecuteNonQuery();
        }

        private void CreateQualificationSessionsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS qualification_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ship_station TEXT NOT NULL,
                    division_activity TEXT NOT NULL,
                    weapons_fired TEXT NOT NULL,
                    range_name_location TEXT NOT NULL,
                    date_of_firing TEXT,
                    rso_signature TEXT,
                    rso_signature_rate TEXT,
                    rso_signature_date TEXT,
                    pdf_file_path TEXT,
                    created_date TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        private void CreateDD2760FormsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS dd2760_forms (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER NOT NULL,
                    date_completed TEXT NOT NULL,
                    date_expires TEXT NOT NULL,
                    date_created TEXT NOT NULL,
                    date_modified TEXT,
                    domestic_violence_response TEXT,
                    domestic_violence_initials TEXT,
                    domestic_violence_date TEXT,
                    court_jurisdiction TEXT,
                    docket_case_number TEXT,
                    statute_charge TEXT,
                    date_sentenced TEXT,
                    certifier_name TEXT,
                    certifier_rank TEXT,
                    certifier_ssn TEXT,
                    certifier_organization TEXT,
                    is_certified INTEGER DEFAULT 0,
                    certifier_signature_date TEXT,
                    pdf_file_path TEXT,
                    pdf_file_name TEXT,
                    is_valid INTEGER DEFAULT 1,
                    status_notes TEXT,
                    FOREIGN KEY (personnel_id) REFERENCES personnel (id)
                )";
            
            command.ExecuteNonQuery();
            
            // Add new columns if they don't exist (for existing databases)
            if (!ColumnExists(connection, "dd2760_forms", "date_sentenced"))
            {
                var addDateSentencedCommand = connection.CreateCommand();
                addDateSentencedCommand.CommandText = "ALTER TABLE dd2760_forms ADD COLUMN date_sentenced TEXT";
                addDateSentencedCommand.ExecuteNonQuery();
            }
            
            if (!ColumnExists(connection, "dd2760_forms", "is_certified"))
            {
                var addIsCertifiedCommand = connection.CreateCommand();
                addIsCertifiedCommand.CommandText = "ALTER TABLE dd2760_forms ADD COLUMN is_certified INTEGER DEFAULT 0";
                addIsCertifiedCommand.ExecuteNonQuery();
            }
            command.ExecuteNonQuery();
        }

        private void CreateAAEScreeningFormsTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS aae_screening_forms (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    personnel_id INTEGER NOT NULL,
                    date_completed TEXT NOT NULL,
                    date_expires TEXT NOT NULL,
                    date_created TEXT NOT NULL,
                    date_modified TEXT,
                    name_screened TEXT,
                    rank_screened TEXT,
                    dodid_screened TEXT,
                    signature_screened TEXT,
                    date_screened TEXT,
                    name_screener TEXT,
                    rank_screener TEXT,
                    dodid_screener TEXT,
                    signature_screener TEXT,
                    date_screener TEXT,
                    question1_response TEXT,
                    question2_response TEXT,
                    question3_response TEXT,
                    question4_response TEXT,
                    question5_response TEXT,
                    question6_response TEXT,
                    question7_response TEXT,
                    remarks1 TEXT,
                    remarks2 TEXT,
                    remarks3 TEXT,
                    remarks4 TEXT,
                    remarks5 TEXT,
                    remarks6 TEXT,
                    remarks7 TEXT,
                    qualified INTEGER DEFAULT 0,
                    unqualified INTEGER DEFAULT 0,
                    review_later INTEGER DEFAULT 0,
                    other_qualified_field TEXT,
                    pdf_file_path TEXT,
                    pdf_file_name TEXT,
                    is_valid INTEGER DEFAULT 1,
                    status_notes TEXT,
                    FOREIGN KEY (personnel_id) REFERENCES personnel (id)
                )";
            
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates database indexes for performance optimization
        /// </summary>
        private void CreateIndexes(SQLiteConnection connection)
        {
            // Personnel table indexes
            CreateIndexIfNotExists(connection, "idx_personnel_dod_id", "personnel", "dod_id");
            CreateIndexIfNotExists(connection, "idx_personnel_name_rate", "personnel", "last_name, first_name, rate");
            
            // Qualifications table indexes (most critical for performance)
            CreateIndexIfNotExists(connection, "idx_qualifications_personnel_id", "qualifications", "personnel_id");
            CreateIndexIfNotExists(connection, "idx_qualifications_weapon", "qualifications", "weapon");
            CreateIndexIfNotExists(connection, "idx_qualifications_date", "qualifications", "date_qualified");
            CreateIndexIfNotExists(connection, "idx_qualifications_personnel_weapon", "qualifications", "personnel_id, weapon");
            
            // Personnel duty table indexes
            CreateIndexIfNotExists(connection, "idx_personnel_duty_personnel_id", "personnel_duty", "personnel_id");
            CreateIndexIfNotExists(connection, "idx_personnel_duty_type_section", "personnel_duty", "duty_section_type, section_number");
            
            // Qualification details table indexes
            CreateIndexIfNotExists(connection, "idx_qualification_details_qual_id", "qualification_details", "qualification_id");
            
            // Additional requirements table indexes
            CreateIndexIfNotExists(connection, "idx_additional_requirements_personnel_id", "additional_requirements", "personnel_id");
            CreateIndexIfNotExists(connection, "idx_additional_requirements_form2760_date", "additional_requirements", "form2760_date");
            CreateIndexIfNotExists(connection, "idx_additional_requirements_aae_screening_date", "additional_requirements", "aae_screening_date");
            CreateIndexIfNotExists(connection, "idx_additional_requirements_deadly_force_training_date", "additional_requirements", "deadly_force_training_date");
            
            // Documents table indexes
            CreateIndexIfNotExists(connection, "idx_documents_personnel_id", "documents", "personnel_id");
            CreateIndexIfNotExists(connection, "idx_documents_document_type", "documents", "document_type");
            CreateIndexIfNotExists(connection, "idx_documents_upload_date", "documents", "upload_date");
            CreateIndexIfNotExists(connection, "idx_documents_ocr_processed", "documents", "ocr_processed");
            
            // OCR extractions table indexes
            CreateIndexIfNotExists(connection, "idx_ocr_extractions_document_id", "ocr_extractions", "document_id");
            CreateIndexIfNotExists(connection, "idx_ocr_extractions_field_name", "ocr_extractions", "field_name");
            CreateIndexIfNotExists(connection, "idx_ocr_extractions_reviewed", "ocr_extractions", "reviewed");
            CreateIndexIfNotExists(connection, "idx_ocr_extractions_approved", "ocr_extractions", "approved");
            
            // Qualification sessions table indexes
            CreateIndexIfNotExists(connection, "idx_qualification_sessions_date", "qualification_sessions", "date_of_firing");
            CreateIndexIfNotExists(connection, "idx_qualification_sessions_ship_station", "qualification_sessions", "ship_station");
            
            // Audit log table indexes
            CreateIndexIfNotExists(connection, "idx_audit_log_timestamp", "audit_log", "timestamp");
            CreateIndexIfNotExists(connection, "idx_audit_log_user_id", "audit_log", "user_id");
        }

        /// <summary>
        /// Creates an index if it doesn't already exist
        /// </summary>
        private void CreateIndexIfNotExists(SQLiteConnection connection, string indexName, string tableName, string columns)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire initialization
                Console.WriteLine($"Warning: Failed to create index {indexName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets the database by deleting all tables and recreating them
        /// Use with caution - this will delete all data!
        /// </summary>
        public void ResetDatabase()
        {
            // Small delay to allow any existing connections to fully close
            Thread.Sleep(100);
            
            // Try multiple times in case of connection conflicts
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var connection = GetConnection();
                    
                    // Drop all tables in reverse dependency order
                    var dropCommands = new[]
                    {
                        "DROP TABLE IF EXISTS qualification_details",
                        "DROP TABLE IF EXISTS qualifications", 
                        "DROP TABLE IF EXISTS qualification_sessions",
                        "DROP TABLE IF EXISTS additional_requirements",
                        "DROP TABLE IF EXISTS personnel_duty",
                        "DROP TABLE IF EXISTS duty_sections",
                        "DROP TABLE IF EXISTS audit_log",
                        "DROP TABLE IF EXISTS users",
                        "DROP TABLE IF EXISTS ocr_text_records",
                        "DROP TABLE IF EXISTS ocr_extractions",
                        "DROP TABLE IF EXISTS documents",
                        "DROP TABLE IF EXISTS personnel"
                    };
                    
                    foreach (var dropCommand in dropCommands)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = dropCommand;
                        command.ExecuteNonQuery();
                    }
                    
                    // Recreate all tables using the same connection
                    CreatePersonnelTable(connection);
                    CreateQualificationsTable(connection);
                    CreateDutySectionsTable(connection);
                    CreatePersonnelDutyTable(connection);
                    CreateUsersTable(connection);
                    CreateAuditLogTable(connection);
                    CreateOcrRecordsTable(connection);
                    CreateQualificationDetailsTable(connection);
                    CreateAdditionalRequirementsTable(connection);
                    CreateDocumentsTable(connection);
                    CreateOcrExtractionsTable(connection);
                    CreateQualificationSessionsTable(connection);
                    
                    // Run migrations
                    RunDatabaseMigrations(connection);
                    
                    // Create indexes
                    CreateIndexes(connection);
                    
                    // Success - exit the retry loop
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == 3)
                    {
                        // Final attempt failed, throw the exception
                        throw new InvalidOperationException($"Failed to reset database after {attempt} attempts. Last error: {ex.Message}", ex);
                    }
                    
                    // Wait before retrying
                    Thread.Sleep(500 * attempt);
                }
            }
        }

        /// <summary>
        /// Alternative reset method that deletes the database file entirely and recreates it
        /// This is more reliable when Visual Studio or other processes are holding connections
        /// </summary>
        public void ResetDatabaseByFile()
        {
            try
            {
                // Get the database file path
                var dbPath = Path.GetFullPath(_connectionString);
                Console.WriteLine($"Attempting to delete database file: {dbPath}");
                
                // Wait a moment for any file locks to be released
                Thread.Sleep(500);
                
                // Force delete the database file if it exists
                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                        Console.WriteLine($"Database file deleted: {dbPath}");
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"Warning: Could not delete file: {deleteEx.Message}");
                        // Try to delete with different approach
                        try
                        {
                            File.SetAttributes(dbPath, FileAttributes.Normal);
                            File.Delete(dbPath);
                            Console.WriteLine($"Database file deleted on second attempt: {dbPath}");
                        }
                        catch (Exception deleteEx2)
                        {
                            throw new InvalidOperationException($"Cannot delete database file: {deleteEx2.Message}");
                        }
                    }
                    Thread.Sleep(200); // Wait for file deletion to complete
                }
                else
                {
                    Console.WriteLine($"Database file not found: {dbPath}");
                }
                
                // Verify the file is actually deleted
                if (File.Exists(dbPath))
                {
                    throw new InvalidOperationException($"Database file still exists after deletion: {dbPath}");
                }
                
                // Recreate the database from scratch
                Console.WriteLine("Recreating database from scratch...");
                
                // Create a completely fresh context for initialization
                var tempContext = new DatabaseContext(_connectionString);
                tempContext.InitializeDatabase();
                tempContext.Dispose();
                
                // Verify the database was created
                if (!File.Exists(dbPath))
                {
                    throw new InvalidOperationException($"Database file was not created: {dbPath}");
                }
                
                Console.WriteLine($"Database successfully recreated: {dbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ResetDatabaseByFile: {ex.Message}");
                throw new InvalidOperationException($"Failed to reset database by file deletion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the full path to the database file
        /// </summary>
        public string GetDatabaseFilePath()
        {
            return Path.GetFullPath(_connectionString);
        }

        public void Dispose()
        {
            // No shared connection to dispose - each GetConnection() call creates a new connection
            // that should be disposed by the caller
        }
    }
} 