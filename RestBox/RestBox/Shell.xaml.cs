using System.Windows;
using RestBox.Services;
using RestBox.ViewModels;

namespace RestBox
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        public Shell(ShellViewModel shellViewModel, IApplicationLayout applicationLayout)
        {
            DataContext = shellViewModel;
            shellViewModel.ApplicationTitle = "REST Box";
            
            InitializeComponent();

            shellViewModel.DockingManager = dockingManager;

            applicationLayout.Load(dockingManager);
        }
    }
}
