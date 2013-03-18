using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Events;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for RequestEnvironmentSettings.xaml
    /// </summary>
    public partial class RequestEnvironmentSettings
    {
        private readonly IEventAggregator eventAggregator;

        public RequestEnvironmentSettings(RequestEnvironmentSettingsViewModel requestEnvironmentSettingsViewModel, IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
            DataContext = requestEnvironmentSettingsViewModel;
            InitializeComponent();
        }

        public string FilePath { get; set; }

        private void RestrictCharacters(object sender, KeyEventArgs e)
        {
            var currentCell = ((DataGrid) sender).CurrentCell;
            if (currentCell.Column.Header.ToString() == "Key")
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            }
        }

        private void SettingsChangedEvent(object sender, SelectedCellsChangedEventArgs e)
        {
            ((RequestEnvironmentSettingsViewModel) DataContext).IsDirty = true;
            eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
        }
    }
}
