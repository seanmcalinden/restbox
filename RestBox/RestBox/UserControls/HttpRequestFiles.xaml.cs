using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Utilities;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for HttpRequestFiles.xaml
    /// </summary>
    public partial class HttpRequestFiles
    {
        #region Declarations

        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;
        private readonly HttpRequestFilesViewModel httpRequestFilesViewModel; 

        #endregion
        
        #region Constructor
        
        public HttpRequestFiles(HttpRequestFilesViewModel httpRequestFilesViewModel, IFileService fileService, IEventAggregator eventAggregator)
        {
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            this.httpRequestFilesViewModel = httpRequestFilesViewModel;
            DataContext = httpRequestFilesViewModel;
            InitializeComponent();
            eventAggregator.GetEvent<SelectHttpRequestItemEvent>().Subscribe(SelectHttpRequestItem);
        } 

        #endregion

        #region Event Handlers

        private void SelectHttpRequestItem(string id)
        {
            if (id == null)
            {
                HttpRequestFilesDataGrid.SelectedItem = null;
            }

            var counter = 0;
            foreach (var httpRequestFile in httpRequestFilesViewModel.ViewFiles)
            {
                if (httpRequestFile.Id == id)
                {
                    HttpRequestFilesDataGrid.SelectedIndex = counter;
                }
                counter++;
            }
        }

        private void UpdateGroupsEvent(object sender, TextChangedEventArgs e)
        {
            httpRequestFilesViewModel.UpdateGroups();
        }

        private void Rename(object sender, RoutedEventArgs e)
        {
            var selectedItem = HttpRequestFilesDataGrid.SelectedItem as ViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for (var i = 0; i < relativePathParts.Length; i++)
            {
                if (i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + "." + SystemFileTypes.HttpRequest.Extension);
                    break;
                }
                sb.Append(relativePathParts[i] + "/");
            }

            var newRelativePath = sb.ToString();

            var destinationFilePath = fileService.GetFilePath(Solution.Current.FilePath, newRelativePath);

            fileService.MoveFile(sourceFilePath, destinationFilePath);

            selectedItem.RelativeFilePath = newRelativePath;

            var solutionItem = Solution.Current.HttpRequestFiles.First(x => x.Id == selectedItem.Id);
            solutionItem.Name = selectedItem.Name;
            solutionItem.RelativeFilePath = newRelativePath;

            fileService.SaveSolution();

            eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                                                                        {
                                                                            Id = selectedItem.Id,
                                                                            Title = selectedItem.Name
                                                                        });
        }

        private void ActivateItem(object sender, MouseButtonEventArgs e)
        {
            httpRequestFilesViewModel.ActivateItem();
        } 

        #endregion
    }
}
