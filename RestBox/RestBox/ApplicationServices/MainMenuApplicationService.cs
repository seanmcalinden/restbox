using System.IO;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.Domain.Entities;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class MainMenuApplicationService : IMainMenuApplicationService
    {
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IEventAggregator eventAggregator;

        public MainMenuApplicationService(
            IFileService fileService, 
            IJsonSerializer jsonSerializer,
            IEventAggregator eventAggregator)
        {
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.eventAggregator = eventAggregator;
        }

        public void CreateInitialMenuItems(ShellViewModel shellViewModel)
        {
            shellViewModel.MenuItems.Clear();
            var fileMenu = new MenuItem();
            fileMenu.Header = "_File";

            var newSolution = new MenuItem();
            newSolution.Command = new DelegateCommand(CreateNewSolution);
            newSolution.Header = "_New Solution";

            var openSolution = new MenuItem();
            openSolution.Command = new DelegateCommand(OpenSolution);
            openSolution.Header = "_Open Solution";

            var closeSolution = new MenuItem();
            closeSolution.Command = new DelegateCommand(CloseSolution);
            closeSolution.Header = "_Close Solution";
            closeSolution.IsEnabled = false;

            var exitMenu = new MenuItem();
            exitMenu.Command = shellViewModel.ExitApplicationCommand;
            exitMenu.Header = "E_xit";

            fileMenu.Items.Add(newSolution);
            fileMenu.Items.Add(openSolution);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(closeSolution);
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(exitMenu);

            shellViewModel.MenuItems.Add(fileMenu);
        }

        public void InsertSeparator(MenuItem parent, int position)
        {
            parent.Items.Insert(position, new Separator());
        }

        public void InsertTopLevelMenuItem(ShellViewModel shellViewModel, MenuItem menuItem, int position)
        {
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
            foreach (var item in parent.Items)
            {
                if (item is MenuItem && ((MenuItem)item).Header.ToString() == headerText)
                {
                    return (MenuItem) item;
                }
            }
            return null;
        }

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

        private void OpenSolution()
        {
            eventAggregator.GetEvent<CloseSolutionEvent>().Publish(true);
            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Rest Box Solution (*.rsln)|*.rsln",
                                         Title = "Open Solution File"
                                     };
            if(openFileDialog.ShowDialog() == true)
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
    }
}
