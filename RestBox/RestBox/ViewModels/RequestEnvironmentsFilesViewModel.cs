using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentsFilesViewModel : FileListViewModelBase<
        RequestEnvironmentsFilesViewModel, 
        RequestEnvironmentSettings, 
        RequestEnvironmentSettingsViewModel, 
        RequestEnvironmentSettingFile, 
        SelectEnvironmentItemEvent, 
        AddRequestEnvironmentMenuItemsEvent>
    {

        #region Declarations

        private readonly IIntellisenseService intellisenseService;
        private readonly IFileService fileService;

        #endregion

        #region Constructor

        public RequestEnvironmentsFilesViewModel(IEventAggregator eventAggregator, IFileService fileService, IMainMenuApplicationService mainMenuApplicationService, IMapper<RequestEnvironmentSettingFile, RequestEnvironmentSettingsViewModel> itemToViewModelMapper, IIntellisenseService intellisenseService)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, itemToViewModelMapper, "New Environment *",
                () => Solution.Current.RequestEnvironmentFiles, "Add existing environment", "Rest Box Environment (*.renv)|*.renv")
        {
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Subscribe(UpdateFileItem);

        } 

        #endregion

        private ViewFile selected;
        public ViewFile Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("Selected"); }
        }

        protected override void SolutionLoadedEvent(bool obj)
        {
            base.SolutionLoadedEvent(obj);

            foreach (var requestEnvironmentFile in Solution.Current.RequestEnvironmentFiles)
            {
                var requestEnvironmentSetting = fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath, requestEnvironmentFile.RelativeFilePath));
                foreach (var environmentSetting in requestEnvironmentSetting.RequestEnvironmentSettings)
                {
                    intellisenseService.AddEnvironmentIntellisenseItem(environmentSetting.Setting, environmentSetting.SettingValue);
                }
            }
        }
    }
}
