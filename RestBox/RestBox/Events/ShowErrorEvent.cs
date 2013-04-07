using System.Collections.Generic;
using Microsoft.Practices.Prism.Events;

namespace RestBox.Events
{
    public class ShowErrorEvent : CompositePresentationEvent<KeyValuePair<string, string>>
    {
    }
}
