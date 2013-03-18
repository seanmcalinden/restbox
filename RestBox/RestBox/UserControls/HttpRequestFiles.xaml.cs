using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for HttpRequestFiles.xaml
    /// </summary>
    public partial class HttpRequestFiles : UserControl
    {
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;
        private readonly HttpRequestFilesViewModel httpRequestFilesViewModel;

        public HttpRequestFiles(HttpRequestFilesViewModel httpRequestFilesViewModel, IFileService fileService, IEventAggregator eventAggregator)
        {
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            this.httpRequestFilesViewModel = httpRequestFilesViewModel;
            DataContext = httpRequestFilesViewModel;
            InitializeComponent();
            eventAggregator.GetEvent<CloneHttpRequestEvent>().Subscribe(CloneHttpRequest);
            eventAggregator.GetEvent<SelectHttpRequestItemEvent>().Subscribe(SelectHttpRequestItem);
        }

        private void SelectHttpRequestItem(string id)
        {
            if(id == null)
            {
                HttpRequestFilesDataGrid.SelectedItem = null;
            }

            var counter = 0;
            foreach (var httpRequestFile in httpRequestFilesViewModel.HttpRequestFiles)
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
            var selectedItem = HttpRequestFilesDataGrid.SelectedItem as HttpRequestFile;
            if (selectedItem != null && selectedItem.Groups != null)
            {
                Solution.Current.HttpRequestFiles.First(x => x.Id == selectedItem.Id).Groups = selectedItem.Groups;
                fileService.SaveSolution();
            }
        }

        private void RenameHttpRequest(object sender, RoutedEventArgs e)
        {
            var selectedItem = HttpRequestFilesDataGrid.SelectedItem as HttpRequestViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for (var i = 0; i < relativePathParts.Length; i++)
            {
                if (i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + ".rhrq");
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

        private void ActivateHttpRequestFile(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;

            var httpRequestViewFile = grid.CurrentItem as HttpRequestViewFile;
            var httpRequestFile = grid.CurrentItem as HttpRequestFile;
            
            if (httpRequestViewFile.Name != "New Http Request *")
            {
                if (
                    !fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath,
                                                                    httpRequestFile.RelativeFilePath)))
                {
                    httpRequestViewFile.Icon = "warning";
                }
                else
                {
                    httpRequestViewFile.Icon = "";
                }
            }

            httpRequestFilesViewModel.OpenHttpRequest(httpRequestViewFile);
        }

        private void CloneHttpRequest(string id)
        {
            var selectedItem = HttpRequestFilesDataGrid.CurrentItem as HttpRequestFile;
            if(selectedItem == null)
            {
                //TODO:selected item can be null, maybe should throw an error
                return;
            }
            var httpRequest =
                   fileService.Load<HttpRequestItemFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                     selectedItem.RelativeFilePath));
            var httpRequestControl = ServiceLocator.Current.GetInstance<HttpRequest>();
            httpRequestControl.FilePath = null;
            var viewModel = httpRequestControl.DataContext as HttpRequestViewModel;

            viewModel.RequestUrl = httpRequest.Url;
            viewModel.RequestVerb = viewModel.RequestVerbs.First(x => x.Content.ToString() == httpRequest.Verb);
            viewModel.RequestHeaders = httpRequest.Headers;
            viewModel.RequestBody = httpRequest.Body;

            var newHttpRequestDocument = new LayoutDocument
            {
                ContentId = id,
                Content = httpRequestControl,
                Title = "New Http Request *",
                IsSelected = true,
                CanFloat = true
            };

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(newHttpRequestDocument);
        }
    }
}
