using System.Linq;
using RestBox.ViewModels;

namespace RestBox.Mappers
{
    public class MapHttpRequestItemFileToHttpRequestViewModel : IMapper<HttpRequestItemFile, HttpRequestViewModel>
    {
        public void Map(HttpRequestItemFile source, HttpRequestViewModel destination)
        {
            destination.RequestUrl = source.Url;
            destination.RequestVerb = destination.RequestVerbs.First(x => x.Content.ToString() == source.Verb);
            destination.RequestHeaders = source.Headers;
            destination.RequestBody = source.Body;
        }
    }
}
