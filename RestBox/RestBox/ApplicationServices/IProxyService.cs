using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public interface IProxyService
    {
        void AddInterceptor(HttpRequestItem httpRequestItemInterceptor);
        void RemoveInterceptor(HttpRequestItem httpRequestItemInterceptor);
        void Start(int port = 0);
        void Shutdown();
    }
}