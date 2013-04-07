using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class HttpRequestSequenceFiles
    {
        private readonly HttpRequestSequenceFilesViewModel httpRequestSequenceFilesViewModel;
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;

        public HttpRequestSequenceFiles(HttpRequestSequenceFilesViewModel httpRequestSequenceFilesViewModel, IFileService fileService, IEventAggregator eventAggregator)
        {
            this.httpRequestSequenceFilesViewModel = httpRequestSequenceFilesViewModel;
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            DataContext = httpRequestSequenceFilesViewModel;
            InitializeComponent();
            eventAggregator.GetEvent<SelectHttpRequestSequenceItemEvent>().Subscribe(SelectHttpRequestSequenceFileItem);
        }

        private void SelectHttpRequestSequenceFileItem(string id)
        {
            for (var i = 0; i < HttpRequestSequenceFilesDataGrid.Items.Count; i++)
            {
                if (((ViewFile)HttpRequestSequenceFilesDataGrid.Items[i]).Id == id)
                {
                    HttpRequestSequenceFilesDataGrid.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateGroupsEvent(object sender, TextChangedEventArgs e)
        {
            httpRequestSequenceFilesViewModel.UpdateGroups();
        }

        private void Rename(object sender, RoutedEventArgs e)
        {
            var selectedItem = HttpRequestSequenceFilesDataGrid.SelectedItem as ViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for (var i = 0; i < relativePathParts.Length; i++)
            {
                if (i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + "." + SystemFileTypes.HttpSequence.Extension);
                    break;
                }
                sb.Append(relativePathParts[i] + "/");
            }

            var newRelativePath = sb.ToString();

            var destinationFilePath = fileService.GetFilePath(Solution.Current.FilePath, newRelativePath);

            fileService.MoveFile(sourceFilePath, destinationFilePath);

            selectedItem.RelativeFilePath = newRelativePath;

            var solutionItem = Solution.Current.HttpRequestSequenceFiles.First(x => x.Id == selectedItem.Id);
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
            eventAggregator.GetEvent<UpdateStatusBarEvent>().Publish(new StatusBarData
                {
                    StatusBarText = "Loading Sequence..."
                });
            httpRequestSequenceFilesViewModel.ActivateItem();
            eventAggregator.GetEvent<UpdateStatusBarEvent>().Publish(new StatusBarData
            {
                StatusBarText = "Ready"
            });
        } 

    }
}
