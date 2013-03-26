using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.UserControls;

namespace RestBox.ViewModels
{
    public class HttpRequestSequenceFilesViewModel: FileListViewModelBase<
        HttpRequestSequenceFilesViewModel, 
        HttpRequestSequence, 
        HttpRequestSequenceViewModel, 
        bool,
        SelectHttpRequestSequenceItemEvent,
        AddHttpRequestSequenceMenuItemsEvent>
    {
        #region Constructor

        public HttpRequestSequenceFilesViewModel(IFileService fileService, IEventAggregator eventAggregator,
                                         IMainMenuApplicationService mainMenuApplicationService)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, null, "New Http Request Sequence *",
                () => Solution.Current.HttpRequestSequenceFiles, "Add Existing Http Request Sequence", "Rest Box Http Request Sequence (*.rseq)|*.rseq")
        {
            eventAggregator.GetEvent<UpdateHttpRequestSequenceFileItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<OpenHttpRequestEvent>().Subscribe(OpenItem);
        }

        #endregion

    }
}
