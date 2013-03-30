using System.Collections.Generic;
using System.Windows;
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
        #region Declarations

        private IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public HttpRequestSequenceFilesViewModel(IFileService fileService, IEventAggregator eventAggregator,
                                         IMainMenuApplicationService mainMenuApplicationService)
            : base(
                fileService, eventAggregator, mainMenuApplicationService, null, "New Http Request Sequence *",
                () => Solution.Current.HttpRequestSequenceFiles, "Add Existing Http Request Sequence", "Rest Box Http Request Sequence (*.rseq)|*.rseq")
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.eventAggregator = eventAggregator;
            eventAggregator.GetEvent<UpdateHttpRequestSequenceFileItemEvent>().Subscribe(UpdateFileItem);
            eventAggregator.GetEvent<OpenHttpRequestEvent>().Subscribe(OpenItem);
            eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Subscribe(BindMenuItems);
        }

        private void BindMenuItems(bool obj)
        {
            var sequencesMenu = mainMenuApplicationService.Get("sequences");

            var addMenu = mainMenuApplicationService.GetChild(sequencesMenu, "sequencesAdd");
            var newMenu = mainMenuApplicationService.GetChild(addMenu, "sequencesNew");
            newMenu.IsEnabled = true;
            newMenu.Command = NewCommand;

            var existingMenu = mainMenuApplicationService.GetChild(addMenu, "sequencesExisting");
            existingMenu.IsEnabled = true;
            existingMenu.Command = AddCommand;
        }

        #endregion

        protected override void OnSelectedFileChange(File viewFile)
        {
           
        }

        protected override void DocumentChangedAdditionalHandler()
        {
            var sequencesMenu = mainMenuApplicationService.Get("sequences");
            var runMenu = mainMenuApplicationService.GetChild(sequencesMenu, "sequencesRun");
            runMenu.IsEnabled = false;
        }
    }
}
