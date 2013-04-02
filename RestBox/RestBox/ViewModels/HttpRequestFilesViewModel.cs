using System.Collections.Generic;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public class HttpRequestFilesViewModel : FileListViewModelBase<
        HttpRequestFilesViewModel, 
        HttpRequest, 
        HttpRequestViewModel, 
        HttpRequestItemFile, 
        SelectHttpRequestItemEvent, 
        AddHttpRequestMenuItemsEvent>
    {
        #region Declarations

        private IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public HttpRequestFilesViewModel(IFileService fileService, IEventAggregator eventAggregator,
                                         IMainMenuApplicationService mainMenuApplicationService,
                                         IMapper<HttpRequestItemFile, HttpRequestViewModel> itemToViewModelMapper)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, itemToViewModelMapper, "New Http Request *",
                () => Solution.Current.HttpRequestFiles, SystemFileTypes.HttpRequest.AddExistingTitle, SystemFileTypes.HttpRequest.FilterText)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.eventAggregator = eventAggregator;
            eventAggregator.GetEvent<UpdateHttpRequestFileItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<OpenHttpRequestEvent>().Subscribe(OpenItem);
            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
        }

        private void BindMenuItems(bool obj)
        {
            var requestsMenu = mainMenuApplicationService.Get("requests");

            var addMenu = mainMenuApplicationService.GetChild(requestsMenu, "requestsAdd");
            var newMenu = mainMenuApplicationService.GetChild(addMenu, "requestsNew");
            newMenu.IsEnabled = true;
            newMenu.Command = NewCommand;

            var existingMenu = mainMenuApplicationService.GetChild(addMenu, "requestsExisting");
            existingMenu.IsEnabled = true;
            existingMenu.Command = AddCommand;
        }

        #endregion

        protected override void OnSelectedFileChange(File viewFile)
        {
           
        }

        protected override void DocumentChangedAdditionalHandler()
        {
            var requestsMenu = mainMenuApplicationService.Get("requests");
            if (requestsMenu == null)
            {
                return;
            }
            var runMenu = mainMenuApplicationService.GetChild(requestsMenu, "requestsRun");
            runMenu.IsEnabled = false;
        }
    }
}