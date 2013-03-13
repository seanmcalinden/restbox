using System.Collections.ObjectModel;
using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentSettingsViewModel : ViewModelBase<RequestEnvironmentSettingsViewModel>
    {
        public RequestEnvironmentSettingsViewModel()
        {
            Settings = new ObservableCollection<RequestEnvironmentSetting>();    
        }

        public ObservableCollection<RequestEnvironmentSetting> Settings { get; set; }

        private bool isDirty;
        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; OnPropertyChanged(x => x.IsDirty); }
        }
    }
}
