using System;
using Microsoft.Practices.Prism.Events;

namespace RestBox.Events
{
    public class GetLayoutDataEvent : CompositePresentationEvent<LayoutDataRequest>
    {
    }

    public class LayoutDataRequest
    {
        public Action<string, object> Action { get; set; }
        public Type UserControlType { get; set; } 
        public object DataContext { get; set; } 
    }
}
