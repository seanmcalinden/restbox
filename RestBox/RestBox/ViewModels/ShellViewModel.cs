using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AvalonDock;
using Microsoft.Practices.Prism.Commands;
using RestBox.Services;

namespace RestBox.ViewModels
{
    public class ShellViewModel : ViewModelBase<ShellViewModel>
    {
        private readonly IApplicationLayout applicationLayout;

        public ShellViewModel(IApplicationLayout applicationLayout)
        {
            this.applicationLayout = applicationLayout;
            MenuItems = new ObservableCollection<MenuItem>();
        }

        private string applicationTitle;
        public string ApplicationTitle
        {
            get { return applicationTitle; }
            set { applicationTitle = value; OnPropertyChanged(x => x.ApplicationTitle); }
        }

        public ObservableCollection<MenuItem> MenuItems { get; private set; }

        public DockingManager DockingManager { get; set; }

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand(ExitApplication);
            }
        }

        private void ExitApplication()
        {
            applicationLayout.Save(DockingManager);
            Application.Current.Shutdown();
        }
    }
}
