using System.Linq;
using RestBox.ViewModels;

namespace RestBox.Mappers
{
    public class MapHttpRequestItemToHttpInterceptorViewModel : IMapper<HttpRequestItemFile, HttpInterceptorViewModel>
    {
        public void Map(HttpRequestItemFile source, HttpInterceptorViewModel destination)
        {
            destination.Url = source.Url;
            destination.Verb = destination.Verbs.First(x => x.Content.ToString() == source.Verb);
            destination.Headers = source.Headers;
            destination.Body = source.Body;
            destination.IsDirty = false;
        }
    }
}
