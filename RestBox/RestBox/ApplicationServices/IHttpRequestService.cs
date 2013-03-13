using System.Collections.Generic;
using System.Net.Http;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public interface IHttpRequestService
    {
        void ExecuteRequest(HttpRequestViewModel httpRequestSessionViewModel, List<RequestEnvironmentSetting> requestEnvironmentSettings);
    }
}
