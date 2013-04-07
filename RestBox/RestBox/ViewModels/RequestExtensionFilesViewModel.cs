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
using RestBox.Utilities;
using RestBox.ViewModels.NoLayoutFiles;

namespace RestBox.ViewModels
{
    public class RequestExtensionFilesViewModel : FileListViewModelBase<
        RequestExtensionFilesViewModel,
        NoLayoutUserControl,
        NoLayoutViewModel,
        NoSettingFile,
        NoItemEvent,
        NoMenuItemsEvent>
    {


         #region Declarations

        private readonly IIntellisenseService intellisenseService;
        private readonly IFileService fileService;
        private IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public RequestExtensionFilesViewModel(IEventAggregator eventAggregator, IFileService fileService, IMainMenuApplicationService mainMenuApplicationService, IIntellisenseService intellisenseService)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, null, null,
                () => Solution.Current.RequestExtensionsFilePaths, SystemFileTypes.RequestExtension.AddExistingTitle, SystemFileTypes.RequestExtension.FilterText)
        {
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
            RequestExtensionFiles = new ObservableCollection<ViewFile>();
        }

        private void BindMenuItems(bool obj)
        {
            var extension = mainMenuApplicationService.Get("extensions");

            var addExtensionMenu = mainMenuApplicationService.GetChild(extension, "addExtension");
            addExtensionMenu.IsEnabled = true;
            addExtensionMenu.Command = AddRequestExtensionCommand;
        }

        #endregion

        private ViewFile selected;
        public ViewFile Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("Selected"); eventAggregator.GetEvent<UpdateEnvironmentEvent>().Publish(true);}
        }

        protected override void OnSelectedFileChange(File viewFile)
        {
            //throw new System.NotImplementedException();
        }

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
                Selected = RequestExtensionFiles[0];
            }

            base.SolutionLoadedEvent(true);
        }

        protected override void DocumentChangedAdditionalHandler()
        {

        }

        public ObservableCollection<ViewFile> RequestExtensionFiles { get; set; }

        public ICommand AddRequestExtensionCommand
        {
            get { return new DelegateCommand(AddRequestExtension); }
        }

        public ICommand RemoveRequestExtensionCommand
        {
            get { return new DelegateCommand(RemoveRequestExtension); }
        } 

        public override void OpenFolder()
        {
            fileService.OpenFileInWindowsExplorer(Selected.RelativeFilePath);
        }
        
        private void RemoveRequestExtension()
        {
            if (Selected == null)
            {
                return;
            }

            var currentRequestExtensionFile = Solution.Current.RequestExtensionsFilePaths.First(x => x == Selected.RelativeFilePath);
            Solution.Current.RequestExtensionsFilePaths.Remove(currentRequestExtensionFile);
            var currentUiHttpRequestFile = RequestExtensionFiles.First(x => x.RelativeFilePath == Selected.RelativeFilePath);
            RequestExtensionFiles.Remove(currentUiHttpRequestFile);
            Selected = null;
            fileService.SaveSolution();

            intellisenseService.RemoveRequestExtensionIntellisenseItem(currentUiHttpRequestFile.Name);
        }

        private void AddRequestExtension()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = SystemFileTypes.RequestExtension.FilterText,
                Title = SystemFileTypes.RequestExtension.AddExistingTitle
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
        
        protected override void SolutionClosedEvent(bool obj)
        {
            RequestExtensionFiles.Clear();
            base.SolutionClosedEvent(obj);
        }
    }
}
