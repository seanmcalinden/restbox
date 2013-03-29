using System.IO;
using System.Linq;
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

        private MenuItem HttpRequestsMenu;
        private MenuItem EnvironmentsMenu;
        private MenuItem RequestExtensionsMenu;
        private MenuItem SequencesMenu;

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
            var fileMenu = new MenuItem();
            fileMenu.Header = "_File";

            var newMenu = new MenuItem();
            newMenu.Header = "_New";

            var openMenu = new MenuItem();
            openMenu.Header = "_Open";

            var saveAllMenu = new MenuItem();
            saveAllMenu.Header = "Save A_ll";
            saveAllMenu.Command = new DelegateCommand(SaveAllCommand);
            saveAllMenu.InputGestureText = "Ctrl+Shift+S";
            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = saveAllKeyGesture, Command = saveAllMenu.Command });

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

            var viewMenu = new MenuItem();
            viewMenu.Header = "_View";

            HttpRequestsMenu = new MenuItem();
            HttpRequestsMenu.Command = new DelegateCommand(ShowHttpRequestsLayout);
            HttpRequestsMenu.Header = "_Http Requests";
            
            EnvironmentsMenu = new MenuItem();
            EnvironmentsMenu.Command = new DelegateCommand(ShowEvironmentsLayout);
            EnvironmentsMenu.Header = "_Environments";

            RequestExtensionsMenu = new MenuItem();
            RequestExtensionsMenu.Command = new DelegateCommand(ShowRequestExtensionsLayout);
            RequestExtensionsMenu.Header = "_Request Extensions";

            SequencesMenu = new MenuItem();
            SequencesMenu.Command = new DelegateCommand(ShowSequencesLayout);
            SequencesMenu.Header = "_Sequences";

            viewMenu.Items.Add(HttpRequestsMenu);
            viewMenu.Items.Add(EnvironmentsMenu);
            viewMenu.Items.Add(RequestExtensionsMenu);
            viewMenu.Items.Add(SequencesMenu);

            shellViewModel.MenuItems.Add(fileMenu);
            shellViewModel.MenuItems.Add(viewMenu);
            eventAggregator.GetEvent<UpdateViewMenuItemChecksEvent>().Publish(true);
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
