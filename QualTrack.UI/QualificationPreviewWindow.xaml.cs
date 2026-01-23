using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QualTrack.UI.Models;

namespace QualTrack.UI
{
    /// <summary>
    /// Interaction logic for QualificationPreviewWindow.xaml
    /// </summary>
    public partial class QualificationPreviewWindow : Window
    {
        public SailorQualification Sailor { get; set; }
        public List<QualificationAssessment> Assessments { get; set; }
        public bool SaveConfirmed { get; private set; } = false;

        public QualificationPreviewWindow(SailorQualification sailor, List<QualificationAssessment> assessments)
        {
            InitializeComponent();
            Sailor = sailor;
            Assessments = assessments;
            PopulateQualificationDetails();
        }

        private void PopulateQualificationDetails()
        {
            // Set sailor info
            SailorInfoText.Text = $"{Sailor.FullName} (DoD ID: {Sailor.DodId})";

            // Clear existing content
            QualificationDetailsPanel.Children.Clear();

            // Add qualification assessments
            foreach (var assessment in Assessments)
            {
                // Only show weapons that have any data
                if (assessment.PresentFields.Count == 0 && assessment.MissingFields.Count == 0)
                    continue;

                // Create weapon header
                var weaponHeader = new TextBlock
                {
                    Text = $"Weapon: {assessment.Weapon}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 10),
                    Foreground = assessment.IsQualified ? Brushes.Green : Brushes.Red
                };
                QualificationDetailsPanel.Children.Add(weaponHeader);

                // Add status
                var statusText = new TextBlock
                {
                    Text = assessment.IsQualified ? "✅ QUALIFIED" : "❌ NOT QUALIFIED",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = assessment.IsQualified ? Brushes.Green : Brushes.Red
                };
                QualificationDetailsPanel.Children.Add(statusText);

                // Add present fields
                if (assessment.PresentFields.Count > 0)
                {
                    var presentHeader = new TextBlock
                    {
                        Text = "Present Scores:",
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 10, 0, 5)
                    };
                    QualificationDetailsPanel.Children.Add(presentHeader);

                    foreach (var field in assessment.PresentFields)
                    {
                        var fieldText = new TextBlock
                        {
                            Text = $"• {field}",
                            Margin = new Thickness(20, 0, 0, 2),
                            Foreground = Brushes.Green
                        };
                        QualificationDetailsPanel.Children.Add(fieldText);
                    }
                }

                // Add missing fields
                if (assessment.MissingFields.Count > 0)
                {
                    var missingHeader = new TextBlock
                    {
                        Text = "Missing Requirements:",
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 10, 0, 5),
                        Foreground = Brushes.Red
                    };
                    QualificationDetailsPanel.Children.Add(missingHeader);

                    foreach (var field in assessment.MissingFields)
                    {
                        var fieldText = new TextBlock
                        {
                            Text = $"• {field}",
                            Margin = new Thickness(20, 0, 0, 2),
                            Foreground = Brushes.Red
                        };
                        QualificationDetailsPanel.Children.Add(fieldText);
                    }
                }

                // Add separator
                var separator = new Separator { Margin = new Thickness(0, 15, 0, 0) };
                QualificationDetailsPanel.Children.Add(separator);
            }
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfirmed = false;
            DialogResult = false;
            Close();
        }
    }


} 