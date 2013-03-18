using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public class RequestExtensionsViewModel : ViewModelBase<RequestEnvironmentsViewModel>
    {
        private readonly IFileService fileService;
        private readonly IIntellisenseService intellisenseService;

        public RequestExtensionsViewModel(IEventAggregator eventAggregator, IFileService fileService, IIntellisenseService intellisenseService)
        {
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            RequestExtensionFiles = new ObservableCollection<RequestExtensionViewFile>();
            SolutionLoadedVisibility = Visibility.Hidden;
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);

        }

        private void SolutionLoadedEvent(bool obj)
        {
            RequestExtensionFiles.Clear();
            foreach(var requestExtensionFile in Solution.Current.RequestExtensionsFilePaths)
            {
                var viewFile = new RequestExtensionViewFile
                                   {
                                       Name = Path.GetFileNameWithoutExtension(requestExtensionFile),
                                       RelativeFilePath = requestExtensionFile
                                   };
                RequestExtensionFiles.Add(viewFile);

                intellisenseService.AddRequestExtensionIntellisenseItem(viewFile.Name);
            }

            if (RequestExtensionFiles.Count > 0)
            {
                SelectedRequestExtension = RequestExtensionFiles[0];
            }

            SolutionLoadedVisibility = Visibility.Visible;
        }

        private void SolutionClosedEvent(bool obj)
        {
            RequestExtensionFiles.Clear();
            SolutionLoadedVisibility = Visibility.Hidden; 
        }

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged(x => x.SolutionLoadedVisibility); }
        }

        public ObservableCollection<RequestExtensionViewFile> RequestExtensionFiles { get; set; }

        private RequestExtensionViewFile selectedRequestExtension;
        public RequestExtensionViewFile SelectedRequestExtension
        {
            get { return selectedRequestExtension; }
            set { selectedRequestExtension = value; OnPropertyChanged(x => x.SelectedRequestEnvironment); }
        }

        private RequestExtensionViewFile selectedRequestExtensionFile;
        public RequestExtensionViewFile SelectedRequestExtensionFile
        {
            get { return selectedRequestExtensionFile; }
            set { selectedRequestExtensionFile = value; OnPropertyChanged(x => x.SelectedRequestEnvironmentFile); }
        }

        public ICommand AddRequestExtensionCommand
        {
            get { return new DelegateCommand(AddRequestExtension); }
        }

        public ICommand RemoveRequestExtensionCommand
        {
            get{return new DelegateCommand(RemoveRequestExtension);}
        }

        private void RemoveRequestExtension()
        {
            if (SelectedRequestExtensionFile == null)
            {
                return;
            }

            var currentRequestExtensionFile = Solution.Current.RequestExtensionsFilePaths.First(x => x == SelectedRequestExtensionFile.RelativeFilePath);
            Solution.Current.RequestExtensionsFilePaths.Remove(currentRequestExtensionFile);
            var currentUiHttpRequestFile = RequestExtensionFiles.First(x => x.RelativeFilePath == SelectedRequestExtensionFile.RelativeFilePath);
            RequestExtensionFiles.Remove(currentUiHttpRequestFile);
            SelectedRequestExtensionFile = null;
            fileService.SaveSolution();

            intellisenseService.RemoveRequestExtensionIntellisenseItem(currentUiHttpRequestFile.Name);
        }

        private void AddRequestExtension()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable (*.exe)|*.exe",
                Title = "Add request extension"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                var relativeFile = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                               openFileDialog.FileName);
                var requestEnvironmentViewFile = new RequestExtensionViewFile
                {
                    RelativeFilePath = relativeFile,
                    Name = fileName
                };

                Solution.Current.RequestExtensionsFilePaths.Add(relativeFile);
                RequestExtensionFiles.Add(requestEnvironmentViewFile);
                fileService.SaveSolution();
                intellisenseService.AddRequestExtensionIntellisenseItem(requestEnvironmentViewFile.Name);
            }
        }
    }
}
