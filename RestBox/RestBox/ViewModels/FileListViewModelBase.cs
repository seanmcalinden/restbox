using System;
using System.Activities.Presentation.Services;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;
using RestBox.Mappers;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public abstract class FileListViewModelBase<TViewModel, TUserControl, TUserControlViewModel, TItemFile, TSelectItemEvent, TAddMenuItemsEvent> : ViewModelBase<TViewModel>
        where TUserControl : UserControl, ITabUserControlBase
        where TUserControlViewModel : class
        where TSelectItemEvent : CompositePresentationEvent<string>, new()
        where TAddMenuItemsEvent : CompositePresentationEvent<TUserControlViewModel>, new()
    {
        #region Declarations

        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IMapper<TItemFile, TUserControlViewModel> itemToViewModelMapper;
        private readonly string newItemTitle;
        private readonly Func<List<File>> viewFilesAction;
        private readonly Func<List<string>> stringViewFilesAction;
        private readonly string openFileTitle;
        private readonly string openFileFilter;
        private ViewFile viewFileToOpen;
        private const string Warning = "warning";
        private LayoutDocumentType layoutDocumentType;

        #endregion

        #region Constructor

        protected FileListViewModelBase(
            IFileService fileService, 
            IEventAggregator eventAggregator, 
            IMainMenuApplicationService mainMenuApplicationService, 
            IMapper<TItemFile, TUserControlViewModel> itemToViewModelMapper, 
            string newItemTitle, 
            Func<List<File>> viewFilesAction,
            string openFileTitle,
            string openFileFilter)
        {
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.itemToViewModelMapper = itemToViewModelMapper;
            this.newItemTitle = newItemTitle;
            this.viewFilesAction = viewFilesAction;
            this.openFileTitle = openFileTitle;
            this.openFileFilter = openFileFilter;
            SolutionLoadedVisibility = Visibility.Hidden;

            ViewFiles = new ObservableCollection<ViewFile>();
            Groups = new ObservableCollection<string>();
            SetupFilesCollectionView();
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);
            eventAggregator.GetEvent<DocumentChangedEvent>().Subscribe(DocumentChanged);

            if (typeof (TUserControl) == typeof (HttpRequest))
            {
                layoutDocumentType = LayoutDocumentType.HttpRequest;
            }
            else if (typeof(TUserControl) == typeof(HttpRequestSequence))
            {
                layoutDocumentType = LayoutDocumentType.Sequence;
            }
            else if (typeof(TUserControl) == typeof(RequestEnvironmentSettings))
            {
                layoutDocumentType = LayoutDocumentType.Environment;
            }
            else if (typeof(TUserControl) == typeof(HttpInterceptor))
            {
                layoutDocumentType = LayoutDocumentType.Interceptor;
            }
        }

        protected FileListViewModelBase(
            IFileService fileService,
            IEventAggregator eventAggregator,
            IMainMenuApplicationService mainMenuApplicationService,
            IMapper<TItemFile, TUserControlViewModel> itemToViewModelMapper,
            string newItemTitle,
            Func<List<string>> viewFilesAction,
            string openFileTitle,
            string openFileFilter)
        {
            this.fileService = fileService;
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.itemToViewModelMapper = itemToViewModelMapper;
            this.newItemTitle = newItemTitle;
            this.stringViewFilesAction = viewFilesAction;
            this.openFileTitle = openFileTitle;
            this.openFileFilter = openFileFilter;
            SolutionLoadedVisibility = Visibility.Hidden;

            ViewFiles = new ObservableCollection<ViewFile>();
            Groups = new ObservableCollection<string>();
            SetupFilesCollectionView();
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);
            eventAggregator.GetEvent<DocumentChangedEvent>().Subscribe(DocumentChanged);

            if (typeof(TUserControl) == typeof(HttpRequest))
            {
                layoutDocumentType = LayoutDocumentType.HttpRequest;
            }
            else if (typeof(TUserControl) == typeof(HttpRequestSequence))
            {
                layoutDocumentType = LayoutDocumentType.Sequence;
            }
            else if (typeof(TUserControl) == typeof(RequestEnvironmentSettings))
            {
                layoutDocumentType = LayoutDocumentType.Environment;
            }
        } 

        #endregion
        
        #region Properties

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged("SolutionLoadedVisibility"); }
        } 

        private string filesFilter;
        public string FilesFilter
        {
            get { return filesFilter; }
            set
            {
                filesFilter = value; 
                OnPropertyChanged("FilesFilter"); 
                FilesCollectionView.Refresh();
            }
        }

        public ObservableCollection<string> Groups { get; set; }

        public ObservableCollection<ViewFile> ViewFiles { get; set; }

        public ICollectionView FilesCollectionView { get; set; }

        private File selectedFile;
        public File SelectedFile
        {
            get { return selectedFile; }
            set
            {
                selectedFile = value;
                OnPropertyChanged("SelectedFile");
                OnSelectedFileChange(value);
            }
        }

        protected abstract void OnSelectedFileChange(File viewFile);

        #endregion

        #region Event Handlers

        protected virtual void SolutionLoadedEvent(bool obj)
        {
            ViewFiles.Clear();

            if (viewFilesAction != null)
            {
                foreach (var file in viewFilesAction())
                {
                    var requestView = new ViewFile
                        {
                            Id = file.Id,
                            Name = file.Name,
                            Groups = file.Groups,
                            RelativeFilePath = file.RelativeFilePath
                        };
                    if (
                        !fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, file.RelativeFilePath)))
                    {
                        requestView.Icon = Warning;
                    }

                    ViewFiles.Add(requestView);
                }
            }

            SolutionLoadedVisibility = Visibility.Visible;
        }


        protected void DocumentChanged(LayoutData layoutData)
        {
            mainMenuApplicationService.ResetFileMenu();
            DocumentChangedAdditionalHandler();
            RaiseDocumentChanged(layoutData);
        }

        protected abstract void DocumentChangedAdditionalHandler();

        protected virtual void SolutionClosedEvent(bool obj)
        {
            ViewFiles.Clear();
            SolutionLoadedVisibility = Visibility.Hidden;
        }

        public void UpdateFileItem(File file)
        {
            var item = ViewFiles.FirstOrDefault(x => x.Id == file.Id);
            if (item == null)
            {
                return;
            }

            item.Name = file.Name;
            item.RelativeFilePath = file.RelativeFilePath;
        }

        public bool IsLoadingSequence { get; set; }

        public void OpenItem(string fileName)
        {
            var itemUserControl = ServiceLocator.Current.GetInstance<TUserControl>();
            itemUserControl.FilePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath), fileName);
            var viewModel = itemUserControl.DataContext as TUserControlViewModel;

            if (itemToViewModelMapper != null)
            {
                var item = fileService.Load<TItemFile>(fileName);
                itemToViewModelMapper.Map(item, viewModel);
            }

            if (typeof(TUserControl) == typeof(HttpRequestSequence))
            {
                var httpRequestSequenceViewModel = (viewModel as HttpRequestSequenceViewModel);
                IsLoadingSequence = true;
                var activity = ActivityXamlServices.Load(fileName);
                IsLoadingSequence = false;
                httpRequestSequenceViewModel.WorkflowDesigner.Load(activity);
                httpRequestSequenceViewModel.MainSequence = activity;
                var modelService = httpRequestSequenceViewModel.WorkflowDesigner.Context.Services.GetService<ModelService>();
                modelService.ModelChanged += (itemUserControl as HttpRequestSequence).ModelChanged;
            }

            var layoutDocument = new LayoutDocument
            {
                Title = Path.GetFileNameWithoutExtension(fileName),
                ContentId = viewFileToOpen.Id,
                Content = itemUserControl,
                IsSelected = true,
                CanFloat = true,
                IconSource = new BitmapImage(LayoutDocumentUtilities.GetImageUri(layoutDocumentType))
            };
            layoutDocument.Closing += DocumentClosing;

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutDocument.Content).DataContext);
        }
        
        #endregion

        #region Public Methods

        public void Rename()
        {
            
        }

        public void ActivateItem()
        {
            var viewFile = SelectedFile as ViewFile;
            if (selectedFile.Name != newItemTitle)
            {
                if (
                    !fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, selectedFile.RelativeFilePath)))
                {
                    viewFile.Icon = "warning";
                }
                else
                {
                    viewFile.Icon = "";
                }
            }

            OpenItem(viewFile);
        }

        public void UpdateGroups()
        {
            if (selectedFile != null && selectedFile.Groups != null)
            {
                var viewFile = viewFilesAction().FirstOrDefault(x => x.Id == selectedFile.Id);

                if (viewFile != null)
                {
                    viewFile.Groups = selectedFile.Groups;
                }

                fileService.SaveSolution();
            }   
        }

        public void OpenItem(ViewFile viewFile)
        {
            if (viewFile.Icon == Warning)
            {
                return;
            }

            viewFileToOpen = viewFile;

            if (viewFileToOpen.Name == newItemTitle)
            {
                var layoutDocument = new LayoutDocument
                {
                    Title = Path.GetFileNameWithoutExtension(newItemTitle),
                    ContentId = viewFileToOpen.Id,
                    Content = ServiceLocator.Current.GetInstance<TUserControl>(),
                    IsSelected = true,
                    CanFloat = true,
                    IconSource = new BitmapImage(LayoutDocumentUtilities.GetImageUri(layoutDocumentType))
                };
                layoutDocument.Closing += DocumentClosing;

                eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
                eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutDocument.Content).DataContext);
            }
            else
            {
                OpenItem(fileService.GetFilePath(Solution.Current.FilePath, viewFile.RelativeFilePath));
            }
        }

        #endregion

        #region Protected Methods

        protected void RaiseDocumentChanged(
            LayoutData layoutData)
        {
            if (layoutData.ContentId == "StartPage")
            {
                RaiseClearToolBarEvent();
                eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);
                return;
            }

            if (layoutData.Content != null && layoutData.Content.GetType() == typeof(TUserControl))
            {
                if (layoutData.IsSelected)
                {
                    RaiseClearToolBarEvent();
                    eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);
                    eventAggregator.GetEvent<TSelectItemEvent>().Publish(layoutData.ContentId);
                    eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutData.Content).DataContext);
                }
            }
        }

        private void RaiseClearToolBarEvent()
        {
            eventAggregator.GetEvent<UpdateToolBarEvent>().Publish(new List<ToolBarItemData>
                {
                    new ToolBarItemData
                        {
                            Command = null,
                            Visibility = Visibility.Collapsed,
                            ToolBarItemType = ToolBarItemType.Save
                        },
                    new ToolBarItemData
                        {
                            Command = null,
                            Visibility = Visibility.Collapsed,
                            ToolBarItemType = ToolBarItemType.Run
                        }
                });
        }

        #endregion

        #region Commands

        public ICommand NewCommand
        {
            get { return new DelegateCommand(NewItem); }
        }

        public ICommand AddCommand
        {
            get { return new DelegateCommand(AddItem); }
        }

        public ICommand OpenFolderInWindowsExplorerCommand
        {
            get { return new DelegateCommand(OpenFolder); }
        }

        public ICommand DeleteCommand
        {
            get { return new DelegateCommand(DeleteItem); }
        }

        public ICommand RemoveCommand
        {
            get { return new DelegateCommand(RemoveItem); }
        }

        public ICommand CloneCommand
        {
            get { return new DelegateCommand(CloneItem); }
        }

        public ICommand RenameCommand
        {
            get { return new DelegateCommand(RenameItem); }
        }


        #endregion

        #region Command Handlers

        private void NewItem()
        {
            var id = Guid.NewGuid().ToString();
            var newViewFile = new ViewFile
            {
                Id = id,
                Name = newItemTitle
            };
            ViewFiles.Add(newViewFile);

            var userControl = ServiceLocator.Current.GetInstance<TUserControl>();

            ((ISave) userControl.DataContext).IsDirty = true;

            var newItemDocument = new LayoutDocument
            {
                ContentId = id,
                Content = userControl,
                Title = newItemTitle,
                IsSelected = true,
                CanFloat = true,
                IconSource = new BitmapImage(LayoutDocumentUtilities.GetImageUri(layoutDocumentType))
            };

            if (typeof(TUserControl) == typeof(HttpRequestSequence))
            {
                var viewModel = ((UserControl)newItemDocument.Content).DataContext as HttpRequestSequenceViewModel;
                viewModel.WorkflowDesigner.Load(viewModel.MainSequence);
                var modelService = ((HttpRequestSequenceViewModel)userControl.DataContext).WorkflowDesigner.Context.Services.GetService<ModelService>();
                modelService.ModelChanged += (userControl as HttpRequestSequence).ModelChanged;
            }

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(newItemDocument);
            eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)newItemDocument.Content).DataContext);
            newItemDocument.Closing += DocumentClosing;
        }

        private void CloneItem()
        {
            var id = Guid.NewGuid().ToString();
            var newViewFile = new ViewFile
            {
                Id = id,
                Name = newItemTitle
            };
            ViewFiles.Add(newViewFile);

            var item = fileService.Load<TItemFile>(fileService.GetFilePath(Solution.Current.FilePath, selectedFile.RelativeFilePath));
            var itemUserControl = ServiceLocator.Current.GetInstance<TUserControl>();
            itemUserControl.FilePath = null;
            var viewModel = itemUserControl.DataContext as TUserControlViewModel;

            itemToViewModelMapper.Map(item, viewModel);

            ((ISave) viewModel).IsDirty = true;

            var layoutDocument = new LayoutDocument
            {
                Title = Path.GetFileNameWithoutExtension(newItemTitle),
                ContentId = newViewFile.Id,
                Content = itemUserControl,
                IsSelected = true,
                CanFloat = true,
                IconSource = new BitmapImage(LayoutDocumentUtilities.GetImageUri(layoutDocumentType))
            };

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutDocument.Content).DataContext);
            layoutDocument.Closing += DocumentClosing;

        }

        private void RenameItem()
        {
            if (SelectedFile.Name == newItemTitle)
            {
                return;
            }

            var selectedViewFile = SelectedFile as ViewFile;

            selectedViewFile.NameVisibility = Visibility.Collapsed;
            selectedViewFile.EditableNameVisibility = Visibility.Visible;
        }

        private void RemoveItem()
        {
            if (SelectedFile == null)
            {
                return;
            }

            var id = SelectedFile.Id;
            if (SelectedFile.Name == newItemTitle)
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = ViewFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    ViewFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentFile = viewFilesAction().First(x => x.Id == id);
            viewFilesAction().Remove(currentFile);
            var currentUiFile = ViewFiles.First(x => x.Id == id);
            ViewFiles.Remove(currentUiFile);
            selectedFile = null;
            fileService.SaveSolution();
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

        private void DeleteItem()
        {
            if (selectedFile == null)
            {
                return;
            }

            var id = SelectedFile.Id;
            if (SelectedFile.Name == newItemTitle)
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = ViewFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    ViewFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentFile = viewFilesAction().First(x => x.Id == id);
            viewFilesAction().Remove(currentFile);
            var currentUiFile = ViewFiles.First(x => x.Id == id);
            ViewFiles.Remove(currentUiFile);
            selectedFile = null;
            fileService.SaveSolution();
            var filePath = fileService.GetFilePath(Solution.Current.FilePath, currentUiFile.RelativeFilePath);
            fileService.DeleteFile(filePath);
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

        private void AddItem()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = openFileFilter,
                Title = openFileTitle
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                var viewFile = new ViewFile
                {
                    Id = Guid.NewGuid().ToString(),
                    RelativeFilePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                     openFileDialog.FileName),
                    Name = fileName
                };

                viewFilesAction().Add(viewFile);
                ViewFiles.Add(viewFile);
                fileService.SaveSolution();
            }
        }

        public virtual void OpenFolder()
        {
            if (selectedFile != null && selectedFile.RelativeFilePath != null)
            {
                fileService.OpenFileInWindowsExplorer(selectedFile.RelativeFilePath);
            }
        }

        #endregion

        #region Helpers

        private bool FilterFiles(object file)
        {
            if (string.IsNullOrWhiteSpace(FilesFilter))
            {
                return true;
            }
            return (((File)file).Groups != null && ((File)file).Groups.ToLower().Contains(FilesFilter.ToLower()))
                || ((File)file).Name.ToLower().Contains(FilesFilter.ToLower());
        }

        private void SetupFilesCollectionView()
        {
            FilesCollectionView = new CollectionViewSource
            {
                Source = ViewFiles
            }.View;
            FilesCollectionView.Filter = FilterFiles;
        }

        private void DocumentClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var layoutDocument = ((LayoutDocument)sender);

            var context = ((UserControl)layoutDocument.Content).DataContext as ISave;
            if (context != null)
            {
                if (context.IsDirty)
                {
                    var result = MessageBox.Show(string.Format("Do you want to save {0}?", layoutDocument.Title),
                                    string.Format("Save {0}?", layoutDocument.Title),
                                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.Cancel)
                    {
                        cancelEventArgs.Cancel = true;
                        return;
                    }
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        context.Save(layoutDocument.ContentId, layoutDocument.Content);
                    }
                }
            }
            
            if (layoutDocument.Title == newItemTitle)
            {
                var viewFile = ViewFiles.First(x => x.Id == layoutDocument.ContentId);
                ViewFiles.Remove(viewFile);
            }
        }

        #endregion
    }
}
