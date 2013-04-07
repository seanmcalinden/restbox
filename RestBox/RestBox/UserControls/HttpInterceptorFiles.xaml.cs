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
    /// Interaction logic for HttpRequestSequenceFiles.xaml
    /// </summary>
    public partial class HttpInterceptorFiles
    {
        private readonly HttpInterceptorFilesViewModel httpInterceptorFilesViewModel;
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;

        public HttpInterceptorFiles(HttpInterceptorFilesViewModel httpInterceptorFilesViewModel, IFileService fileService, IEventAggregator eventAggregator)
        {
            this.httpInterceptorFilesViewModel = httpInterceptorFilesViewModel;
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            DataContext = httpInterceptorFilesViewModel;
            InitializeComponent();
            eventAggregator.GetEvent<SelectHttpInterceptorItemEvent>().Subscribe(SelectHttpInterceptorSequenceFileItem);
        }

        private void SelectHttpInterceptorSequenceFileItem(string id)
        {
            for (var i = 0; i < HttpInterceptorFilesDataGrid.Items.Count; i++)
            {
                if (((ViewFile)HttpInterceptorFilesDataGrid.Items[i]).Id == id)
                {
                    HttpInterceptorFilesDataGrid.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateGroupsEvent(object sender, TextChangedEventArgs e)
        {
            httpInterceptorFilesViewModel.UpdateGroups();
        }

        private void Rename(object sender, RoutedEventArgs e)
        {
            var selectedItem = HttpInterceptorFilesDataGrid.SelectedItem as ViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for (var i = 0; i < relativePathParts.Length; i++)
            {
                if (i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + "." + SystemFileTypes.Interceptor.Extension);
                    break;
                }
                sb.Append(relativePathParts[i] + "/");
            }

            var newRelativePath = sb.ToString();

            var destinationFilePath = fileService.GetFilePath(Solution.Current.FilePath, newRelativePath);

            fileService.MoveFile(sourceFilePath, destinationFilePath);

            selectedItem.RelativeFilePath = newRelativePath;

            var solutionItem = Solution.Current.HttpInterceptorFiles.First(x => x.Id == selectedItem.Id);
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
            httpInterceptorFilesViewModel.ActivateItem();
        } 

    }
}
