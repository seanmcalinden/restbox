using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public class RequestExtensionFilesViewModel : FileListViewModel<RequestExtensionFilesViewModel>
    {
        #region Declarations

        private readonly IFileService fileService;
        private readonly IIntellisenseService intellisenseService;
        private readonly IMainMenuApplicationService mainMenuApplicationService;

        #endregion

        #region Constructor
        
        public RequestExtensionFilesViewModel(IEventAggregator eventAggregator, IFileService fileService, IIntellisenseService intellisenseService, IMainMenuApplicationService mainMenuApplicationService)
            : base(eventAggregator)
        {
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            this.mainMenuApplicationService = mainMenuApplicationService;
            RequestExtensionFiles = new ObservableCollection<ViewFile>();

            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
        }

        private void BindMenuItems(bool obj)
        {
            var extension = mainMenuApplicationService.Get("extensions");

            var addExtensionMenu = mainMenuApplicationService.GetChild(extension, "addExtension");
            addExtensionMenu.IsEnabled = true;
            addExtensionMenu.Command = AddRequestExtensionCommand;
        }

        #endregion

        #region Properties

        public ObservableCollection<ViewFile> RequestExtensionFiles { get; set; }

        private ViewFile selectedRequestExtension;
        public ViewFile SelectedRequestExtension
        {
            get { return selectedRequestExtension; }
            set { selectedRequestExtension = value; OnPropertyChanged(x => x.SelectedRequestExtension); }
        }

        private ViewFile selectedRequestExtensionFile;
        public ViewFile SelectedRequestExtensionFile
        {
            get { return selectedRequestExtensionFile; }
            set { selectedRequestExtensionFile = value; OnPropertyChanged(x => x.SelectedRequestExtensionFile); }
        } 

        #endregion

        #region Commands

        public ICommand OpenFolderInWindowsExplorer
        {
            get { return new DelegateCommand(OpenFolder); }
        }

        public ICommand AddRequestExtensionCommand
        {
            get { return new DelegateCommand(AddRequestExtension); }
        }

        public ICommand RemoveRequestExtensionCommand
        {
            get { return new DelegateCommand(RemoveRequestExtension); }
        } 

        #endregion

        #region Command Handlers

        private void OpenFolder()
        {
            fileService.OpenFileInWindowsExplorer(SelectedRequestExtensionFile.RelativeFilePath);
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
                var requestEnvironmentViewFile = new ViewFile
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

        #endregion

        #region Event Handlers

        protected override void SolutionLoadedEvent(bool obj)
        {
            RequestExtensionFiles.Clear();
            foreach (var requestExtensionFile in Solution.Current.RequestExtensionsFilePaths)
            {
                var viewFile = new ViewFile
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

            base.SolutionLoadedEvent(true);
        }

        protected override void SolutionClosedEvent(bool obj)
        {
            RequestExtensionFiles.Clear();
            base.SolutionClosedEvent(obj);
        }

        #endregion
    }
}
