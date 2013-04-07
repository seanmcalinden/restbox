using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public class HttpInterceptorFilesViewModel : FileListViewModelBase<
        HttpInterceptorFilesViewModel, 
        HttpInterceptor, 
        HttpInterceptorViewModel,
        HttpRequestItemFile,
        SelectHttpInterceptorItemEvent,
        AddHttpInterceptorMenuItemsEvent>
    {
        #region Declarations

        private IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public HttpInterceptorFilesViewModel(IFileService fileService, IEventAggregator eventAggregator,
                                         IMainMenuApplicationService mainMenuApplicationService,
                                         IMapper<HttpRequestItemFile, HttpInterceptorViewModel> itemToViewModelMapper)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, itemToViewModelMapper, "New Http Interceptor *",
                () => Solution.Current.HttpInterceptorFiles, SystemFileTypes.Interceptor.AddExistingTitle, SystemFileTypes.Interceptor.FilterText)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.eventAggregator = eventAggregator;
            eventAggregator.GetEvent<UpdateHttpInterceptorFileItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
        }

        private void BindMenuItems(bool obj)
        {
            var requestsMenu = mainMenuApplicationService.Get("interceptors");

            var addMenu = mainMenuApplicationService.GetChild(requestsMenu, "interceptorsAdd");
            var newMenu = mainMenuApplicationService.GetChild(addMenu, "interceptorsNew");
            newMenu.IsEnabled = true;
            newMenu.Command = NewCommand;

            var existingMenu = mainMenuApplicationService.GetChild(addMenu, "interceptorsExisting");
            existingMenu.IsEnabled = true;
            existingMenu.Command = AddCommand;
        }

        #endregion

        protected override void OnSelectedFileChange(File viewFile)
        {
           
        }

        protected override void DocumentChangedAdditionalHandler()
        {
            var requestsMenu = mainMenuApplicationService.Get("interceptors");
            if (requestsMenu == null)
            {
                return;
            }
            var runMenu = mainMenuApplicationService.GetChild(requestsMenu, "interceptorsStart");
            runMenu.IsEnabled = false;
        }
    }
}