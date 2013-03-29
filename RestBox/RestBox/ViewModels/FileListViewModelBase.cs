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
        private readonly string openFileTitle;
        private readonly string openFileFilter;
        private ViewFile viewFileToOpen;
        private const string Warning = "warning";
        private const string StandaloneNewItemPrefix = "StandaloneNewItem";

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
            set { filesFilter = value; OnPropertyChanged("FilesFilter"); FilesCollectionView.Refresh(); }
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
            }
        }

        #endregion

        #region Event Handlers

        protected virtual void SolutionLoadedEvent(bool obj)
        {
            ViewFiles.Clear();

            foreach (var file in viewFilesAction())
            {
                var requestView = new ViewFile
                {
                    Id = file.Id,
                    Name = file.Name,
                    Groups = file.Groups,
                    RelativeFilePath = file.RelativeFilePath
                };
                if (!fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, file.RelativeFilePath)))
                {
                    requestView.Icon = Warning;
                }

                ViewFiles.Add(requestView);
            }

            SolutionLoadedVisibility = Visibility.Visible;
        }


        protected void DocumentChanged(LayoutData layoutData)
        {
            RaiseDocumentChanged(layoutData);
        }

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
                var activity = ActivityXamlServices.Load(fileName);
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
                CanFloat = true
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
                viewFilesAction().First(x => x.Id == selectedFile.Id).Groups = selectedFile.Groups;
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
                    CanFloat = true
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
            if (layoutData != null && layoutData.Content != null && layoutData.Content.GetType() == typeof(TUserControl))
            {
                if (layoutData.IsSelected)
                {
                    eventAggregator.GetEvent<TSelectItemEvent>().Publish(layoutData.ContentId);
                    eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutData.Content).DataContext);
                }
            }
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
                CanFloat = true
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

            var layoutDocument = new LayoutDocument
            {
                Title = Path.GetFileNameWithoutExtension(newItemTitle),
                ContentId = viewFileToOpen.Id,
                Content = itemUserControl,
                IsSelected = true,
                CanFloat = true
            };
            layoutDocument.Closing += DocumentClosing;

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            eventAggregator.GetEvent<TAddMenuItemsEvent>().Publish((TUserControlViewModel)((TUserControl)layoutDocument.Content).DataContext);

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

        private void OpenFolder()
        {
            fileService.OpenFileInWindowsExplorer(selectedFile.RelativeFilePath);
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
            if (layoutDocument.Title == newItemTitle)
            {
                var viewFile = ViewFiles.First(x => x.Id == layoutDocument.ContentId);
                ViewFiles.Remove(viewFile);
            }
            mainMenuApplicationService.CreateInitialMenuItems();
        }

        #endregion
    }
}
