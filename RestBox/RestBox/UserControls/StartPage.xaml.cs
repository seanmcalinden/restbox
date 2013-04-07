using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Domain.Services;
using RestBox.ViewModels;
using System.Diagnostics;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : UserControl
    {
        private readonly IMainMenuApplicationService mainMenuApplicationService;

        public StartPage(IMainMenuApplicationService mainMenuApplicationService, StartPageViewModel startPageViewModel)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            DataContext = startPageViewModel;

            InitializeComponent();

            StartPageWebBrowser.Navigate(new Uri("http://www.google.co.uk"));
        }

        private void OpenItem(object sender, RoutedEventArgs e)
        {
            var result = mainMenuApplicationService.OpenSolution((RecentFilesDataGrid.SelectedItem as RestBoxStateFile).FilePath);
            if (!result)
            {
                MessageBox.Show("Cant file the solution file");
            }
        }
    }
}
