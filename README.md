# QualTrack Desktop Application

A C# WPF desktop application for managing Navy personnel arms qualifications according to OPNAVINST 3591.1G.

## Project Structure

```
QualTrack/
├── QualTrack.Core/          # Business logic and models
│   ├── Models/              # Data models (Personnel, Qualification, etc.)
│   └── Services/            # Business logic services
├── QualTrack.Data/          # Data access layer
│   ├── Database/            # Database context and schema
│   └── Repositories/        # Data access repositories
├── QualTrack.UI/            # WPF user interface
└── QualTrack.sln           # Solution file
```

## Current Implementation Status

### ✅ Completed Features
- **Core Models**: Personnel, Qualification, QualificationStatus, PersonnelViewModel
- **Business Logic**: QualificationService with OPNAVINST 3591.1G compliance
- **Data Access**: Complete repository implementations for CRUD operations
- **Database**: SQLite schema with all required tables
- **UI**: WPF interface with dashboard and qualification management
- **Security**: Audit logging for all user actions
- **Export**: CSV export functionality

### 🔄 In Progress
- **Testing**: Application testing and validation
- **Documentation**: User guides and deployment instructions

### 📋 Planned Features
- **Role-based Access Control**: User authentication and permissions
- **Data Encryption**: Sensitive data encryption
- **Advanced Reporting**: Detailed qualification reports
- **Backup/Restore**: Database backup functionality

## Features

### Core Functionality
- **Qualification Management**: Add, view, and track weapon qualifications
- **Personnel Management**: Manage Navy personnel records
- **OPNAVINST 3591.1G Compliance**: Automatic qualification status evaluation
- **Multi-Category Support**: CAT I-IV qualification categories
- **Duty Section Tracking**: Associate personnel with duty sections
- **Real-time Status**: Automatic calculation of qualification status

### Security Features
- **Local Data Storage**: All data stored locally using SQLite
- **Audit Logging**: Track all user actions (additions, exports, etc.)
- **No Network Dependencies**: Completely offline operation
- **Navy Security Standards**: Designed for unclassified Navy networks

### User Interface
- **Dashboard**: View all personnel with their qualification status
- **Filtering**: Filter by qualification status (Qualified, Sustainment Due, Disqualified)
- **Add Qualifications**: Simple form for adding new qualifications
- **Export Data**: Export to CSV format for reporting
- **Test Data**: Built-in test data loader for demonstration

## Installation Requirements

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime
- SQLite (included with application)

### Building from Source
1. Install .NET 8.0 SDK
2. Clone the repository
3. Open QualTrack.sln in Visual Studio
4. Build the solution
5. Run QualTrack.UI project

## Usage

### Getting Started
1. Launch the application
2. Database will be automatically initialized on first run
3. Use "File > Load Test Data" to populate with sample data
4. Navigate between Dashboard and Add Qualification tabs

### Adding Qualifications
1. Go to "Add Qualification" tab
2. Enter personnel name and rate
3. Select weapon and category
4. Choose qualification date
5. Optionally add duty sections (comma-separated)
6. Click "Add Qualification"

### Viewing Dashboard
- Main dashboard shows all personnel with their qualification status
- Use the filter dropdown to view specific status types
- Click "Refresh" to reload data
- Use "Export Qualifications" to save data as CSV

### Qualification Rules (OPNAVINST 3591.1G)

#### Category I & II
- **Full Validity**: 365 days
- **Sustainment Due**: 180 days
- **Weapons**: M9, M4/M16, M500

#### Category III
- **Full Validity**: 730 days (2 years)
- **Sustainment Due**: 365 days (1 year)
- **Weapons**: M240, M249

#### Category IV
- **Full Validity**: 1095 days (3 years)
- **Sustainment Due**: 547 days (1.5 years)
- **Weapons**: M2

## Development

### Project Dependencies
- **QualTrack.Core**: Business logic and models
- **QualTrack.Data**: SQLite data access
- **QualTrack.UI**: WPF user interface

### Key Classes
- `QualificationService`: Implements OPNAVINST 3591.1G rules
- `PersonnelRepository`: Data access for personnel
- `QualificationRepository`: Data access for qualifications
- `AuditService`: Security logging
- `DatabaseContext`: SQLite database management

### Database Schema
- `personnel`: Personnel records
- `qualifications`: Weapon qualifications
- `duty_sections`: Duty section definitions
- `personnel_duty`: Personnel-duty section relationships
- `audit_log`: Security audit trail
- `users`: User accounts (for future role-based access)

## Security Considerations

### Data Protection
- All personnel data is stored locally in SQLite
- No external network communication
- Comprehensive audit logging of all actions
- Designed for Navy unclassified networks

### Audit Logging
- All qualification additions are logged
- Data exports are tracked
- User actions are timestamped and stored
- Audit trail available for compliance

### Navy Network Deployment
- Designed for unclassified Navy networks
- No internet dependencies
- Compliant with Navy cybersecurity requirements
- Local installation and operation

## Testing

### Test Data
The application includes a test data loader with sample Navy personnel:
- 5 sample personnel with various rates
- Multiple qualifications across all categories
- Mix of valid, sustainment-due, and expired qualifications
- Various duty section assignments

### Manual Testing
1. Load test data using "File > Load Test Data"
2. Verify dashboard displays all personnel
3. Test filtering by status
4. Add new qualifications
5. Export data to CSV
6. Check audit log entries

## Support

For technical support or questions about Navy qualification requirements, contact your unit's RSO (Range Safety Officer) or IT department.

## License

This software is developed for Navy use and should be deployed according to Navy network security policies. 