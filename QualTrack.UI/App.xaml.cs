using System.Windows;

namespace QualTrack.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize database
            using var dbContext = new QualTrack.Data.Database.DatabaseContext();
            dbContext.InitializeDatabase();
        }
    }
} 