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
    /// Interaction logic for RequestEnvironments.xaml
    /// </summary>
    public partial class RequestEnvironments
    {
        #region Declarations
        
        private readonly RequestEnvironmentsFilesViewModel requestEnvironmentsFilesViewModel;
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator; 

        #endregion

        #region Constructor
     
        public RequestEnvironments(IEventAggregator eventAggregator, RequestEnvironmentsFilesViewModel requestEnvironmentsFilesViewModel, IFileService fileService)
        {
            this.requestEnvironmentsFilesViewModel = requestEnvironmentsFilesViewModel;
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            DataContext = requestEnvironmentsFilesViewModel;
            InitializeComponent();
            eventAggregator.GetEvent<SelectEnvironmentItemEvent>().Subscribe(SelectEnvironmentItem);
        } 

        #endregion

        #region Helpers

        private void SelectEnvironmentItem(string id)
        {
            for (var i = 0; i < EnvironmentsDataGrid.Items.Count; i++)
            {
                if (((ViewFile)EnvironmentsDataGrid.Items[i]).Id == id)
                {
                    EnvironmentsDataGrid.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateGroupsEvent(object sender, TextChangedEventArgs e)
        {
            requestEnvironmentsFilesViewModel.UpdateGroups();
        }

        private void ActivateItem(object sender, MouseButtonEventArgs e)
        {
            requestEnvironmentsFilesViewModel.ActivateItem();
        }

        private void Rename(object sender, RoutedEventArgs e)
        {
            var selectedItem = EnvironmentsDataGrid.SelectedItem as ViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for (var i = 0; i < relativePathParts.Length; i++)
            {
                if (i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + "." + SystemFileTypes.Environment.Extension);
                    break;
                }
                sb.Append(relativePathParts[i] + "/");
            }

            var newRelativePath = sb.ToString();

            var destinationFilePath = fileService.GetFilePath(Solution.Current.FilePath, newRelativePath);

            fileService.MoveFile(sourceFilePath, destinationFilePath);

            selectedItem.RelativeFilePath = newRelativePath;

            var solutionItem = Solution.Current.RequestEnvironmentFiles.First(x => x.Id == selectedItem.Id);
            solutionItem.Name = selectedItem.Name;
            solutionItem.RelativeFilePath = newRelativePath;

            fileService.SaveSolution();

            eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
            {
                Id = selectedItem.Id,
                Title = selectedItem.Name
            });
           requestEnvironmentsFilesViewModel.Rename();
        } 

        #endregion
    }
}
