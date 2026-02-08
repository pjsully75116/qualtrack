using System.Windows;
using System.Windows.Threading;
using QualTrack.Core.Services;
using QualTrack.UI.Services;

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
            
            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (s, exArgs) =>
            {
                MessageBox.Show("A critical error occurred. Please restart the application.\n\n" +
                    (exArgs.ExceptionObject as Exception)?.Message,
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            this.DispatcherUnhandledException += (s, exArgs) =>
            {
                MessageBox.Show("An unexpected error occurred. Please check your input.\n\n" +
                    exArgs.Exception.Message,
                    "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                exArgs.Handled = true;
            };

            var basePath = StorageSetupService.GetOrPromptForBasePath(Current?.MainWindow);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                MessageBox.Show("A shared storage location is required to run QualTrack.", "Setup Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            StorageSetupService.InitializeStorage(basePath);

            // Initialize database
            using var dbContext = new QualTrack.Data.Database.DatabaseContext();
            dbContext.InitializeDatabase();
        }
    }
} 