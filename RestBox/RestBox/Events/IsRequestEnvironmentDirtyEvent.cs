using System.Collections.Generic;
using Microsoft.Practices.Prism.Events;
using RestBox.UserControls;

namespace RestBox.Events
{
    public class IsRequestEnvironmentDirtyEvent : CompositePresentationEvent<RequestEnvironmentSettings>
    {
    }
}
