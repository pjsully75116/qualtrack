using System.Data.SQLite;
using QualTrack.Core.Models;

namespace QualTrack.Data.Database
{
    /// <summary>
    /// Manages database connections and provides access to SQLite database
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection? _connection;

        public DatabaseContext(string dbPath = "qualtrack.db")
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        /// <summary>
        /// Gets a database connection
        /// </summary>
        /// <returns>SQLite connection</returns>
        public SQLiteConnection GetConnection()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
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
        }

        private void CreatePersonnelTable(SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS personnel (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    dod_id TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    rate TEXT NOT NULL
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
                    FOREIGN KEY (personnel_id) REFERENCES personnel(id)
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
                    nhqc_score INTEGER,
                    hllc_score INTEGER,
                    hpwc_score INTEGER,
                    rqc_score INTEGER,
                    rlc_score INTEGER,
                    instructor TEXT,
                    remarks TEXT,
                    FOREIGN KEY (qualification_id) REFERENCES qualifications(id)
                )";
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
} 