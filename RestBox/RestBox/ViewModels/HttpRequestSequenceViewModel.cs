using System;
using System.Activities;
using System.Activities.Presentation;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;
using RestBox.UserControls;

namespace RestBox.ViewModels
{
    public class HttpRequestSequenceViewModel : ViewModelBase<HttpRequestSequenceViewModel>
    {

        #region Declarations

        private IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IFileService fileService;
        private readonly KeyGesture saveKeyGesture;
        private readonly KeyGesture runHttpRequestKeyGesture;
        private WorkflowApplication workflowApplication;

        #endregion

        #region Constructor

        public HttpRequestSequenceViewModel(IEventAggregator eventAggregator, IMainMenuApplicationService mainMenuApplicationService, IFileService fileService)
        {
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.fileService = fileService;
            saveKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            runHttpRequestKeyGesture = new KeyGesture(Key.F5);
            eventAggregator.GetEvent<AddHttpRequestSequenceMenuItemsEvent>().Subscribe(AddHttpRequestSequenceMenuItems);
            Responses = new ObservableCollection<HttpResponseItem>();
            ProgressBarVisibility = Visibility.Hidden;
            CancelButtonVisibility = Visibility.Hidden;
            StartButtonVisibility = Visibility.Visible;
        }

        #endregion

        #region Event Handlers

        private void AddHttpRequestSequenceMenuItems(HttpRequestSequenceViewModel httpRequestSequenceViewModel)
        {
            if (httpRequestSequenceViewModel != this)
            {
                return;
            }

            eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);

            mainMenuApplicationService.CreateInitialMenuItems();
            var fileMenu = mainMenuApplicationService.Get(null, "_File");
            var saveHttpRequest = new MenuItem { Header = "Save Http Request Sequence", InputGestureText = "Ctrl+S" };
            var saveHttpRequestAs = new MenuItem { Header = "Save Http Request Sequence As..." };

            saveHttpRequest.Command = new DelegateCommand(SetupSaveRequest);
            saveHttpRequestAs.Command = new DelegateCommand(SetupSaveRequestAs);

            mainMenuApplicationService.InsertSeparator(fileMenu, 4);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequest, 5);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequestAs, 6);

            var httpRequestMenu = new MenuItem { Header = "_Http Request Sequences" };
            var runHttpRequest = new MenuItem { Header = "_Run", InputGestureText = "F5", Command = RunSequenceCommand };

            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = saveKeyGesture, Command = saveHttpRequest.Command });
            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = runHttpRequestKeyGesture, Command = RunSequenceCommand });

            mainMenuApplicationService.InsertTopLevelMenuItem(httpRequestMenu, 1);
            mainMenuApplicationService.InsertMenuItem(httpRequestMenu, runHttpRequest, 0);
        }

        public ICommand RunSequenceCommand
        {
            get { return new DelegateCommand(RunSequence); }
        }

        private void RunSequence()
        {
           // throw new NotImplementedException();
        }

        private void SetupSaveRequestAs()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = SaveRequestAs,
                UserControlType = typeof(HttpRequestSequence),
                DataContext = this
            });
        }

        private void SetupSaveRequest()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = SaveRequest,
                UserControlType = typeof(HttpRequestSequence),
                DataContext = this
            });
        }

        private void SaveRequestAs(string id, object httpRequestSequenceContent)
        {
            if (((HttpRequestSequence)httpRequestSequenceContent).DataContext != this)
            {
                return;
            }

            var httpRequestSequence = httpRequestSequenceContent as HttpRequestSequence;

            var httpRequestSequenceViewModel = httpRequestSequence.DataContext as HttpRequestSequenceViewModel;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Rest Box Http Request Sequence (*.rseq)|*.rseq",
                FileName = Path.GetFileName(httpRequestSequence.FilePath) ?? "Untitled",
                Title = "Save Http Request Sequence As..."
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                
                    var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                                   saveFileDialog.FileName);



                    var seequenceExists = Solution.Current.HttpRequestSequenceFiles.Any(x => x.Id == id);
                    if (!seequenceExists)
                    {
                        Solution.Current.HttpRequestSequenceFiles.Add(
                            new File { Id = id, RelativeFilePath = relativePath, Name = title });
                    }
                    else
                    {
                        var existingSequence = Solution.Current.HttpRequestSequenceFiles.First(x => x.Id == id);
                        existingSequence.Name = title;
                        existingSequence.RelativeFilePath = relativePath;
                    }

                    fileService.SaveSolution();

                    WorkflowDesigner.Save(saveFileDialog.FileName);

                    httpRequestSequence.FilePath = relativePath;
                    eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                    {
                        Id = id,
                        Title = title
                    });
                    eventAggregator.GetEvent<UpdateHttpRequestSequenceFileItemEvent>().Publish(new File
                    {
                        Id = id,
                        Name = title,
                        RelativeFilePath =
                            relativePath
                    });

            }
            Keyboard.ClearFocus();
            eventAggregator.GetEvent<IsDirtyEvent>().Publish(false);
            IsDirty = false;
        }

        private void SaveRequest(string id, object httpRequestSequenceContent)
        {
            if (!(httpRequestSequenceContent is HttpRequestSequence))
            {
                return;
            }

            if (((HttpRequestSequence)httpRequestSequenceContent).DataContext != this)
            {
                return;
            }


            var httpRequestSequence = httpRequestSequenceContent as HttpRequestSequence;

            var httpRequestSequenceViewModel = httpRequestSequence.DataContext as HttpRequestSequenceViewModel;

            if (string.IsNullOrWhiteSpace(httpRequestSequence.FilePath))
            {
                SaveRequestAs(id, httpRequestSequenceContent);
                return;
            }

            string filePath = fileService.GetFilePath(Solution.Current.FilePath, httpRequestSequence.FilePath);

            WorkflowDesigner.Save(filePath);

            eventAggregator.GetEvent<IsDirtyEvent>().Publish(false);
            Keyboard.ClearFocus();
            httpRequestSequenceViewModel.IsDirty = false;
        } 

        #endregion

        #region Properties

        public Activity MainSequence { get; set; }
        public bool IsDirty { get; set; }
        public WorkflowDesigner WorkflowDesigner { get; set; }
        public static ConcurrentDictionary<Guid, HttpRequestSequenceViewModel> RunningWorkflows = new ConcurrentDictionary<Guid, HttpRequestSequenceViewModel>();
        public ObservableCollection<HttpResponseItem> Responses { get; set; }

        private HttpResponseItem selectedResponse;
        public HttpResponseItem SelectedResponse
        {
            get { return selectedResponse; }
            set
            {
                selectedResponse = value;
                OnPropertyChanged(x => x.SelectedResponse);
            }
        }

        private bool runnerSelected;
        public bool RunnerSelected
        {
            get { return runnerSelected; }
            set
            {
                runnerSelected = value;
                OnPropertyChanged(x => x.RunnerSelected);
            }
        }

        private bool isProgressBarEnabled;
        public bool IsProgressBarEnabled
        {
            get { return isProgressBarEnabled; }
            set
            {
                isProgressBarEnabled = value;
                OnPropertyChanged(x => x.IsProgressBarEnabled);
            }
        }

        private Visibility progressBarVisibility;
        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                progressBarVisibility = value;
                OnPropertyChanged(x => x.ProgressBarVisibility);
            }
        }

        private Visibility startButtonVisibility;
        public Visibility StartButtonVisibility
        {
            get { return startButtonVisibility; }
            set
            {
                startButtonVisibility = value;
                OnPropertyChanged(x => x.StartButtonVisibility);
            }
        }

        private Visibility cancelButtonVisibility;
        public Visibility CancelButtonVisibility
        {
            get { return cancelButtonVisibility; }
            set
            {
                cancelButtonVisibility = value;
                OnPropertyChanged(x => x.CancelButtonVisibility);
            }
        }

        #endregion

        #region Commands

        public ICommand ExecuteHttpSequenceCommand
        {
            get
            {
                return new DelegateCommand(ExecuteHttpSequence);
            }
        }

        public ICommand CancelHttpSequenceCommand
        {
            get
            {
                return new DelegateCommand(CancelHttpSequence);
            }
        }

        private void CancelHttpSequence()
        {
            workflowApplication.Cancel();
        }

        #endregion

        #region Command Handlers

        private void ExecuteHttpSequence()
        {
            Responses.Clear();
            RunnerSelected = true;
            IsProgressBarEnabled = true;
            StartButtonVisibility = Visibility.Hidden;
            CancelButtonVisibility = Visibility.Visible;
            ProgressBarVisibility = Visibility.Visible;
            workflowApplication = new WorkflowApplication(MainSequence);
            RunningWorkflows.TryAdd(workflowApplication.Id, this);
            workflowApplication.Completed += Completed;
            workflowApplication.Run();
        }

        private void Completed(WorkflowApplicationCompletedEventArgs workflowApplicationCompletedEventArgs)
        {
            HttpRequestSequenceViewModel viewModel;
            RunningWorkflows.TryRemove(workflowApplicationCompletedEventArgs.InstanceId, out viewModel);
            IsProgressBarEnabled = false;
            StartButtonVisibility = Visibility.Visible;
            CancelButtonVisibility = Visibility.Hidden;
            ProgressBarVisibility = Visibility.Hidden;
            var unloader = new Action(UnloadWorkflowApplication);
            unloader.BeginInvoke(null, null);
        }

        private void UnloadWorkflowApplication()
        {
            workflowApplication.Unload();
        }

        #endregion
    }
}
