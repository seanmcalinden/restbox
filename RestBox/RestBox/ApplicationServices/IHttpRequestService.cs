using System;
using System.Collections.Generic;
using System.Threading;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public interface IHttpRequestService
    {
        void BeginExecuteRequest(
            HttpRequestItem httpRequestItem,
            List<RequestEnvironmentSetting> requestEnvironmentSettings,
            Action<Uri, List<RequestEnvironmentSetting>, HttpResponseItem> onSuccess,
            Action<string> onError,
            CancellationToken cancellationToken);

        void ExecuteRequest(HttpRequestItem httpRequestItem, List<RequestEnvironmentSetting> requestEnvironmentSettings,
                            Action<Uri, List<RequestEnvironmentSetting>, HttpResponseItem> onSuccess,
                            Action<string> onError, CancellationToken cancellationToken, bool callMainThread = false);
    }
}
