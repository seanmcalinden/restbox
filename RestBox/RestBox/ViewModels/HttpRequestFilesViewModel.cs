using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;

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
        #region Constructor

        public HttpRequestFilesViewModel(IFileService fileService, IEventAggregator eventAggregator,
                                         IMainMenuApplicationService mainMenuApplicationService,
                                         IMapper<HttpRequestItemFile, HttpRequestViewModel> itemToViewModelMapper)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, itemToViewModelMapper, "New Http Request *",
                () => Solution.Current.HttpRequestFiles, "Add Existing Http Request", "Rest Box Http Request (*.rhrq)|*.rhrq")
        {
            eventAggregator.GetEvent<UpdateHttpRequestFileItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<OpenHttpRequestEvent>().Subscribe(OpenItem);
        }

        #endregion

    }
}