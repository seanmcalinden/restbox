using RestBox.ViewModels;

namespace RestBox.Mappers
{
    class MapRequestEnvironmentFileToRequestEnvironmentSettingsViewModel : IMapper<RequestEnvironmentSettingFile, RequestEnvironmentSettingsViewModel>
    {
        public void Map(RequestEnvironmentSettingFile source, RequestEnvironmentSettingsViewModel destination)
        {
            foreach (var requestEnvironmentSetting in source.RequestEnvironmentSettings)
            {
                destination.Settings.Add(requestEnvironmentSetting);
            }
        }
    }
}
