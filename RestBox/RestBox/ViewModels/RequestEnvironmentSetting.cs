using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentSetting : ViewModelBase<RequestEnvironmentSetting>
    {
        private string setting;
        public string Setting
        {
            get { return setting; }
            set { setting = value; OnPropertyChanged(x => x.Setting); }
        }

        private string settingValue;
        public string SettingValue
        {
            get { return settingValue; }
            set { settingValue = value; OnPropertyChanged(x => x.SettingValue); }
        }
    }
}
