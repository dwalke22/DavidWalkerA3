﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Covid19Analysis.DataHandling;
using Covid19Analysis.Model;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Covid19Analysis.View
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainPage" /> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.DataCreator = new CovidDataCreator();
            this.LoadedDataCollection = new CovidDataCollection();
            this.UpperBoundaryLimit = GetBoundariesContentDialog.UpperBoundaryDefault;
            this.LowerBoundaryLimit = GetBoundariesContentDialog.LowerBoundaryDefault;

            ApplicationView.PreferredLaunchViewSize = new Size {Width = ApplicationWidth, Height = ApplicationHeight};
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(ApplicationWidth, ApplicationHeight));
        }

        #endregion

        #region Methods

        private async void changeBoundaries_Click(object sender, RoutedEventArgs e)
        {
            var boundaryContentDialog = new GetBoundariesContentDialog();

            var result = await boundaryContentDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                this.UpperBoundaryLimit = boundaryContentDialog.UpperBoundary;
                this.LowerBoundaryLimit = boundaryContentDialog.LowerBoundary;
                if (this.LoadedDataCollection.CovidRecords.Count > 0)
                {
                    this.CreateNewReportSummary();
                }
            }
        }

        private async void loadFile_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker {
                ViewMode = PickerViewMode.Thumbnail, SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".csv");
            openPicker.FileTypeFilter.Add(".txt");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                await this.processFile(file);
            }
            else
            {
                this.SummaryTextBox.Text = "Operation cancelled.";
            }
        }

        private async Task processFile(StorageFile file)
        {
            this.DataCreator.ErrorLines.Clear();
            var lines = await getFileLines(file);
            this.DataCreator.CreateCovidData(lines);
            var stateCovidData = this.DataCreator.GetStateCovidData(DefaualtStateSelector);
            if (this.LoadedDataCollection.CovidRecords.Any())
            {
                this.handleExistingFileLoading(stateCovidData);
            }
            else
            {
                this.LoadedDataCollection = stateCovidData;
            }
            this.CreateNewReportSummary();
        }

        private async void handleExistingFileLoading(CovidDataCollection covidCollection)
        {
            var loadingDialog = new ContentDialog()
            {
                Title = "There Is Already A File Loaded",
                Content = "Do You Wish To Replace Or Merge This File?",
                PrimaryButtonText = "Replace",
                SecondaryButtonText = "Merge"
            };

            var result = await loadingDialog.ShowAsync();

            if (result == Replace)
            {
                this.LoadedDataCollection = covidCollection;
                this.CreateNewReportSummary();
            }

            if (result == Merge)
            {
                this.mergeFile(covidCollection);
            }
        }

        private void mergeFile(CovidDataCollection covidCollection)
        {

        }

        private void CreateNewReportSummary()
        {
            var stateMonthData = new MonthlyCovidDataCollection(this.LoadedDataCollection);
            var covidFormatter = new CovidDataFormatter(this.LoadedDataCollection);
            this.SummaryTextBox.Text = "";
            this.SummaryTextBox.Text = covidFormatter.FormatGeneralData(this.UpperBoundaryLimit, this.LowerBoundaryLimit);
            this.SummaryTextBox.Text += covidFormatter.FormatMonthlyData(stateMonthData);
        }

        private async void errorLines_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataCreator.ErrorLines.Count == 0)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Lines With Errors",
                    Content = "No Lines With Errors",
                    PrimaryButtonText = "Close"
                };

                await errorDialog.ShowAsync();
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Lines With Errors",
                    Content = new CovidDataFormatter(this.LoadedDataCollection).ErrorLinesToString(this.DataCreator),
                    PrimaryButtonText = "Close"
                };

                await errorDialog.ShowAsync();
            }
        }

        private static async Task<string[]> getFileLines(StorageFile file)
        {
            var fileText = await FileIO.ReadTextAsync(file);
            var lines = fileText.Split("\r\n");
            return lines;
        }

        #endregion

        #region CovidRecords members

        /// <summary>
        ///     The application height
        /// </summary>
        public const int ApplicationHeight = 355;

        /// <summary>
        ///     The application width
        /// </summary>
        public const int ApplicationWidth = 625;

        /// <summary>
        ///     The default State selector to get from a covid collection
        /// </summary>
        public const string DefaualtStateSelector = "GA";

        /// <summary>
        ///     The upper boundary limit of the threshold
        /// </summary>
        public int UpperBoundaryLimit { get; set; }

        /// <summary>
        ///     the lower boundary limit of the threshold
        /// </summary>
        public int LowerBoundaryLimit { get; set; }

        /// <summary>
        ///     The CovidData Collection that is loaded into the app
        /// </summary>
        public CovidDataCollection LoadedDataCollection { get; set; }

        /// <summary>
        ///     The DataCreator for the application
        /// </summary>
        public CovidDataCreator DataCreator { get; set; }

        private const ContentDialogResult Replace = ContentDialogResult.Primary;

        private const ContentDialogResult Merge = ContentDialogResult.Secondary;

        #endregion
    }
}