﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;
using RestBox.UserControls;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentsViewModel : ViewModelBase<RequestEnvironmentsViewModel>
    {
        public static string NewEnvironmentTitle = "New Environment *";
        private readonly IEventAggregator eventAggregator;
        private readonly IFileService fileService;
        private readonly IIntellisenseService intellisenseService;

        public RequestEnvironmentsViewModel(IEventAggregator eventAggregator, IFileService fileService, IIntellisenseService intellisenseService)
        {
            this.eventAggregator = eventAggregator;
            this.fileService = fileService;
            this.intellisenseService = intellisenseService;
            RequestEnvironmentFiles = new ObservableCollection<RequestEnvironmentViewFile>();
            SolutionLoadedVisibility = Visibility.Hidden;
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(SolutionLoadedEvent);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(SolutionClosedEvent);
            eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Subscribe(UpdateEnvironmentItem);
            eventAggregator.GetEvent<CloseEnvironmentItemEvent>().Subscribe(CloseEnvironmentItem);
            eventAggregator.GetEvent<DocumentChangedEvent>().Subscribe(DocumentChanged);

        }

        private void DocumentChanged(LayoutData layoutData)
        {
            if (layoutData != null && layoutData.Content is RequestEnvironmentSettings)
            {
                if (layoutData.IsSelected)
                {
                    eventAggregator.GetEvent<SelectEnvironmentItemEvent>().Publish(layoutData.ContentId);
                    eventAggregator.GetEvent<AddRequestEnvironmentMenuItemsEvent>().Publish((RequestEnvironmentSettingsViewModel)((RequestEnvironmentSettings)layoutData.Content).DataContext);
                }
            }
        }

        private void CloseEnvironmentItem(string id)
        {
            RequestEnvironmentFiles.Remove(RequestEnvironmentFiles.First(x => x.Id == id));
        }

        private void UpdateEnvironmentItem(RequestEnvironmentFile requestEnvironmentFile)
        {
            var requestEnvironmentFileToUpdate = RequestEnvironmentFiles.First(x => x.Id == requestEnvironmentFile.Id);
            requestEnvironmentFileToUpdate.Name = requestEnvironmentFile.Name;
            requestEnvironmentFileToUpdate.RelativeFilePath = requestEnvironmentFile.RelativeFilePath;
        }

        private void SolutionLoadedEvent(bool obj)
        {
            RequestEnvironmentFiles.Clear();
            foreach(var requestEnvironmentFile in Solution.Current.RequestEnvironmentFiles)
            {
                RequestEnvironmentFiles.Add(new RequestEnvironmentViewFile
                                                {
                                                    Id = requestEnvironmentFile.Id,
                                                    Name = requestEnvironmentFile.Name,
                                                    RelativeFilePath = requestEnvironmentFile.RelativeFilePath
                                                });
                var requestEnvironmentSetting = fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                                 requestEnvironmentFile.RelativeFilePath));
                foreach (var environmentSetting in requestEnvironmentSetting.RequestEnvironmentSettings)
                {
                    intellisenseService.AddEnvironmentIntellisenseItem(environmentSetting.Setting, environmentSetting.SettingValue);
                }
            }

            if (RequestEnvironmentFiles.Count > 0)
            {
                SelectedRequestEnvironment = RequestEnvironmentFiles[0];
            }

            SolutionLoadedVisibility = Visibility.Visible;
        }

        private void SolutionClosedEvent(bool obj)
        {
            RequestEnvironmentFiles.Clear();
            SolutionLoadedVisibility = Visibility.Hidden; 
        }

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged(x => x.SolutionLoadedVisibility); }
        }

        public ObservableCollection<RequestEnvironmentViewFile> RequestEnvironmentFiles { get; set; }

        private RequestEnvironmentViewFile selectedRequestEnvironment;
        public RequestEnvironmentViewFile SelectedRequestEnvironment
        {
            get { return selectedRequestEnvironment; }
            set { selectedRequestEnvironment = value; OnPropertyChanged(x => x.SelectedRequestEnvironment); }
        }

        private RequestEnvironmentViewFile selectedRequestEnvironmentFile;
        public RequestEnvironmentViewFile SelectedRequestEnvironmentFile
        {
            get { return selectedRequestEnvironmentFile; }
            set { selectedRequestEnvironmentFile = value; OnPropertyChanged(x => x.SelectedRequestEnvironmentFile); }
        }

        public void DoubleClickedItem()
        {
            if(selectedRequestEnvironmentFile != null)
            {
                SelectLayoutDocument(selectedRequestEnvironmentFile);
            }
        }

        private void SelectLayoutDocument(RequestEnvironmentViewFile requestEnvironmentViewFile)
        {
            var fileMissing = false;

            RequestEnvironmentSettings requestEnvironmentSettings;
            if (requestEnvironmentViewFile.Name != NewEnvironmentTitle)
            {
                if (
                    !fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath,
                                                                    requestEnvironmentViewFile.RelativeFilePath)))
                {
                    requestEnvironmentViewFile.Icon = "warning";
                    fileMissing = true;
                }
                else
                {
                    requestEnvironmentViewFile.Icon = string.Empty;
                }


                if (fileMissing)
                {
                    return;
                }
                var requestEnvironmentSettingFile =
                    fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                                            requestEnvironmentViewFile.
                                                                                                RelativeFilePath));

                requestEnvironmentSettings = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>();
                requestEnvironmentSettings.FilePath = requestEnvironmentViewFile.RelativeFilePath;
                var viewModel = requestEnvironmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

                foreach (var requestEnvironmentSetting in requestEnvironmentSettingFile.RequestEnvironmentSettings)
                {
                    viewModel.Settings.Add(requestEnvironmentSetting);
                }
            }
            else
            {
                requestEnvironmentSettings = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>();
            }
            var layoutDocument = new LayoutDocument
                                     {
                                         Title = requestEnvironmentViewFile.Name,
                                         ContentId = requestEnvironmentViewFile.Id,
                                         Content = requestEnvironmentSettings,
                                         IsSelected = true,
                                         CanFloat = true
                                     };

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
            eventAggregator.GetEvent<AddRequestEnvironmentMenuItemsEvent>().Publish(
                (RequestEnvironmentSettingsViewModel) ((RequestEnvironmentSettings) layoutDocument.Content).DataContext);

            layoutDocument.Closing += EnvironmentDocumentOnClosing;
        }

        public ICommand NewEnvironmentCommand
        {
            get { return new DelegateCommand(CreateNewEnvironment); }
        }

        public ICommand AddExistingEnvironmentCommand
        {
            get { return new DelegateCommand(AddExistingEnvironment); }
        }

        public ICommand CloneEnvironmentCommand
        {
            get{return new DelegateCommand(CloneEnvironment);}
        }

        public ICommand RemoveEnvironmentCommand
        {
            get{return new DelegateCommand(RemoveEnvironment);}
        }

        public ICommand DeleteEnvironmentCommand
        {
            get{return new DelegateCommand(DeleteEnvironment);}
        }

        public ICommand RenameEnvironmentCommand
        {
            get { return new DelegateCommand(RenameEnvironment); }
        }

        private void RenameEnvironment()
        {
            if (SelectedRequestEnvironmentFile.Name == NewEnvironmentTitle)
            {
                return;
            }

            var selectedHttpRequestViewFile = SelectedRequestEnvironmentFile;

            selectedHttpRequestViewFile.NameVisibility = Visibility.Collapsed;
            selectedHttpRequestViewFile.EditableNameVisibility = Visibility.Visible;
        }

        private void DeleteEnvironment()
        {
            if (SelectedRequestEnvironmentFile == null)
            {
                return;
            }

            var id = SelectedRequestEnvironmentFile.Id;
            if (SelectedRequestEnvironmentFile.Name == NewEnvironmentTitle)
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = RequestEnvironmentFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    RequestEnvironmentFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentRequestEnvironmentFile = Solution.Current.RequestEnvironmentFiles.First(x => x.Id == id);
            Solution.Current.RequestEnvironmentFiles.Remove(currentRequestEnvironmentFile);
            var currentUiRequestEnvironmentFile = RequestEnvironmentFiles.First(x => x.Id == id);
            RequestEnvironmentFiles.Remove(currentUiRequestEnvironmentFile);
            SelectedRequestEnvironmentFile = null;
            fileService.SaveSolution();
            var requestFilePath = fileService.GetFilePath(Solution.Current.FilePath, currentUiRequestEnvironmentFile.RelativeFilePath);
            fileService.DeleteFile(requestFilePath);
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

        private void RemoveEnvironment()
        {
            if (SelectedRequestEnvironmentFile == null)
            {
                return;
            }

            var id = SelectedRequestEnvironmentFile.Id;
            if (SelectedRequestEnvironmentFile.Name == NewEnvironmentTitle)
            {
                eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);

                var selectedItem = RequestEnvironmentFiles.FirstOrDefault(x => x.Id == id);
                if (selectedItem != null)
                {
                    RequestEnvironmentFiles.Remove(selectedItem);
                    return;
                }
            }
            var currentRequestEnvironmentFile = Solution.Current.RequestEnvironmentFiles.First(x => x.Id == id);
            Solution.Current.RequestEnvironmentFiles.Remove(currentRequestEnvironmentFile);
            var currentUiHttpRequestFile = RequestEnvironmentFiles.First(x => x.Id == id);
            RequestEnvironmentFiles.Remove(currentUiHttpRequestFile);
            SelectedRequestEnvironmentFile = null;
            fileService.SaveSolution();
            eventAggregator.GetEvent<RemoveTabEvent>().Publish(id);
        }

        private void CloneEnvironment()
        {
            if(string.IsNullOrWhiteSpace(SelectedRequestEnvironmentFile.RelativeFilePath))
            {
                return;
            }

            var id = Guid.NewGuid().ToString();
            var requestEnvironmentViewFile = new RequestEnvironmentViewFile
            {
                Id = id,
                Name = NewEnvironmentTitle,
                RelativeFilePath = SelectedRequestEnvironmentFile.RelativeFilePath
            };
            RequestEnvironmentFiles.Add(requestEnvironmentViewFile);

            var requestEnvironmentSettings =
                 fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                   requestEnvironmentViewFile.RelativeFilePath));

            var environmentSettings = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>();
            environmentSettings.FilePath = null;

            var viewModel = environmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

            foreach (var requestEnvironmentSettingsItem in requestEnvironmentSettings.RequestEnvironmentSettings)
            {
                viewModel.Settings.Add(requestEnvironmentSettingsItem);
            }

            var newRequestEnvironmentDocument = new LayoutDocument
            {
                ContentId = requestEnvironmentViewFile.Id,
                Content = environmentSettings,
                Title = NewEnvironmentTitle,
                IsSelected = true,
                CanFloat = true
            };
            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(newRequestEnvironmentDocument);
            eventAggregator.GetEvent<AddRequestEnvironmentMenuItemsEvent>().Publish((RequestEnvironmentSettingsViewModel)((RequestEnvironmentSettings)newRequestEnvironmentDocument.Content).DataContext);

            newRequestEnvironmentDocument.Closing += EnvironmentDocumentOnClosing;
        }

        private void CreateNewEnvironment()
        {
            var newEnvironment = new RequestEnvironmentViewFile
                                     {
                                         Id = Guid.NewGuid().ToString(),
                                         Name = NewEnvironmentTitle
                                     };
            RequestEnvironmentFiles.Add(newEnvironment);

            var newEnvironmentDocument = new LayoutDocument
            {
                ContentId = newEnvironment.Id,
                Content = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>(),
                Title = NewEnvironmentTitle,
                IsSelected = true,
                CanFloat = true
            };
            
            newEnvironmentDocument.IsActiveChanged += SelectDataGridItem;
            newEnvironmentDocument.Closing += EnvironmentDocumentOnClosing;

            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(newEnvironmentDocument);
            eventAggregator.GetEvent<AddRequestEnvironmentMenuItemsEvent>().Publish((RequestEnvironmentSettingsViewModel)((RequestEnvironmentSettings)newEnvironmentDocument.Content).DataContext);
        }

        private void AddExistingEnvironment()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Rest Box Environment (*.renv)|*.renv",
                Title = "Add existing environment"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                var requestEnvironmentViewFile = new RequestEnvironmentViewFile
                {
                    Id = Guid.NewGuid().ToString(),
                    RelativeFilePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                     openFileDialog.FileName),
                    Name = fileName
                };

                Solution.Current.RequestEnvironmentFiles.Add(requestEnvironmentViewFile);
                RequestEnvironmentFiles.Add(requestEnvironmentViewFile);
                fileService.SaveSolution();
            }
        }

        private void SelectDataGridItem(object sender, EventArgs e)
        {
            eventAggregator.GetEvent<ResetMenuEvent>().Publish(true);
            var layoutDocument = ((LayoutDocument)sender);
            eventAggregator.GetEvent<SelectEnvironmentItemEvent>().Publish(layoutDocument.ContentId);
        }

        private void EnvironmentDocumentOnClosing(object sender, CancelEventArgs e)
        {
            eventAggregator.GetEvent<ResetMenuEvent>().Publish(true);
            var layoutDocument = ((LayoutDocument)sender);
            if (layoutDocument.Title == NewEnvironmentTitle)
            {
                RequestEnvironmentFiles.Remove(RequestEnvironmentFiles.First(x => x.Id == layoutDocument.ContentId));
            }
        }
    }
}
