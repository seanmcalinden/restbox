using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;
using RestBox.Utilities;

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
        private IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public RequestEnvironmentsFilesViewModel(IEventAggregator eventAggregator, IFileService fileService, IMainMenuApplicationService mainMenuApplicationService, IMapper<RequestEnvironmentSettingFile, RequestEnvironmentSettingsViewModel> itemToViewModelMapper, IIntellisenseService intellisenseService)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, itemToViewModelMapper, "New Environment *",
                () => Solution.Current.RequestEnvironmentFiles, SystemFileTypes.Environment.AddExistingTitle, SystemFileTypes.Environment.FilterText)
        {
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
        }

        private void BindMenuItems(bool obj)
        {
            var environments = mainMenuApplicationService.Get("environments");

            var addMenu = mainMenuApplicationService.GetChild(environments, "environmentsAdd");
            var newMenu = mainMenuApplicationService.GetChild(addMenu, "environmentsNew");
            newMenu.IsEnabled = true;
            newMenu.Command = NewCommand;

            var existingMenu = mainMenuApplicationService.GetChild(addMenu, "environmentsExisting");
            existingMenu.IsEnabled = true;
            existingMenu.Command = AddCommand;
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

        protected override void DocumentChangedAdditionalHandler()
        {
           
        }
    }
}
