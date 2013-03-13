using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.UserControls;
using RestBox.ViewModels;
using IFileService = RestBox.ApplicationServices.IFileService;

namespace RestBox
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        private readonly ILayoutApplicationService layoutApplicationService;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IFileService fileService;
        private readonly IEventAggregator eventAggregator;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IIntellisenseService intellisenseService;
        private ShellViewModel shellViewModel;
        private KeyGesture SaveKeyGesture;
        private KeyGesture SaveAllKeyGesture;
        private KeyGesture RunHttpRequestKeyGesture;

        public Shell(
            ShellViewModel shellViewModel, 
            IEventAggregator eventAggregator, 
            ILayoutApplicationService layoutApplicationService, 
            IMainMenuApplicationService mainMenuApplicationService,
            IFileService fileService,
            IJsonSerializer jsonSerializer,
            IIntellisenseService intellisenseService)
        {
            this.layoutApplicationService = layoutApplicationService;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.intellisenseService = intellisenseService;
            this.eventAggregator = eventAggregator;
            this.shellViewModel = shellViewModel;
            DataContext = shellViewModel;
            shellViewModel.ApplicationTitle = "REST Box";
            
            InitializeComponent();

            EnvironmentsLayout.Content = ServiceLocator.Current.GetInstance<RequestEnvironments>();
            RequestExtensions.Content = ServiceLocator.Current.GetInstance<RequestExtensions>();

            //layoutApplicationService.Load(dockingManager);

            eventAggregator.GetEvent<NewHttpRequestEvent>().Subscribe(NewHttpRequest);
            eventAggregator.GetEvent<CloneHttpRequestEvent>().Subscribe(CloneHttpRequest);
            eventAggregator.GetEvent<AddInputBindingEvent>().Subscribe(AddInputBinding);
            eventAggregator.GetEvent<RemoveInputBindingEvent>().Subscribe(RemoveInputBinding);
            eventAggregator.GetEvent<RemoveTabEvent>().Subscribe(RemoveTabById);
            eventAggregator.GetEvent<IsHttpRequestDirtyEvent>().Subscribe(IsDirtyHandler);

            eventAggregator.GetEvent<NewEnvironmentEvent>().Subscribe(NewEnvironment);
            eventAggregator.GetEvent<AddLayoutDocumentEvent>().Subscribe(AddLayoutDocument);
            eventAggregator.GetEvent<SelectLayoutDocumentEvent>().Subscribe(SelectLayoutDocument);
            eventAggregator.GetEvent<CloneEnvironmentEvent>().Subscribe(CloneEnvironment);
            eventAggregator.GetEvent<UpdateTabTitleEvent>().Subscribe(UpdateTabTitle);
            eventAggregator.GetEvent<IsRequestEnvironmentDirtyEvent>().Subscribe(IsRequestEnvironmentDirtyEventHandler);

            eventAggregator.GetEvent<ShowErrorEvent>().Subscribe(ShowError);

            SaveKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            SaveAllKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift);
            RunHttpRequestKeyGesture = new KeyGesture(Key.F5);

            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(CloseSolution);
        }

        public void ShowError(KeyValuePair<string, string> errorMessage)
        {
            MessageBox.Show(errorMessage.Value, errorMessage.Key, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void IsRequestEnvironmentDirtyEventHandler(RequestEnvironmentSettings requestEnvironmentSettings)
        {
            var requestEnvironmentSettingsDocuments = DocumentsPane.Children.Where(x => x.Content is RequestEnvironmentSettings).ToList();
            var currentSettingDocument = requestEnvironmentSettingsDocuments.FirstOrDefault(x => ((RequestEnvironmentSettings) x.Content) == requestEnvironmentSettings);
            if(currentSettingDocument == null)
            {
                return;
            }
            var viewModel = ((RequestEnvironmentSettings) currentSettingDocument.Content).DataContext as RequestEnvironmentSettingsViewModel;

            if(viewModel == null)
            {
                return;
            }

            if(viewModel.IsDirty)
            {
                currentSettingDocument.Title = currentSettingDocument.Title.Replace(" *", string.Empty);
                currentSettingDocument.Title = string.Format("{0} *", currentSettingDocument.Title);
            }
            else
            {
                currentSettingDocument.Title = currentSettingDocument.Title.Replace(" *", string.Empty);
            }
        }

        private void UpdateTabTitle(TabHeader tabHeader)
        {
            var correspondingTab = DocumentsPane.Children.FirstOrDefault(x => x.ContentId == tabHeader.Id);
            if (correspondingTab != null)
            {
                correspondingTab.Title = tabHeader.Title;
            }
        }

        private void AddLayoutDocument(LayoutDocument layoutDocument)
        {
            DocumentsPane.Children.Add(layoutDocument);
            layoutDocument.IsSelected = true;
            layoutDocument.IsActive = true;
            layoutDocument.IsActiveChanged += DocumentIsActiveChanged;
            AddEnvironmentMenuItems();
        }

        private void IsDirtyHandler(bool isDirty)
        {
            var selectedDocument = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected && x.IsActive);
            if (selectedDocument == null)
            {
                return;
            }
            IsDirty(selectedDocument, isDirty);
        }

        private void IsDirty(LayoutContent layoutContent, bool isDirty)
        {
            if (layoutContent == null)
            {
                return;
            }

            if (isDirty)
            {
                if (!layoutContent.Title.EndsWith(" *"))
                {
                    layoutContent.Title = layoutContent.Title + " *";
                }
            }
            else
            {
                layoutContent.Title = layoutContent.Title.Replace(" *", "");
            }
        }

        private void RemoveTabById(string id)
        {
            var documentPane = DocumentsPane.Children.FirstOrDefault(x => x.ContentId == id);
            if(documentPane != null)
            {
                documentPane.Close();
            }
        }

        private void CloseSolution(bool obj)
        {
            var documents = DocumentsPane.Children.Where(x => x.Title != "Start Page").ToList();

            for(var i = 0; i < documents.Count; i++)
            {
                documents[i].Close();
            }
            //documents.Clear();
        }

        private void NewEnvironment(string id)
        {
            var newEnvironmentDocument = new LayoutDocument
            {
                ContentId = id,
                Content = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>(),
                Title = RequestEnvironmentsViewModel.NewEnvironmentTitle,
                IsSelected = true,
                CanFloat = false
            };

            newEnvironmentDocument.IsActiveChanged += DocumentIsActiveChanged;
            DocumentsPane.Children.Add(newEnvironmentDocument);
        }

        private void NewHttpRequest(string id)
        {
            var newHttpRequestDocument = new LayoutDocument
                                             {
                                                 ContentId = id,
                                                 Content = ServiceLocator.Current.GetInstance<HttpRequest>(),
                                                 Title = "New Http Request *",
                                                 IsSelected = true,
                                                 CanFloat = false
                                             };
            newHttpRequestDocument.IsActiveChanged += DocumentIsActiveChanged;
            newHttpRequestDocument.Closing += NewHttpRequestDocumentOnClosing;
            DocumentsPane.Children.Add(newHttpRequestDocument);
        }

        private void CloneEnvironment(RequestEnvironmentViewFile requestEnvironmentViewFile)
        {
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
                Title = RequestEnvironmentsViewModel.NewEnvironmentTitle,
                IsSelected = true,
                CanFloat = false
            };
            newRequestEnvironmentDocument.IsActiveChanged += DocumentIsActiveChanged;
            newRequestEnvironmentDocument.Closing += CloseEnvironmentItem;
            DocumentsPane.Children.Add(newRequestEnvironmentDocument);
        }

        private void CloneHttpRequest(string id)
        {
            var selectedItem = HttpRequestFilesDataGrid.CurrentItem as HttpRequestFile;
            //TODO:selected item can be null
            var httpRequest =
                   fileService.Load<HttpRequestItemFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                     selectedItem.RelativeFilePath));
            var httpRequestControl = ServiceLocator.Current.GetInstance<HttpRequest>();
            httpRequestControl.FilePath = null;
            var viewModel = httpRequestControl.DataContext as HttpRequestViewModel;

            viewModel.RequestUrl = httpRequest.Url;
            viewModel.RequestVerb = viewModel.RequestVerbs.First(x => x.Content.ToString() == httpRequest.Verb);
            viewModel.RequestHeaders = httpRequest.Headers;
            viewModel.RequestBody = httpRequest.Body;

            var newHttpRequestDocument = new LayoutDocument
            {
                ContentId = id,
                Content = httpRequestControl,
                Title = "New Http Request *",
                IsSelected = true,
                CanFloat = false
            };
            newHttpRequestDocument.IsActiveChanged += DocumentIsActiveChanged;
            newHttpRequestDocument.Closing += NewHttpRequestDocumentOnClosing;
            DocumentsPane.Children.Add(newHttpRequestDocument);
        }

        private void NewHttpRequestDocumentOnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var layoutDocument = ((LayoutDocument)sender);
            if(layoutDocument.Title == "New Http Request *")
            {
                var httpRequestFile = shellViewModel.HttpRequestFiles.First(x => x.Id == layoutDocument.ContentId);
                shellViewModel.HttpRequestFiles.Remove(httpRequestFile);
            }
        }

        private void DocumentIsActiveChanged(object sender, EventArgs eventArgs)
        {
            RemoveInputBindings();

            mainMenuApplicationService.CreateInitialMenuItems(shellViewModel);

            SetCloseSolutionState();

            var currentSelectedTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);

            if (currentSelectedTab != null && currentSelectedTab.Content is HttpRequest)
            {
                AddHttpRequestMenuItems();

                var counter = 0;
                foreach (var httpRequestFile in shellViewModel.HttpRequestFiles)
                {
                    if (httpRequestFile.Id == currentSelectedTab.ContentId)
                    {
                        HttpRequestFilesDataGrid.SelectedIndex = counter;
                    }
                    counter++;
                }
            }
            else if (currentSelectedTab != null && currentSelectedTab.Content is RequestEnvironmentSettings)
            {
                AddEnvironmentMenuItems();

                eventAggregator.GetEvent<SelectEnvironmentItemEvent>().Publish(currentSelectedTab.ContentId);
            }
            else
            {
                RemoveInputBindings();
                HttpRequestFilesDataGrid.SelectedItem = null;
            }
        }

        private void SetCloseSolutionState()
        {
            if (Solution.Current.FilePath != null)
            {
                foreach (var item in shellViewModel.MenuItems[0].Items)
                {
                    if (item is MenuItem)
                    {
                        var menuItem = (MenuItem) item;
                        if (menuItem.Header.ToString() == "_Close Solution")
                        {
                            menuItem.IsEnabled = true;
                            break;
                        }
                    }
                }
            }
        }

        private void SaveAll()
        {
            foreach (var layoutContent in DocumentsPane.Children)
            {
                if (layoutContent.Content is HttpRequest)
                {
                    SaveRequest(layoutContent);
                    IsDirty(layoutContent, false);
                    ((HttpRequestViewModel) ((HttpRequest) layoutContent.Content).DataContext).IsDirty = false;
                }
                else if (layoutContent.Content is RequestEnvironmentSettings)
                {
                    SaveEnvironmentFile(layoutContent);
                    IsDirty(layoutContent, false);
                    ((RequestEnvironmentSettingsViewModel)((RequestEnvironmentSettings)layoutContent.Content).DataContext).IsDirty = false;
                }

            }
        }

        private void AddHttpRequestMenuItems()
        {
            var fileMenu = shellViewModel.MenuItems.First(x => x.Header.ToString() == "_File");
            var saveHttpRequest = new MenuItem() { Header = "Save Http Request", InputGestureText = "Ctrl+S" };
            var saveHttpRequestAs = new MenuItem() { Header = "Save Http Request As..." };
            
            saveHttpRequest.Command = new DelegateCommand(SaveRequest);
            saveHttpRequestAs.Command = new DelegateCommand(SaveRequestAs);

            var saveAll = CreateSaveAllMenuItem();

            AddInputBinding(new KeyBindingData { Command = saveHttpRequest.Command, KeyGesture = SaveKeyGesture });
            AddInputBinding(new KeyBindingData { Command = MakeRequestCommand, KeyGesture = RunHttpRequestKeyGesture });
            
            mainMenuApplicationService.InsertSeparator(fileMenu, 4);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequest, 5);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequestAs, 6);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveAll, 7);

            var httpRequestMenu = new MenuItem() {Header = "_Http Requests"};
            var runHttpRequest = new MenuItem() {Header = "_Run", InputGestureText = "F5", Command = MakeRequestCommand};
            
            mainMenuApplicationService.InsertTopLevelMenuItem(DataContext as ShellViewModel, httpRequestMenu, 1);
            mainMenuApplicationService.InsertMenuItem(httpRequestMenu, runHttpRequest, 0);
        }

        private MenuItem CreateSaveAllMenuItem()
        {
            var saveAll = new MenuItem() {Header = "Save All", InputGestureText = "Ctrl+Shift+S"};
            saveAll.Command = new DelegateCommand(SaveAll);
            AddInputBinding(new KeyBindingData {Command = saveAll.Command, KeyGesture = SaveAllKeyGesture});
            return saveAll;
        }

        private void AddEnvironmentMenuItems()
        {
            SetCloseSolutionState();
            var fileMenu = shellViewModel.MenuItems.First(x => x.Header.ToString() == "_File");
            var saveEnvironment = new MenuItem() { Header = "Save Environment", InputGestureText = "Ctrl+S" };
            var saveEnvironmentAs = new MenuItem() { Header = "Save Environment As..." };
            saveEnvironment.Command = new DelegateCommand(SaveEnvironmentFile);
            saveEnvironmentAs.Command = new DelegateCommand(SaveEnvironmentFileAs);
            AddInputBinding(new KeyBindingData { Command = saveEnvironment.Command, KeyGesture = SaveKeyGesture });

            var saveAll = CreateSaveAllMenuItem();

            mainMenuApplicationService.InsertSeparator(fileMenu, 4);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveEnvironment, 5);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveEnvironmentAs, 6);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveAll, 7);
        }

        private void SaveEnvironmentFileAs()
        {
            var currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);

            if (currentTab == null)
            {
                return;
            }

            var environmentSettings = currentTab.Content as RequestEnvironmentSettings;

            var environmentSettingsViewModel = environmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Rest Box Environment (*.renv)|*.renv",
                FileName = Path.GetFileName(environmentSettings.FilePath) ?? "Untitled",
                Title = "Save Environment As..."
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath), saveFileDialog.FileName);

                var title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                var requestExists = Solution.Current.RequestEnvironmentFiles.Any(x => x.Id == currentTab.ContentId);
                if (!requestExists)
                {
                    Solution.Current.RequestEnvironmentFiles.Add(
                        new RequestEnvironmentFile { Id = currentTab.ContentId, RelativeFilePath = relativePath, Name = title });
                }
                else
                {
                    var existingRequestEnvironmentFile = Solution.Current.RequestEnvironmentFiles.First(x => x.Id == currentTab.ContentId);
                    existingRequestEnvironmentFile.Name = title;
                    existingRequestEnvironmentFile.RelativeFilePath = relativePath;
                }

                fileService.SaveSolution();

                var requestEnvironmentFile = new RequestEnvironmentSettingFile()
                                                 {
                                                     RequestEnvironmentSettings = new List<RequestEnvironmentSetting>(environmentSettingsViewModel.Settings)
                                                 };
                fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(requestEnvironmentFile));

                environmentSettings.FilePath = relativePath;
                currentTab.Title = title;
                eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Publish(new RequestEnvironmentFile
                                                                                   {
                                                                                       Id = currentTab.ContentId,
                                                                                       Name = title,
                                                                                       RelativeFilePath = relativePath
                                                                                   });
                foreach (var requestEnvironmentSetting in requestEnvironmentFile.RequestEnvironmentSettings)
                {
                    intellisenseService.AddEnvironmentIntellisenseItem(requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
                }
            }
            Keyboard.ClearFocus();
            eventAggregator.GetEvent<IsRequestEnvironmentDirtyEvent>().Publish(environmentSettings);
            environmentSettingsViewModel.IsDirty = false;
            currentTab.Title = currentTab.Title.Replace(" *", string.Empty);
        }

        private void SaveEnvironmentFile()
        {
            var currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);

            if (currentTab == null)
            {
                return;
            }

            SaveEnvironmentFile(currentTab);
        }

        private void SaveEnvironmentFile(LayoutContent currentTab)
        {
            if (currentTab == null)
            {
                currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);
            }

            if (currentTab == null)
            {
                return;
            }

            var environmentSettings = currentTab.Content as RequestEnvironmentSettings;
            if(string.IsNullOrWhiteSpace(environmentSettings.FilePath))
            {
                SaveEnvironmentFileAs();
                return;
            }
            var environmentSettingsViewModel = environmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

            var requestEnvironmentFile = new RequestEnvironmentSettingFile()
            {
                RequestEnvironmentSettings = new List<RequestEnvironmentSetting>(environmentSettingsViewModel.Settings)
            };

            var filePath = fileService.GetFilePath(Solution.Current.FilePath, environmentSettings.FilePath);

            fileService.SaveFile(filePath, jsonSerializer.ToJsonString(requestEnvironmentFile));

            foreach (var requestEnvironmentSetting in requestEnvironmentFile.RequestEnvironmentSettings)
            {
                intellisenseService.AddEnvironmentIntellisenseItem(requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
            }
            eventAggregator.GetEvent<IsRequestEnvironmentDirtyEvent>().Publish(environmentSettings);
            environmentSettingsViewModel.IsDirty = false;
            currentTab.Title = currentTab.Title.Replace(" *", string.Empty);
        }

        private void RemoveInputBindings()
        {
            RemoveInputBinding(new KeyBindingData { KeyGesture = SaveKeyGesture });
            RemoveInputBinding(new KeyBindingData { KeyGesture = SaveAllKeyGesture });
            RemoveInputBinding(new KeyBindingData { KeyGesture = RunHttpRequestKeyGesture });
        }

        public ICommand MakeRequestCommand
        {
            get
            {
                return new DelegateCommand(MakeRequest);
            }
        }

        private void MakeRequest()
        {
            eventAggregator.GetEvent<MakeRequestEvent>().Publish(true);
        }

        protected override void OnClosed(System.EventArgs e)
        {
            CloseSolution(true);
            //layoutApplicationService.Save(dockingManager);
            base.OnClosed(e);
        }



        private void SelectLayoutDocument(RequestEnvironmentViewFile requestEnvironmentViewFile)
        {
            var fileMissing = false;

            if (requestEnvironmentViewFile.Name != RequestEnvironmentsViewModel.NewEnvironmentTitle)
            {
                if(!fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath, requestEnvironmentViewFile.RelativeFilePath)))
                {
                    requestEnvironmentViewFile.Icon = "warning";
                    fileMissing = true;
                }
                else
                {
                    requestEnvironmentViewFile.Icon = string.Empty;
                }
            }

            var correspondingTab = DocumentsPane.Children.FirstOrDefault(x => x.ContentId == requestEnvironmentViewFile.Id);
            if (correspondingTab != null)
            {
                correspondingTab.IsActive = true;
                correspondingTab.IsSelected = true;
            }
            else
            {
                if (fileMissing)
                {
                    return;
                }
                var requestEnvironmentSettingFile =
                    fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                      requestEnvironmentViewFile.RelativeFilePath));

                var requestEnvironmentSettings = ServiceLocator.Current.GetInstance<RequestEnvironmentSettings>();
                requestEnvironmentSettings.FilePath = requestEnvironmentViewFile.RelativeFilePath;
                var viewModel = requestEnvironmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

                foreach (var requestEnvironmentSetting in requestEnvironmentSettingFile.RequestEnvironmentSettings)
                {
                    viewModel.Settings.Add(requestEnvironmentSetting);
                }

                var layoutDocument = new LayoutDocument
                {
                    Title = requestEnvironmentViewFile.Name,
                    ContentId = requestEnvironmentViewFile.Id,
                    Content = requestEnvironmentSettings,
                    IsSelected = true,
                    CanFloat = false
                };
                DocumentsPane.Children.Add(layoutDocument);
                layoutDocument.IsActiveChanged += DocumentIsActiveChanged;
                layoutDocument.Closing += CloseEnvironmentItem;
            }

            DocumentsPane.Children.First(x => x.ContentId == requestEnvironmentViewFile.Id).IsSelected = true;
            DocumentsPane.Children.First(x => x.ContentId == requestEnvironmentViewFile.Id).IsActive = true;
        }

        private void CloseEnvironmentItem(object sender, CancelEventArgs cancelEventArgs)
        {
            var layoutDocument = ((LayoutDocument)sender);
            if (layoutDocument.Title == RequestEnvironmentsViewModel.NewEnvironmentTitle)
            {
                eventAggregator.GetEvent<CloseEnvironmentItemEvent>().Publish(layoutDocument.ContentId);
            }
        }

        private void ActivateHttpRequestFile(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;

            var httpRequestViewFile = grid.CurrentItem as HttpRequestViewFile;
            var httpRequestFile = grid.CurrentItem as HttpRequestFile;

            var fileMissing = false;

            if (httpRequestViewFile.Name != "New Http Request *")
            {
                if (
                    !fileService.FileExists(fileService.GetFilePath(Solution.Current.FilePath,
                                                                    httpRequestFile.RelativeFilePath)))
                {
                    httpRequestViewFile.Icon = "warning";
                    fileMissing = true;
                }
                else
                {
                    httpRequestViewFile.Icon = "";
                }
            }

            var correspondingTab = DocumentsPane.Children.FirstOrDefault(x => x.ContentId == httpRequestFile.Id);
            if (correspondingTab != null)
            {
                correspondingTab.IsActive = true;
                correspondingTab.IsSelected = true;
            }
            else
            {
                if (fileMissing)
                {
                    return;
                }
                var httpRequest =
                    fileService.Load<HttpRequestItemFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                      httpRequestFile.RelativeFilePath));
                var httpRequestControl = ServiceLocator.Current.GetInstance<HttpRequest>();
                httpRequestControl.FilePath = httpRequestFile.RelativeFilePath;
                var viewModel = httpRequestControl.DataContext as HttpRequestViewModel;
               
                viewModel.RequestUrl = httpRequest.Url;
                viewModel.RequestVerb = viewModel.RequestVerbs.First(x => x.Content.ToString() == httpRequest.Verb);
                viewModel.RequestHeaders = httpRequest.Headers;
                viewModel.RequestBody = httpRequest.Body;

                var layoutDocument = new LayoutDocument
                                         {
                                             Title = httpRequestFile.Name,
                                             ContentId = httpRequestFile.Id,
                                             Content = httpRequestControl,
                                             IsSelected = true,
                                             CanFloat = false
                                         };
                DocumentsPane.Children.Add(layoutDocument);
                layoutDocument.IsActiveChanged += DocumentIsActiveChanged;
                layoutDocument.Closing += NewHttpRequestDocumentOnClosing;
            }
        }

        private void AddInputBinding(KeyBindingData keyBindingData)
        {
            InputBindings.Add(new KeyBinding(keyBindingData.Command, keyBindingData.KeyGesture));
        }

        private void RemoveInputBinding(KeyBindingData keyBindingData)
        {
            KeyBinding keyBindingToRemove = null;
            foreach (var inputBinding in InputBindings)
            {
                if (inputBinding is KeyBinding)
                {
                    var keyBinding = (KeyBinding)inputBinding;
                    if (keyBinding.Key == keyBindingData.KeyGesture.Key && keyBinding.Modifiers == keyBindingData.KeyGesture.Modifiers)
                    {
                        keyBindingToRemove = (KeyBinding)inputBinding;
                    }
                }
            }
            if (keyBindingToRemove != null)
            {
                InputBindings.Remove(keyBindingToRemove);
            }
        }

        private void SaveRequestAs()
        {
            var currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);

            if(currentTab == null)
            {
                return;
            }

            var httpRequest = currentTab.Content as HttpRequest;

            var httpRequestViewModel = httpRequest.DataContext as HttpRequestViewModel;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Rest Box HttpRequest (*.rhrq)|*.rhrq",
                FileName = Path.GetFileName(httpRequest.FilePath) ?? "Untitled",
                Title = "Save Http Request As..."
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath), saveFileDialog.FileName);

                var title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                var requestExists = Solution.Current.HttpRequestFiles.Any(x => x.Id == currentTab.ContentId);
                if (!requestExists)
                {
                    Solution.Current.HttpRequestFiles.Add(
                        new HttpRequestFile{Id = currentTab.ContentId,RelativeFilePath = relativePath,Name = title});
                }
                else
                {
                    var existingHttpRequestFile = Solution.Current.HttpRequestFiles.First(x => x.Id == currentTab.ContentId);
                    existingHttpRequestFile.Name = title;
                    existingHttpRequestFile.RelativeFilePath = relativePath;
                }

                fileService.SaveSolution();
                
                var httpRequestFile = new HttpRequestItemFile
                                          {
                                              Url = httpRequestViewModel.RequestUrl,
                                              Verb = httpRequestViewModel.RequestVerb.Content.ToString(),
                                              Headers = httpRequestViewModel.RequestHeaders,
                                              Body = httpRequestViewModel.RequestBody
                                          };

                fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(httpRequestFile));
                
                httpRequest.FilePath = relativePath;
                currentTab.Title = title;
                shellViewModel.HttpRequestFiles.First(x => x.Id == currentTab.ContentId).Name = title;
                shellViewModel.HttpRequestFiles.First(x => x.Id == currentTab.ContentId).RelativeFilePath = relativePath;
            }
            Keyboard.ClearFocus();
            eventAggregator.GetEvent<IsHttpRequestDirtyEvent>().Publish(false);
            httpRequestViewModel.IsDirty = false;
        }
        
        private void SaveRequest()
        {
            var currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);

            if (currentTab == null)
            {
                return;
            }

            SaveRequest(currentTab);
        }

        private void SaveRequest(LayoutContent currentTab)
        {
            if (currentTab == null)
            {
                currentTab = DocumentsPane.Children.FirstOrDefault(x => x.IsSelected);
            }

            if (currentTab == null)
            {
                return;
            }

            var httpRequest = currentTab.Content as HttpRequest;

            if(string.IsNullOrWhiteSpace(httpRequest.FilePath))
            {
                SaveRequestAs();
                return;
            }

            var httpRequestViewModel = httpRequest.DataContext as HttpRequestViewModel;

            var httpRequestFile = new HttpRequestItemFile
            {
                Url = httpRequestViewModel.RequestUrl,
                Verb = httpRequestViewModel.RequestVerb.Content.ToString(),
                Headers = httpRequestViewModel.RequestHeaders,
                Body = httpRequestViewModel.RequestBody
            };

            var filePath = fileService.GetFilePath(Solution.Current.FilePath, httpRequest.FilePath);

            fileService.SaveFile(filePath, jsonSerializer.ToJsonString(httpRequestFile));


            eventAggregator.GetEvent<IsHttpRequestDirtyEvent>().Publish(false);
            Keyboard.ClearFocus();
            httpRequestViewModel.IsDirty = false;
        }

        private void UpdateGroupsEvent(object sender, TextChangedEventArgs e)
        {
            var selectedItem = HttpRequestFilesDataGrid.SelectedItem as HttpRequestFile;
            if (selectedItem != null && selectedItem.Groups != null)
            {
                Solution.Current.HttpRequestFiles.First(x => x.Id == selectedItem.Id).Groups = selectedItem.Groups;
                fileService.SaveSolution();
            }
        }

        private void RenameHttpRequest(object sender, RoutedEventArgs e)
        {
            var selectedItem = HttpRequestFilesDataGrid.SelectedItem as HttpRequestViewFile;
            selectedItem.NameVisibility = Visibility.Visible;
            selectedItem.EditableNameVisibility = Visibility.Collapsed;

            var sourceFilePath = fileService.GetFilePath(Solution.Current.FilePath, selectedItem.RelativeFilePath);

            var relativePathParts = selectedItem.RelativeFilePath.Split('/');

            var sb = new StringBuilder();

            for(var i = 0; i < relativePathParts.Length; i++)
            {
                if(i == relativePathParts.Length - 1)
                {
                    sb.Append(selectedItem.Name + ".rhrq");
                    break;
                }
                sb.Append(relativePathParts[i] + "/");
            }

            var newRelativePath = sb.ToString();

            var destinationFilePath = fileService.GetFilePath(Solution.Current.FilePath, newRelativePath);

            fileService.MoveFile(sourceFilePath, destinationFilePath);

            selectedItem.RelativeFilePath = newRelativePath;

            var solutionItem = Solution.Current.HttpRequestFiles.First(x => x.Id == selectedItem.Id);
            solutionItem.Name = selectedItem.Name;
            solutionItem.RelativeFilePath = newRelativePath;

            fileService.SaveSolution();

            var correspondingTab = DocumentsPane.Children.FirstOrDefault(x => x.ContentId == selectedItem.Id);
            if(correspondingTab != null)
            {
                correspondingTab.Title = selectedItem.Name;
            }
            
        }
    }
}
