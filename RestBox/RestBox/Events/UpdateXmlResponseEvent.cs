﻿using Microsoft.Practices.Prism.Events;
using RestBox.ViewModels;

namespace RestBox.Events
{
    public class UpdateXmlResponseEvent : CompositePresentationEvent<HttpRequestViewModel>
    {
    }
}