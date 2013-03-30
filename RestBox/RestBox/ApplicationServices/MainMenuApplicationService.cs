using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class MainMenuApplicationService : IMainMenuApplicationService
    {
        #region Declarations
        
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IEventAggregator eventAggregator;
        private readonly KeyGesture saveAllKeyGesture;

        private MenuItem httpRequestsViewMenu;
        private MenuItem environmentsViewMenu;
        private MenuItem requestExtensionsViewMenu;
        private MenuItem sequencesViewMenu;
        private MenuItem resetLayoutMenu;

        #endregion

        #region Constructor

        public MainMenuApplicationService(
          IFileService fileService,
          IJsonSerializer jsonSerializer,
          IEventAggregator eventAggregator)
        {
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.eventAggregator = eventAggregator;
            saveAllKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift);
        } 

        #endregion

        #region Public Methods

        public void CreateInitialMenuItems()
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
            shellViewModel.MenuItems.Clear();
            var fileMenu = CreateFileMenu(shellViewModel);
            var viewMenu = CreateViewMenu();
            var helpMenu = CreateHelpMenu();
            var requestMenu = CreateRequestMenu();
            var environmentMenu = CreateEnvironmentMenu();
            var extensionsMenu = CreateRequestExtensionsMenu();
            var sequencesMenu = CreateSequencesMenu();

            shellViewModel.MenuItems.Add(fileMenu);
            shellViewModel.MenuItems.Add(viewMenu);
            shellViewModel.MenuItems.Add(requestMenu);
            shellViewModel.MenuItems.Add(environmentMenu);
            shellViewModel.MenuItems.Add(sequencesMenu);
            shellViewModel.MenuItems.Add(extensionsMenu);
            shellViewModel.MenuItems.Add(helpMenu);
            eventAggregator.GetEvent<UpdateViewMenuItemChecksEvent>().Publish(true);

            DisableSolutionMenus();

            //if (!string.IsNullOrWhiteSpace(Solution.Current.FilePath))
            //{
            //    LoadSolutionMenus();
            //}
            //else
            //{
            //    DisableSolutionMenus();
            //}
        }

        public void LoadSolutionMenus()
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();

            var requestsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "requests");
            if (requestsMenuItem != null)
            {
                requestsMenuItem.Visibility = Visibility.Visible;
            }

            var environmentsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "environments");
            if (environmentsMenuItem != null)
            {
                environmentsMenuItem.Visibility = Visibility.Visible;
            }

            var sequencesMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "sequences");
            if (sequencesMenuItem != null)
            {
                sequencesMenuItem.Visibility = Visibility.Visible;
            }

            var extensionsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "extensions");
            if (extensionsMenuItem != null)
            {
                extensionsMenuItem.Visibility = Visibility.Visible;
            }
        }

        private static MenuItem CreateSequencesMenu()
        {
            var menuItem = new MenuItem();
            menuItem.Header = "_Sequences";
            menuItem.Name = "sequences";

            var runRequest = new MenuItem();
            runRequest.Header = "_Run";
            runRequest.Name = "sequencesRun";
            runRequest.InputGestureText = "F5";
            runRequest.IsEnabled = false;

            var addRequest = new MenuItem();
            addRequest.Header = "_Add";
            addRequest.Name = "sequencesAdd";

            var newRequest = new MenuItem();
            newRequest.Header = "_New";
            newRequest.Name = "sequencesNew";

            var existingRequest = new MenuItem();
            existingRequest.Header = "_Existing";
            existingRequest.Name = "sequencesExisting";

            addRequest.Items.Add(newRequest);
            addRequest.Items.Add(existingRequest);

            menuItem.Items.Add(runRequest);
            menuItem.Items.Add(new Separator());
            menuItem.Items.Add(addRequest);
            return menuItem;
        }

        private static MenuItem CreateRequestExtensionsMenu()
        {
            var menuItem = new MenuItem();
            menuItem.Header = "E_xtensions";
            menuItem.Name = "extensions";

            var addExtension = new MenuItem();
            addExtension.Header = "_Add Extension";
            addExtension.Name = "addExtension";

            menuItem.Items.Add(addExtension);
            return menuItem;
        }

        private static MenuItem CreateEnvironmentMenu()
        {
            var menuItem = new MenuItem();
            menuItem.Header = "_Environments";
            menuItem.Name = "environments";

            var addRequest = new MenuItem();
            addRequest.Header = "_Add";
            addRequest.Name = "environmentsAdd";

            var newRequest = new MenuItem();
            newRequest.Header = "_New";
            newRequest.Name = "environmentsNew";

            var existingRequest = new MenuItem();
            existingRequest.Header = "_Existing";
            existingRequest.Name = "environmentsExisting";
            
            addRequest.Items.Add(newRequest);
            addRequest.Items.Add(existingRequest);

            menuItem.Items.Add(addRequest);
            return menuItem;
        }

        private static MenuItem CreateRequestMenu()
        {
            var menuItem = new MenuItem();
            menuItem.Header = "_Requests";
            menuItem.Name = "requests";

            var runRequest = new MenuItem();
            runRequest.Header = "_Run";
            runRequest.Name = "requestsRun";
            runRequest.InputGestureText = "F5";
            runRequest.IsEnabled = false;

            var addRequest = new MenuItem();
            addRequest.Header = "_Add";
            addRequest.Name = "requestsAdd";

            var newRequest = new MenuItem();
            newRequest.Header = "_New";
            newRequest.Name = "requestsNew";

            var existingRequest = new MenuItem();
            existingRequest.Header = "_Existing";
            existingRequest.Name = "requestsExisting";

            addRequest.Items.Add(newRequest);
            addRequest.Items.Add(existingRequest);
            menuItem.Items.Add(runRequest);
            menuItem.Items.Add(new Separator());
            menuItem.Items.Add(addRequest);
            return menuItem;
        }

        public void DisableSolutionMenus()
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
            
            var requestsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "requests");
            if (requestsMenuItem != null)
            {
                requestsMenuItem.Visibility = Visibility.Collapsed;
            }

            var environmentsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "environments");
            if (environmentsMenuItem != null)
            {
                environmentsMenuItem.Visibility = Visibility.Collapsed;
            }

            var sequencesMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "sequences");
            if (sequencesMenuItem != null)
            {
                sequencesMenuItem.Visibility = Visibility.Collapsed;
            }

            var extensionsMenuItem = shellViewModel.MenuItems.FirstOrDefault(x => x.Name == "extensions");
            if (extensionsMenuItem != null)
            {
                extensionsMenuItem.Visibility = Visibility.Collapsed;
            }
        }

        public void ResetFileMenu()
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
            var fileMenu = Get("file");
            var newfileMenu = CreateFileMenu(shellViewModel);
            shellViewModel.MenuItems.Remove(fileMenu);
            shellViewModel.MenuItems.Insert(0, newfileMenu);
        }

        private MenuItem CreateHelpMenu()
        {
            var helpMenu = new MenuItem();
            helpMenu.Header = "_Help";

            var tutorials = new MenuItem();
            tutorials.Header = "Online _Tutorials";
            tutorials.Command = new DelegateCommand(() => LaunchHelp(HelpSite.Tutorials));

            var onlineDocumentation = new MenuItem();
            onlineDocumentation.Header = "Online _Documentation";
            onlineDocumentation.Command = new DelegateCommand(() => LaunchHelp(HelpSite.Documentation));

            var aboutMenu = new MenuItem();
            aboutMenu.Header = "_About RestBox";

            helpMenu.Items.Add(tutorials);
            helpMenu.Items.Add(onlineDocumentation);
            helpMenu.Items.Add(new Separator());
            helpMenu.Items.Add(aboutMenu);

            return helpMenu;
        }

        private enum HelpSite
        {
            Tutorials,
            Documentation
        }

        private void LaunchHelp(HelpSite helpSite)
        {
            string target = "http://www.microsoft.com";

            switch (helpSite)
            {
                    case HelpSite.Documentation:
                    target = "http://www.yahoo.com";
                    break;
                    case HelpSite.Tutorials:
                    target = "http://www.google.co.uk";
                    break;
            }

            try
            {
                System.Diagnostics.Process.Start(target);
            }
            catch
                (
                 System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }

        }

        private MenuItem CreateFileMenu(ShellViewModel shellViewModel)
        {
            var fileMenu = new MenuItem();
            fileMenu.Header = "_File";
            fileMenu.Name = "file";

            var newMenu = new MenuItem();
            newMenu.Header = "_New";

            var openMenu = new MenuItem();
            openMenu.Header = "_Open";

            var saveAllMenu = new MenuItem();
            saveAllMenu.Header = "Save A_ll";
            saveAllMenu.Command = new DelegateCommand(SaveAllCommand);
            saveAllMenu.InputGestureText = "Ctrl+Shift+S";
            eventAggregator.GetEvent<AddInputBindingEvent>()
                           .Publish(new KeyBindingData {KeyGesture = saveAllKeyGesture, Command = saveAllMenu.Command});

            var newSolution = new MenuItem();
            newSolution.Command = new DelegateCommand(CreateNewSolution);
            newSolution.Header = "_Solution";

            var newRequest = new MenuItem();
            newRequest.Command = new DelegateCommand(CreateNewRequest);
            newRequest.Header = "_Http Request";

            var openSolution = new MenuItem();
            openSolution.Command = new DelegateCommand(OpenSolution);
            openSolution.Header = "_Solution";

            var openRequest = new MenuItem();
            openRequest.Command = new DelegateCommand(OpenRequest);
            openRequest.Header = "_Http Request";

            var closeSolution = new MenuItem();
            closeSolution.Command = new DelegateCommand(CloseSolution);
            closeSolution.Header = "_Close Solution";
            closeSolution.IsEnabled = true;

            var exitMenu = new MenuItem();
            exitMenu.Command = shellViewModel.ExitApplicationCommand;
            exitMenu.Header = "E_xit";

            newMenu.Items.Add(newSolution);
            newMenu.Items.Add(newRequest);
            openMenu.Items.Add(openSolution);
            openMenu.Items.Add(openRequest);

            fileMenu.Items.Add(newMenu);
            fileMenu.Items.Add(openMenu);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(saveAllMenu);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(closeSolution);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(exitMenu);

            eventAggregator.GetEvent<UpdateToolBarEvent>().Publish(new List<ToolBarItemData>
                {
                    new ToolBarItemData{ Command = newSolution.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.NewSolution},
                    new ToolBarItemData{ Command = openSolution.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.OpenSolution},
                    new ToolBarItemData{ Command = saveAllMenu.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.SaveAll},
                    new ToolBarItemData{ Command = new DelegateCommand(() => LaunchHelp(HelpSite.Documentation)), Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.Help}
                });

            return fileMenu;
        }

        private MenuItem CreateViewMenu()
        {
            var viewMenu = new MenuItem();
            viewMenu.Header = "_View";

            httpRequestsViewMenu = new MenuItem();
            httpRequestsViewMenu.Command = new DelegateCommand(ShowHttpRequestsLayout);
            httpRequestsViewMenu.Header = "_Http Requests";

            environmentsViewMenu = new MenuItem();
            environmentsViewMenu.Command = new DelegateCommand(ShowEvironmentsLayout);
            environmentsViewMenu.Header = "_Environments";

            requestExtensionsViewMenu = new MenuItem();
            requestExtensionsViewMenu.Command = new DelegateCommand(ShowRequestExtensionsLayout);
            requestExtensionsViewMenu.Header = "_Request Extensions";

            sequencesViewMenu = new MenuItem();
            sequencesViewMenu.Command = new DelegateCommand(ShowSequencesLayout);
            sequencesViewMenu.Header = "_Sequences";

            resetLayoutMenu = new MenuItem();
            resetLayoutMenu.Command = new DelegateCommand(ResetLayout);
            resetLayoutMenu.Header = "_Reset layout on restart";

            viewMenu.Items.Add(httpRequestsViewMenu);
            viewMenu.Items.Add(environmentsViewMenu);
            viewMenu.Items.Add(requestExtensionsViewMenu);
            viewMenu.Items.Add(sequencesViewMenu);
            viewMenu.Items.Add(new Separator());
            viewMenu.Items.Add(resetLayoutMenu);
            return viewMenu;
        }

        private void ResetLayout()
        {
            if (resetLayoutMenu.IsChecked)
            {
                eventAggregator.GetEvent<ResetLayoutEvent>().Publish(false);
                resetLayoutMenu.IsChecked = false;
            }
            else
            {
                eventAggregator.GetEvent<ResetLayoutEvent>().Publish(true);
                resetLayoutMenu.IsChecked = true;
            }
        }

        private void SaveAllCommand()
        {
            eventAggregator.GetEvent<SaveAllEvent>().Publish(true);
        }

        private void ShowSequencesLayout()
        {
            eventAggregator.GetEvent<ShowLayoutEvent>().Publish(LayoutType.Sequences);
        }

        private void ShowRequestExtensionsLayout()
        {
            eventAggregator.GetEvent<ShowLayoutEvent>().Publish(LayoutType.RequestExtensions);
        }

        private void ShowEvironmentsLayout()
        {
            eventAggregator.GetEvent<ShowLayoutEvent>().Publish(LayoutType.Environments);
        }

        private void ShowHttpRequestsLayout()
        {
            eventAggregator.GetEvent<ShowLayoutEvent>().Publish(LayoutType.HttpRequests);
        }

        public void InsertSeparator(MenuItem parent, int position)
        {
            parent.Items.Insert(position, new Separator());
        }

        public void InsertTopLevelMenuItem(MenuItem menuItem, int position)
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
            shellViewModel.MenuItems.Insert(position, menuItem);
        }

        public void InsertMenuItem(MenuItem parent, MenuItem menuItem, int position)
        {
            parent.Items.Insert(position, menuItem);
        }

        public void RemoveItem(MenuItem parent, int position)
        {
            parent.Items.RemoveAt(position);
        }

        public MenuItem Get(string name)
        {
            var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
            return shellViewModel.MenuItems.FirstOrDefault(x => x.Name == name);
        }

        public MenuItem GetChild(MenuItem parent, string name)
        {
            if (parent == null)
            {
                var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
                return shellViewModel.MenuItems.FirstOrDefault(x => x.Name == name);
            }

            foreach (var item in parent.Items)
            {
                if (item is MenuItem && ((MenuItem)item).Name == name)
                {
                    return (MenuItem)item;
                }
            }
            return null;
        } 

        public MenuItem Get(MenuItem parent, string headerText)
        {
            if (parent == null)
            {
                var shellViewModel = ServiceLocator.Current.GetInstance<ShellViewModel>();
                return shellViewModel.MenuItems.FirstOrDefault(x => x.Header.ToString() == headerText);
            }

            foreach (var item in parent.Items)
            {
                if (item is MenuItem && ((MenuItem)item).Header.ToString() == headerText)
                {
                    return (MenuItem)item;
                }
            }
            return null;
        } 

        #endregion

        #region Helpers

        private void CreateNewSolution()
        {
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
            var saveFileDialog = new SaveFileDialog
                                     {
                                         Filter = "Rest Box Solution (*.rsln)|*.rsln",
                                         FileName = "Untitled",
                                         Title = "Create Solution File"
                                     };
            if (saveFileDialog.ShowDialog() == true)
            {
                Solution.Current.Clear();
                Solution.Current.Name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                Solution.Current.FilePath = saveFileDialog.FileName;
                fileService.SaveSolution();
                eventAggregator.GetEvent<NewSolutionEvent>().Publish(true);
                LoadSolutionMenus();
                eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Publish(true);
            }
        }

        private void CreateNewRequest()
        {
            // TODO: Doesnt work
            //eventAggregator.GetEvent<NewHttpRequestEvent>().Publish("StandaloneNewItem" + Guid.NewGuid().ToString());
        }

        private void OpenRequest()
        {
            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Http Request (*.rhrq)|*.rhrq",
                                         Title = "Open Http Request File"
                                     };
            if (openFileDialog.ShowDialog() == true)
            {
                eventAggregator.GetEvent<OpenHttpRequestEvent>().Publish(openFileDialog.FileName);
            }
        }

        private void OpenSolution()
        {
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Solution (*.rsln)|*.rsln",
                                         Title = "Open Solution File"
                                     };
            if (openFileDialog.ShowDialog() == true)
            {
                Solution.Current.Clear();

                using (var fileStream = openFileDialog.OpenFile())
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var fileContent = reader.ReadToEnd();
                        Solution.Current = jsonSerializer.FromJsonString<Solution>(fileContent);
                    }

                }
                eventAggregator.GetEvent<OpenSolutionEvent>().Publish(true);
                LoadSolutionMenus();
                eventAggregator.GetEvent<BindSolutionMenuItemsEvent>().Publish(true);
            }
        }

        private void CloseSolution()
        {
            Solution.Current.Clear();
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
        } 

        #endregion
    }
}
