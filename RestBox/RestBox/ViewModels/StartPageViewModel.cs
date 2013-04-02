using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class StartPageViewModel : ViewModelBase<StartPageViewModel>
    {
        private readonly IRestBoxStateService restBoxStateService;
        private readonly IFileService fileService;

        public StartPageViewModel(IRestBoxStateService restBoxStateService, IFileService fileService)
        {
            this.restBoxStateService = restBoxStateService;
            this.fileService = fileService;
            RestBoxStateFiles = new ObservableCollection<RestBoxStateFile>(restBoxStateService.GetState().RestBoxStateFiles);
        }

        public ObservableCollection<RestBoxStateFile> RestBoxStateFiles { get; set; } 

        public ICommand RemoveRecentActivityCommand
        {
            get{ return new DelegateCommand<RestBoxStateFile>(RemoveRecentActivity);}
        }

        public ICommand OpenFolderInWindowsExplorerCommand
        {
            get { return new DelegateCommand<RestBoxStateFile>(OpenFolderInWindowsExplorer); }
        }

        private void OpenFolderInWindowsExplorer(RestBoxStateFile restBoxStateFile)
        {
            if (!fileService.FileExists(restBoxStateFile.FilePath))
            {
                return;
            }
            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", restBoxStateFile.FilePath));
        }

        private void RemoveRecentActivity(RestBoxStateFile restBoxStateFile)
        {
            restBoxStateService.RemoveRestBoxStateFile(restBoxStateFile);
            RestBoxStateFiles.Remove(restBoxStateFile);
        }
    }
}
