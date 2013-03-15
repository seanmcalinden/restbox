using Microsoft.Practices.Prism.Events;
using RestBox.ViewModels;

namespace RestBox.Events
{
    public class MakeRequestEvent : CompositePresentationEvent<HttpRequestViewModel>
    {
    }
}
