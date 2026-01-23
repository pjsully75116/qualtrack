using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QualTrack.Core.Models;

namespace QualTrack.UI
{
    public partial class OcrReviewWindow : Window
    {
        private readonly Document _document;
        private readonly List<OcrExtraction> _extractions;
        private int _currentPageIndex = 0;

        public bool IsApproved { get; private set; }
        public string? CorrectedText { get; private set; }

        public OcrReviewWindow(Document document, List<OcrExtraction> extractions)
        {
            InitializeComponent();
            _document = document;
            _extractions = extractions;
            LoadExtractions();
        }

        private void LoadExtractions()
        {
            DocumentTitleText.Text = $"Document: {_document.OriginalFilename}";
            
            // Check if we have multiple pages
            var pageExtractions = _extractions.Where(e => e.FieldName.StartsWith("Page")).ToList();
            var singleExtraction = _extractions.FirstOrDefault(e => e.FieldName == "FullText");
            
            if (pageExtractions.Any())
            {
                // Multi-page document
                SetupPageNavigation(pageExtractions);
                PageNavigationPanel.Visibility = Visibility.Visible;
                LoadPage(0);
            }
            else if (singleExtraction != null)
            {
                // Single page/image document
                LoadSingleExtraction(singleExtraction);
            }
            else
            {
                // No extractions
                ExtractedTextBlock.Text = "No text was extracted from the document.";
                CorrectedTextBox.Text = "";
            }
        }

        private void SetupPageNavigation(List<OcrExtraction> pageExtractions)
        {
            // Extract page numbers and sort them
            var pages = pageExtractions
                .Select(e => int.Parse(e.FieldName.Split('_')[0].Replace("Page", "")))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            PageComboBox.Items.Clear();
            foreach (var page in pages)
            {
                PageComboBox.Items.Add($"Page {page}");
            }
            
            if (PageComboBox.Items.Count > 0)
            {
                PageComboBox.SelectedIndex = 0;
            }
            
            PageInfoText.Text = $"Page 1 of {pages.Count}";
        }

        private void LoadPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < PageComboBox.Items.Count)
            {
                _currentPageIndex = pageIndex;
                var pageNumber = pageIndex + 1;
                var pageExtraction = _extractions.FirstOrDefault(e => e.FieldName == $"Page{pageNumber}_FullText");
                
                if (pageExtraction != null)
                {
                    ConfidenceText.Text = $"Confidence: {pageExtraction.Confidence:P0}";
                    ExtractedTextBlock.Text = pageExtraction.ExtractedValue ?? "No text extracted";
                    CorrectedTextBox.Text = pageExtraction.ExtractedValue ?? "";
                }
                
                PageInfoText.Text = $"Page {pageNumber} of {PageComboBox.Items.Count}";
            }
        }

        private void LoadSingleExtraction(OcrExtraction extraction)
        {
            ConfidenceText.Text = $"Confidence: {extraction.Confidence:P0}";
            ExtractedTextBlock.Text = extraction.ExtractedValue ?? "No text extracted";
            CorrectedTextBox.Text = extraction.ExtractedValue ?? "";
        }

        private void PageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageComboBox.SelectedIndex >= 0)
            {
                LoadPage(PageComboBox.SelectedIndex);
            }
        }

        private void CopyToCorrectedButton_Click(object sender, RoutedEventArgs e)
        {
            CorrectedTextBox.Text = ExtractedTextBlock.Text;
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            IsApproved = true;
            CorrectedText = CorrectedTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            CorrectedText = CorrectedTextBox.Text;
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
            Close();
        }
    }
} 