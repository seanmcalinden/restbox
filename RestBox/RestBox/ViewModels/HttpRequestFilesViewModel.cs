using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;
using RestBox.UserControls;

namespace RestBox.ViewModels
{
    public class HttpRequestFilesViewModel : ViewModelBase<HttpRequestFilesViewModel>
    {
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;

        public HttpRequestFilesViewModel(IFileService fileService, IEventAggregator eventAggregator, IMainMenuApplicationService mainMenuApplicationService)
        {
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            HttpRequestFiles = new ObservableCollection<HttpRequestViewFile>();
            HttpRequestGroups = new ObservableCollection<string>();
            HttpRequestFilesView = new CollectionViewSource
            {
                Source = HttpRequestFiles
            }.View;
            HttpRequestFilesView.Filter = FilterHttpRequestFiles;
            SolutionLoadedVisibility = Visibility.Hidden;
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);
            eventAggregator.GetEvent<NewHttpRequestEvent>().Subscribe(NewHttpRequest);
            eventAggregator.GetEvent<UpdateHttpRequestFileItemEvent>().Subscribe(UpdateHttpRequestFileItem);
            eventAggregator.GetEvent<OpenHttpRequestEvent>().Subscribe(OpenHttpRequest);
            eventAggregator.GetEvent<DocumentChangedEvent>().Subscribe(DocumentChanged);
        }

        private void DocumentChanged(LayoutData layoutData)
        {
            if (layoutData != null && layoutData.Content is HttpRequest)
            {
                if(layoutData.IsSelected)
                {
                    eventAggregator.GetEvent<SelectHttpRequestItemEvent>().Publish(layoutData.ContentId);
                    eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Publish((HttpRequestViewModel)((HttpRequest)layoutData.Content).DataContext);
                }
            }
        }

        private void UpdateHttpRequestFileItem(HttpRequestFile httpRequestFile)
        {
            var item = HttpRequestFiles.FirstOrDefault(x => x.Id == httpRequestFile.Id);
            if(item == null)
            {
                return;
            }

            item.Name = httpRequestFile.Name;
            item.RelativeFilePath = httpRequestFile.RelativeFilePath;
        }

        private void SolutionLoadedEvent(bool obj)
        {
            HttpRequestFiles.Clear();

            foreach (var httpRequest in Solution.Current.HttpRequestFiles)
            {
                var requestView = new HttpRequestViewFile
                {
                    Id = httpRequest.Id,
                    Name = httpRequest.Name,
                    Groups = httpRequest.Groups,
                    RelativeFilePath = httpRequest.RelativeFilePath
                };
                if (!fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, httpRequest.RelativeFilePath)))
                {
                    requestView.Icon = "warning";
                }

                HttpRequestFiles.Add(requestView);
                SolutionLoadedVisibility = Visibility.Visible;
            }
        }

        private void NewHttpRequest(string id)
        {
            var newHttpRequestDocument = new LayoutDocument
            {
                ContentId = id,
                Content = ServiceLocator.Current.GetInstance<HttpRequest>(),
                Title = "New Http Request *",
                IsSelected = true,
                CanFloat = true
            };

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(newHttpRequestDocument);
            eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Publish((HttpRequestViewModel)((HttpRequest)newHttpRequestDocument.Content).DataContext);
            newHttpRequestDocument.Closing += RequestDocumentOnClosing;
        }

        private HttpRequestViewFile httpRequestViewFileToOpen;

        public void OpenHttpRequest(HttpRequestViewFile httpRequestViewFile)
        {
            if (httpRequestViewFile.Icon == "warning")
            {
                return;
            }

            httpRequestViewFileToOpen = httpRequestViewFile;

            if(httpRequestViewFileToOpen.Name == "New Http Request *")
            {
                var layoutDocument = new LayoutDocument
                {
                    Title = Path.GetFileNameWithoutExtension("New Http Request *"),
                    ContentId = httpRequestViewFileToOpen.Id,
                    Content = ServiceLocator.Current.GetInstance<HttpRequest>(),
                    IsSelected = true,
                    CanFloat = true
                };
                layoutDocument.Closing += RequestDocumentOnClosing;

                eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
                eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Publish((HttpRequestViewModel)((HttpRequest)layoutDocument.Content).DataContext);
            }
            else
            {
                OpenHttpRequest(fileService.GetFilePath(Solution.Current.FilePath, httpRequestViewFile.RelativeFilePath));    
            }
        }

        private void OpenHttpRequest(string fileName)
        {
            var httpRequest = fileService.Load<HttpRequestItemFile>(fileName);
            var httpRequestControl = ServiceLocator.Current.GetInstance<HttpRequest>();
            httpRequestControl.FilePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath), fileName);
            var viewModel = httpRequestControl.DataContext as HttpRequestViewModel;

            viewModel.RequestUrl = httpRequest.Url;
            viewModel.RequestVerb = viewModel.RequestVerbs.First(x => x.Content.ToString() == httpRequest.Verb);
            viewModel.RequestHeaders = httpRequest.Headers;
            viewModel.RequestBody = httpRequest.Body;

            var layoutDocument = new LayoutDocument
            {
                Title = Path.GetFileNameWithoutExtension(fileName),
                ContentId = httpRequestViewFileToOpen.Id,
                Content = httpRequestControl,
                IsSelected = true,
                CanFloat = true
            };
            layoutDocument.Closing += RequestDocumentOnClosing;

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Publish((HttpRequestViewModel)((HttpRequest)layoutDocument.Content).DataContext);
        }
        
        private void RequestDocumentOnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var layoutDocument = ((LayoutDocument)sender);
            if (layoutDocument.Title == "New Http Request *" && !layoutDocument.ContentId.StartsWith("StandaloneHttpRequest"))
            {
                var httpRequestFile = HttpRequestFiles.First(x => x.Id == layoutDocument.ContentId);
                HttpRequestFiles.Remove(httpRequestFile);
            }
            mainMenuApplicationService.CreateInitialMenuItems();
        }

        private void SolutionClosedEvent(bool obj)
        {
            HttpRequestFiles.Clear();
            SolutionLoadedVisibility = Visibility.Hidden;
        }

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged(x => x.SolutionLoadedVisibility); }
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

        private string httpRequestFilesFilter;
        public string HttpRequestFilesFilter
        {
            get { return httpRequestFilesFilter; }
            set { httpRequestFilesFilter = value; OnPropertyChanged(x => x.HttpRequestFilesFilter); HttpRequestFilesView.Refresh(); }
        }

        public ObservableCollection<HttpRequestViewFile> HttpRequestFiles { get; set; }

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
            get { return new DelegateCommand(AddExistingHttpRequest); }
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
            if (SelectedHttpRequestFile.Name == "New Http Request *")
            {
                return;
            }

            var selectedHttpRequestViewFile = SelectedHttpRequestFile as HttpRequestViewFile;

            selectedHttpRequestViewFile.NameVisibility = Visibility.Collapsed;
            selectedHttpRequestViewFile.EditableNameVisibility = Visibility.Visible;
        }

        private void RemoveHttpRequestItem()
        {
            if (SelectedHttpRequestFile == null)
            {
                return;
            }

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


    }
}
