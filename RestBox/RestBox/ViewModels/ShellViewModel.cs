using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Domain.Services;
using RestBox.Events;
using IFileService = RestBox.ApplicationServices.IFileService;

namespace RestBox.ViewModels
{
    public class ShellViewModel : ViewModelBase<ShellViewModel>
    {
        private readonly ILayoutApplicationService layoutApplicationService;
        private readonly IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;

        public ShellViewModel(
            ILayoutApplicationService layoutApplicationService, 
            IEventAggregator eventAggregator, 
            IMainMenuApplicationService mainMenuApplicationService, 
            IFileService fileService, IJsonSerializer jsonSerializer)
        {
            this.layoutApplicationService = layoutApplicationService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            MenuItems = new ObservableCollection<MenuItem>();
            HttpRequestFiles = new ObservableCollection<HttpRequestViewFile>();
            HttpRequestGroups = new ObservableCollection<string>();
            HttpRequestFilesView = new CollectionViewSource
                                       {
                                           Source = HttpRequestFiles
                                       }.View;
            HttpRequestFilesView.Filter = FilterHttpRequestFiles;

            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(NewSolutionSetUp);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(OpenSolution);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(CloseSolution);
            eventAggregator.GetEvent<ResetMenuEvent>().Subscribe(ResetMenu);
            HttpRequestFileNewOpenVisibility = Visibility.Collapsed;
            SolutionLoadedVisibility = Visibility.Visible;
        }

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged(x => x.SolutionLoadedVisibility); }
        }

        private void ResetMenu(bool obj)
        {
            MenuItems.Clear();
            mainMenuApplicationService.CreateInitialMenuItems(this);
        }

        private bool FilterHttpRequestFiles(object httpRequestFile)
        {
            if (string.IsNullOrWhiteSpace(HttpRequestFilesFilter))
            {
                return true;
            }
            return (((HttpRequestFile)httpRequestFile).Groups != null && ((HttpRequestFile)httpRequestFile).Groups.ToLower().Contains(HttpRequestFilesFilter.ToLower()))
                || ((HttpRequestFile)httpRequestFile).Name.ToLower().Contains(HttpRequestFilesFilter.ToLower());
        }

        private void OpenSolution(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Visible;
            CloseSolution(true);
            ApplicationTitle = string.Format("REST Box - {0}", Solution.Current.Name);
            HttpRequestFileNewOpenVisibility = Visibility.Visible;
            var fileMenu = MenuItems.First(x => x.Header.ToString() == "_File");
            mainMenuApplicationService.Get(fileMenu, "_Close Solution").IsEnabled = true;

            foreach (var httpRequest in Solution.Current.HttpRequestFiles)
            {
                var requestView = new HttpRequestViewFile
                                      {
                                          Id = httpRequest.Id,
                                          Name = httpRequest.Name,
                                          Groups = httpRequest.Groups,
                                          RelativeFilePath = httpRequest.RelativeFilePath
                                      };
                if(!fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, httpRequest.RelativeFilePath)))
                {
                    requestView.Icon = "warning";
                }

                HttpRequestFiles.Add(requestView);
            }
        }

        private void NewSolutionSetUp(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Visible;
            CloseSolution(true);
            ApplicationTitle = string.Format("REST Box - {0}", Solution.Current.Name);
            HttpRequestFileNewOpenVisibility = Visibility.Visible;
            var fileMenu = MenuItems.First(x => x.Header.ToString() == "_File");
            mainMenuApplicationService.Get(fileMenu, "_Close Solution").IsEnabled = true;
        }

        private void CloseSolution(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Hidden;
            MenuItems.Clear();
            mainMenuApplicationService.CreateInitialMenuItems(this);
            HttpRequestFiles.Clear();
            HttpRequestFileNewOpenVisibility = Visibility.Collapsed;
            var fileMenu = MenuItems.First(x => x.Header.ToString() == "_File");
            mainMenuApplicationService.Get(fileMenu, "_Close Solution").IsEnabled = false;
        }

        private string applicationTitle;
        public string ApplicationTitle
        {
            get { return applicationTitle; }
            set { applicationTitle = value; OnPropertyChanged(x => x.ApplicationTitle); }
        }

        public ObservableCollection<MenuItem> MenuItems { get; private set; }
        public ObservableCollection<HttpRequestViewFile> HttpRequestFiles { get; private set; }

        private string httpRequestFilesFilter;
        public string HttpRequestFilesFilter
        {
            get { return httpRequestFilesFilter; }
            set { httpRequestFilesFilter = value; OnPropertyChanged(x => x.HttpRequestFilesFilter); HttpRequestFilesView.Refresh(); }
        }

        public ICollectionView HttpRequestFilesView { get; set; }

        private HttpRequestFile selectedHttpRequestFile;

        public HttpRequestFile SelectedHttpRequestFile
        {
            get { return selectedHttpRequestFile; }
            set
            {
                selectedHttpRequestFile = value;
                OnPropertyChanged(x => x.SelectedHttpRequestFile);
            }
        }

        private Visibility httpRequestFileNewOpenVisibility;
        public Visibility HttpRequestFileNewOpenVisibility
        {
            get { return httpRequestFileNewOpenVisibility; }
            set { httpRequestFileNewOpenVisibility = value; OnPropertyChanged(x => x.HttpRequestFileNewOpenVisibility); }
        }

        public ICommand NewHttpRequestFile
        {
            get { return new DelegateCommand(NewHttpRequestItem); }
        }

        private void NewHttpRequestItem()
        {
            var id = Guid.NewGuid().ToString();
            var httpRequestFile = new HttpRequestViewFile
                                                  {
                                                      Id = id,
                                                      Name = "New Http Request *"
                                                  };
            HttpRequestFiles.Add(httpRequestFile);
            eventAggregator.GetEvent<NewHttpRequestEvent>().Publish(id);
        }

        public ICommand AddExistingHttpRequestFile
        {
            get{return new DelegateCommand(AddExistingHttpRequest);}
        }

        private void AddExistingHttpRequest()
        {
             var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Http Request (*.rhrq)|*.rhrq",
                                         Title = "Add Existing Http Request"
                                     };
             if (openFileDialog.ShowDialog() == true)
             {
                 var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                 var httpRequestFile = new HttpRequestViewFile
                                           {
                                               Id = Guid.NewGuid().ToString(),
                                               RelativeFilePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                                                openFileDialog.FileName),
                                               Name = fileName
                                           };

                 Solution.Current.HttpRequestFiles.Add(httpRequestFile);
                 HttpRequestFiles.Add(httpRequestFile);
                 fileService.SaveSolution();
             }
        }

        public ICommand DeleteHttpRequestFile
        {
            get { return new DelegateCommand(DeleteHttpRequestItem); }
        }

        public ICommand RemoveHttpRequestFile
        {
            get { return new DelegateCommand(RemoveHttpRequestItem); }
        }

        public ICommand CloneHttpRequestFile
        {
            get { return new DelegateCommand(CloneHttpRequestItem); }
        }

        private void CloneHttpRequestItem()
        {
            var id = Guid.NewGuid().ToString();
            var httpRequestFile = new HttpRequestViewFile
            {
                Id = id,
                Name = "New Http Request *"
            };
            HttpRequestFiles.Add(httpRequestFile);
            eventAggregator.GetEvent<CloneHttpRequestEvent>().Publish(id);
        }

        public ICommand RenameHttpRequestFile
        {
            get { return new DelegateCommand(RenameHttpRequestItem); }
        }

        private void RenameHttpRequestItem()
        {
            if(SelectedHttpRequestFile.Name == "New Http Request *")
            {
                return;
            }

            var selectedHttpRequestViewFile = SelectedHttpRequestFile as HttpRequestViewFile;

            selectedHttpRequestViewFile.NameVisibility = Visibility.Collapsed;
            selectedHttpRequestViewFile.EditableNameVisibility = Visibility.Visible;
        }

        private void RemoveHttpRequestItem()
        {
            if(SelectedHttpRequestFile == null)
            {
                return;
            }

            var id = SelectedHttpRequestFile.Id;
            if(SelectedHttpRequestFile.Name == "New Http Request *")
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = HttpRequestFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    HttpRequestFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentHttpRequestFile = Solution.Current.HttpRequestFiles.First(x => x.Id == id);
            Solution.Current.HttpRequestFiles.Remove(currentHttpRequestFile);
            var currentUiHttpRequestFile = HttpRequestFiles.First(x => x.Id == id);
            HttpRequestFiles.Remove(currentUiHttpRequestFile);
            selectedHttpRequestFile = null;
            fileService.SaveSolution();
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

        private void DeleteHttpRequestItem()
        {
            var id = SelectedHttpRequestFile.Id;
            if (SelectedHttpRequestFile.Name == "New Http Request *")
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = HttpRequestFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    HttpRequestFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentHttpRequestFile = Solution.Current.HttpRequestFiles.First(x => x.Id == id);
            Solution.Current.HttpRequestFiles.Remove(currentHttpRequestFile);
            var currentUiHttpRequestFile = HttpRequestFiles.First(x => x.Id == id);
            HttpRequestFiles.Remove(currentUiHttpRequestFile);
            selectedHttpRequestFile = null;
            fileService.SaveSolution();
            var httpRequestFilePath = fileService.GetFilePath(Solution.Current.FilePath, currentUiHttpRequestFile.RelativeFilePath);
            fileService.DeleteFile(httpRequestFilePath);
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

       public ObservableCollection<string> HttpRequestGroups { get; set; } 

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand(ExitApplication);
            }
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
