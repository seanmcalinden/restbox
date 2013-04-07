using System;
using System.Collections.Generic;
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
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentSettingsViewModel : ViewModelBase<RequestEnvironmentSettingsViewModel>, ISave
    {
        #region Declarations

        private IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IIntellisenseService intellisenseService;
        private readonly KeyGesture saveKeyGesture; 

        #endregion

        #region Constructor 

        public RequestEnvironmentSettingsViewModel(
           IEventAggregator eventAggregator,
           IMainMenuApplicationService mainMenuApplicationService,
           IFileService fileService,
           IJsonSerializer jsonSerializer,
           IIntellisenseService intellisenseService)
        {
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.intellisenseService = intellisenseService;
            Settings = new ObservableCollection<RequestEnvironmentSetting>();
            eventAggregator.GetEvent<AddRequestEnvironmentMenuItemsEvent>().Subscribe(AddRequestEnvironmentSettingsMenuItems);
            saveKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control);
        } 

        #endregion

        #region Properties

        public ObservableCollection<RequestEnvironmentSetting> Settings { get; set; }

        private bool isDirty;
        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; OnPropertyChanged(x => x.IsDirty); }
        }

        #endregion

        #region Event Handlers

        private void AddRequestEnvironmentSettingsMenuItems(RequestEnvironmentSettingsViewModel requestEnvironmentSettingsViewModel)
        {
            if (requestEnvironmentSettingsViewModel != this)
            {
                return;
            }

            eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);

            var fileMenu = mainMenuApplicationService.Get("file");

            var saveEnvironment = new MenuItem { Header = "Save Environment", InputGestureText = "Ctrl+S" };
            var saveEnvironmentAs = new MenuItem { Header = "Save Environment As..." };
            saveEnvironment.Command = new DelegateCommand(SetupSaveRequest);
            saveEnvironmentAs.Command = new DelegateCommand(SetupSaveRequestAs);
            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = saveKeyGesture, Command = saveEnvironment.Command });

            mainMenuApplicationService.InsertMenuItem(fileMenu, saveEnvironment, 3);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveEnvironmentAs, 4);
            eventAggregator.GetEvent<UpdateToolBarEvent>().Publish(new List<ToolBarItemData>
                {
                    new ToolBarItemData{ Command = saveEnvironment.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.Save}
                });
        } 

        #endregion

        #region Command handlers

        private void SetupSaveRequestAs()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = SaveAs,
                UserControlType = typeof(RequestEnvironmentSettings),
                DataContext = this
            });
        }

        private void SetupSaveRequest()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = Save,
                UserControlType = typeof(RequestEnvironmentSettings),
                DataContext = this
            });
        }

        public void Save(string id, object content)
        {
            if (((RequestEnvironmentSettings)content).DataContext != this)
            {
                return;
            }

            var environmentSettings = content as RequestEnvironmentSettings;

            if (string.IsNullOrWhiteSpace(environmentSettings.FilePath))
            {
                SaveAs(id, content);
                return;
            }

            var environmentSettingsViewModel = environmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

            var requestEnvironmentFile = new RequestEnvironmentSettingFile
            {
                RequestEnvironmentSettings = new List<RequestEnvironmentSetting>(environmentSettingsViewModel.Settings)
            };

            var filePath = fileService.GetFilePath(Solution.Current.FilePath, environmentSettings.FilePath);

            fileService.SaveFile(filePath, jsonSerializer.ToJsonString(requestEnvironmentFile));

            foreach (var requestEnvironmentSetting in requestEnvironmentFile.RequestEnvironmentSettings)
            {
                intellisenseService.AddEnvironmentIntellisenseItem(requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
            }
            eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
            environmentSettingsViewModel.IsDirty = false;
            eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
            {
                Id = id,
                Title = Path.GetFileNameWithoutExtension(environmentSettings.FilePath)
            });
        }

        public void SaveAs(string id, object content)
        {
            if (((RequestEnvironmentSettings)content).DataContext != this)
            {
                return;
            }

            var environmentSettings = content as RequestEnvironmentSettings;

            var environmentSettingsViewModel = environmentSettings.DataContext as RequestEnvironmentSettingsViewModel;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = SystemFileTypes.Environment.FilterText,
                FileName = Path.GetFileName(environmentSettings.FilePath) ?? SystemFileTypes.Environment.UntitledFileName,
                Title = SystemFileTypes.Environment.SaveAsTitle
            };
            var title = string.Empty;
            if (saveFileDialog.ShowDialog() == true)
            {
                var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath), saveFileDialog.FileName);

                title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                var requestExists = Solution.Current.RequestEnvironmentFiles.Any(x => x.Id == id);
                if (!requestExists)
                {
                    Solution.Current.RequestEnvironmentFiles.Add(
                        new File { Id = id, RelativeFilePath = relativePath, Name = title });
                }
                else
                {
                    var existingRequestEnvironmentFile = Solution.Current.RequestEnvironmentFiles.First(x => x.Id == id);
                    existingRequestEnvironmentFile.Name = title;
                    existingRequestEnvironmentFile.RelativeFilePath = relativePath;
                }

                fileService.SaveSolution();

                var requestEnvironmentFile = new RequestEnvironmentSettingFile
                {
                    RequestEnvironmentSettings = new List<RequestEnvironmentSetting>(environmentSettingsViewModel.Settings)
                };
                fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(requestEnvironmentFile));

                environmentSettings.FilePath = relativePath;
                eventAggregator.GetEvent<UpdateEnvironmentItemEvent>().Publish(new File
                {
                    Id = id,
                    Name = title,
                    RelativeFilePath = relativePath
                });
                foreach (var requestEnvironmentSetting in requestEnvironmentFile.RequestEnvironmentSettings)
                {
                    intellisenseService.AddEnvironmentIntellisenseItem(requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
                }

                Keyboard.ClearFocus();
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
                environmentSettingsViewModel.IsDirty = false;
                eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                {
                    Id = id,
                    Title = title
                });
            }
        }

        #endregion
    }
}
