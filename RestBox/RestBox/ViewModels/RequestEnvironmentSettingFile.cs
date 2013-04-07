using System.Collections.Generic;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentSettingFile
    {
        public RequestEnvironmentSettingFile()
        {
            RequestEnvironmentSettings = new List<RequestEnvironmentSetting>();    
        }

        public List<RequestEnvironmentSetting> RequestEnvironmentSettings { get; set; }
    }
}
