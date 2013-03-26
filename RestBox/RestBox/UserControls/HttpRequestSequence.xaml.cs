﻿using System;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AvalonDock.Layout;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.Activities;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for HttpRequestSequence.xaml
    /// </summary>
    public partial class HttpRequestSequence : ITabUserControlBase
    {
        private readonly HttpRequestSequenceViewModel httpRequestSequenceViewModel;
        private readonly IEventAggregator eventAggregator;
        private readonly IFileService fileService;
        private WorkflowDesigner workflowDesigner;
        private readonly Sequence mainSequence;

        public HttpRequestSequence(HttpRequestSequenceViewModel httpRequestSequenceViewModel, IEventAggregator eventAggregator, IFileService fileService)
        {
            mainSequence = new Sequence();
            this.httpRequestSequenceViewModel = httpRequestSequenceViewModel;
            this.eventAggregator = eventAggregator;
            this.fileService = fileService;
            httpRequestSequenceViewModel.MainSequence = mainSequence;
            DataContext = httpRequestSequenceViewModel;
            InitializeComponent();
            RegisterMetadata();
            AddDesigner();
            AddToolBox();
            //AddPropertyInspector(); 
            workflowDesigner.Context.Items.Subscribe<Selection>(SelectionChanged);
            var ms = workflowDesigner.Context.Services.GetService<ModelService>();
            if (ms != null)
            {
                ms.ModelChanged += ms_ModelChanged;
            }

        }

        private void ms_ModelChanged(object sender, ModelChangedEventArgs e)
        {
            if (e.ItemsAdded != null && e.ItemsAdded.Count<ModelItem>() == 1)
            {
                
            }
            //{
            //    ModelItem item = e.ItemsAdded.FirstOrDefault<ModelItem>();
            //    var test = item.GetCurrentValue() as HttpRequestActivity;

            //    if (test != null && test.Id == null)
            //    {
            //        //do whatever initialization logic is needed here
            //    }
            //}

        }

        private void SelectionChanged(Selection selection) 
        {
            eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);

            var modelItem = selection.PrimarySelection; 

            if (modelItem == null)
            {
                return;
            }

            var httpRequests = modelItem.Properties["HttpRequests"];
            if (modelItem.Properties["HttpRequests"] != null)
            {
                // remove deleted
                var deleted = httpRequests.Collection.Where(x => !Solution.Current.HttpRequestFiles.Any(s => s.Id == x.Properties["Id"].Value.ToString()));

                for (var i = httpRequests.Collection.Count - 1; i >= 0; i--)
                {
                    var deletedItems = deleted as IList<ModelItem> ?? deleted.ToList();
                    if (deletedItems.Any(x => x.Properties["Id"].Value.ToString() == httpRequests.Collection[i].Properties["Id"].Value.ToString()))
                    {
                        httpRequests.Collection.RemoveAt(i);
                    }
                }

                // add new
                foreach (var httpRequestFile in Solution.Current.HttpRequestFiles)
                {
                    if (!httpRequests.Collection.Any(x => x.Properties["Id"].Value.ToString() == httpRequestFile.Id))
                    {
                        httpRequests.Collection.Add(httpRequestFile);
                    }
                }
            }


            

            //var sb = new StringBuilder(); 
            //while (modelItem != null) 
            //{ 
            //    var displayName = modelItem.Properties["DisplayName"]; 
            //    if (displayName != null) { if (sb.Length > 0)                
            //        sb.Insert(0, " - "); 
            //        sb.Insert(0, displayName.ComputedValue); } 
            //    modelItem = modelItem.Parent; } 
        }

        private void RegisterMetadata()
        {
            DesignerMetadata dm = new DesignerMetadata();
            dm.Register();
        }
        
        private void AddDesigner()
        {
            //Create an instance of WorkflowDesigner class.
            workflowDesigner = new WorkflowDesigner();
            httpRequestSequenceViewModel.WorkflowDesigner = workflowDesigner;

            //Place the designer canvas in the middle column of the grid.
            //Grid.SetColumn(this.wd.View, 1);
            Grid.SetColumn(workflowDesigner.View, 1);

            //Load a new Sequence as default.
            //workflowDesigner.Load(mainSequence);
            
            //Add the designer canvas to the grid.
            HttpSequenceGrid.Children.Add(workflowDesigner.View);
        }

        private void AddToolBox()
        {
            ToolboxControl tc = GetToolboxControl();
            Grid.SetColumn(tc, 0);
            HttpSequenceGrid.Children.Add(tc);
        }

        private ToolboxControl GetToolboxControl()
        {
            var toolBoxControl = new ToolboxControl();

            var toolBoxCategory = new ToolboxCategory("Http Requests");

            var sequenceActivity = new ToolboxItemWrapper("System.Activities.Statements.Sequence",
                typeof(Sequence).Assembly.FullName, null, "Sequence");

            //var ifActivity = new ToolboxItemWrapper("System.Activities.Statements.If",
            //    typeof(If).Assembly.FullName, null, "If-Else");

            //var parallelActivity = new ToolboxItemWrapper("System.Activities.Statements.Parallel",
            //    typeof(Parallel).Assembly.FullName, null, "Parallel");

            var httpRequestActivity = new ToolboxItemWrapper("RestBox.Activities.HttpRequestActivityModel",
                typeof(HttpRequestActivityModel).Assembly.FullName, null, "Http Request");

            var httpRequestIfElseActivity = new ToolboxItemWrapper("RestBox.Activities.HttpRequestIfElseActivityModel",
                typeof(HttpRequestIfElseActivityModel).Assembly.FullName, null, "Decision");

            toolBoxCategory.Add(sequenceActivity);
            //toolBoxCategory.Add(ifActivity);
            toolBoxCategory.Add(httpRequestActivity);
            toolBoxCategory.Add(httpRequestIfElseActivity);
            //toolBoxCategory.Add(parallelActivity);

            toolBoxControl.Categories.Add(toolBoxCategory);
            return toolBoxControl;
        }

        private void AddPropertyInspector()
        {
            Grid.SetColumn(workflowDesigner.PropertyInspectorView, 2);
            HttpSequenceGrid.Children.Add(workflowDesigner.PropertyInspectorView);
        }

        public string FilePath { get; set; }

        private void ActivateItem(object sender, MouseButtonEventArgs e)
        {
            if (httpRequestSequenceViewModel.SelectedResponse.CallingRequest.Url == null)
            {
                return;    
            }

            var requestEnvironment = ServiceLocator.Current.GetInstance<RequestEnvironmentsFilesViewModel>();
            var requestEnvironmentSettings = new List<RequestEnvironmentSetting>();
            if (requestEnvironment.Selected != null)
            {
                var requestEnvironmentSettingFile =
                    fileService.Load<RequestEnvironmentSettingFile>(
                        fileService.GetFilePath(Solution.Current.FilePath,
                                                requestEnvironment.Selected.RelativeFilePath));

                requestEnvironmentSettings = requestEnvironmentSettingFile.RequestEnvironmentSettings;
            }

            var httpRequest = ServiceLocator.Current.GetInstance<HttpRequest>();
                var layoutDocument = new LayoutDocument
                {
                    Title = httpRequestSequenceViewModel.SelectedResponse.CallingRequest.Name + " - " + httpRequestSequenceViewModel.SelectedResponse.RequestStartString  + " Sequence Result *",
                    ContentId = "SequenceRequestView-" + Guid.NewGuid(),
                    Content = httpRequest,
                    IsSelected = true,
                    CanFloat = true
                };

                ((HttpRequestViewModel)httpRequest.DataContext).DisplayHttpResponse(new Uri(httpRequestSequenceViewModel.SelectedResponse.CallingRequest.Url), requestEnvironmentSettings, httpRequestSequenceViewModel.SelectedResponse);

                eventAggregator.GetEvent<AddLayoutDocumentEvent>().Publish(layoutDocument);
                eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Publish((HttpRequestViewModel)((HttpRequest)layoutDocument.Content).DataContext);
        }
    }
}
