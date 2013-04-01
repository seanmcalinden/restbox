using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.Activities
{
    [Designer(typeof(HttpRequestActivity))]
    public class HttpRequestActivityModel : NativeActivity
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private IEventAggregator eventAggregator;
        private IHttpRequestService httpRequestService;
        private IFileService fileService;
        private Guid workflowInstanceId;

        public HttpRequestActivityModel()
        {
            DisplayName = "Http Request";
            HttpRequests = new ObservableCollection<File>();
            eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            httpRequestService = ServiceLocator.Current.GetInstance<IHttpRequestService>();
            fileService = ServiceLocator.Current.GetInstance<IFileService>();
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<File> HttpRequests { get; set; }
        
        private int selectedHttpRequestIndex;
        public int SelectedHttpRequestIndex { get { return selectedHttpRequestIndex; } 
            set
            {
                selectedHttpRequestIndex = value; 
                OnPropertyChanged("SelectedHttpRequestIndex");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, true));
            } 
        }

        private string icon;
        public string Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                OnPropertyChanged("Icon");
            }
        }
        
        protected override void Execute(NativeActivityContext context)
        {
            workflowInstanceId = context.WorkflowInstanceId;
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

            var httpRequestItemFile = fileService.Load<HttpRequestItemFile>(fileService.GetFilePath(Solution.Current.FilePath, HttpRequests[SelectedHttpRequestIndex].RelativeFilePath));

            httpRequestService.ExecuteRequest(new HttpRequestItem
            {
                Name = HttpRequests[SelectedHttpRequestIndex].Name,
                Url = httpRequestItemFile.Url,
                Body = httpRequestItemFile.Body,
                Headers = httpRequestItemFile.Headers,
                Verb = httpRequestItemFile.Verb
            }, requestEnvironmentSettings, HandleResponse, HandleError, default(CancellationToken));
        }

        private void HandleError(string errorMessage)
        {
            if (errorMessage.Contains("env."))
            {
                errorMessage = errorMessage + " Have you selected an environment?";
            }

            HttpRequestSequenceViewModel viewModel;
            HttpRequestSequenceViewModel.RunningWorkflows.TryGetValue(workflowInstanceId, out viewModel);
            AddOnUi(viewModel.Responses, new HttpResponseItem(0,null, null, null, null, DateTime.MinValue, DateTime.MinValue, 0, new HttpRequestItem
                {
                    Name = HttpRequests[SelectedHttpRequestIndex].Name + ": " + errorMessage
                }));
        }

        private void HandleResponse(Uri arg1, List<RequestEnvironmentSetting> arg2, HttpResponseItem httpResponseItem)
        {
            HttpRequestSequenceViewModel viewModel;
            HttpRequestSequenceViewModel.RunningWorkflows.TryGetValue(workflowInstanceId, out viewModel);
            AddOnUi(viewModel.Responses, httpResponseItem);
        }

        private static void AddOnUi(ObservableCollection<HttpResponseItem> responses, HttpResponseItem item)
        {
            Action<HttpResponseItem> addMethod = responses.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }

    }
}
