﻿using System.Activities;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.Events;
using RestBox.ViewModels;
using System.Activities.Presentation;
using System.Threading;

namespace RestBox.Activities
{
    [Designer(typeof(HttpRequestIfElseActivity))]
    public class HttpRequestIfElseActivityModel : NativeActivity, IActivityTemplateFactory
    {
        private readonly IEventAggregator eventAggregator;
        public event PropertyChangedEventHandler PropertyChanged;

        public HttpRequestIfElseActivityModel()
        {
            DisplayName = "Decision";
            eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            ConditionTrueActivity = new ActivityAction();
            ConditionFalseActivity = new ActivityAction();
            ResponseSections = new ObservableCollection<string>();
            Operators = new ObservableCollection<string>();

            ResponseSections.Add("Status Code");
            ResponseSections.Add("Headers");
            ResponseSections.Add("Body");

            Operators.Add("Equals");
            Operators.Add("Does not equal");
            Operators.Add("Includes");
            Operators.Add("Does not include");
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ActivityAction conditionTrueActivity;
        [Browsable(false)]
        public ActivityAction ConditionTrueActivity
        {
            get { return conditionTrueActivity; }
            set
            {
                conditionTrueActivity = value;
                OnPropertyChanged("ConditionTrueActivity");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        private ActivityAction conditionFalseActivity;
        [Browsable(false)]
        public ActivityAction ConditionFalseActivity
        {
            get { return conditionFalseActivity; }
            set
            {
                conditionFalseActivity = value;
                OnPropertyChanged("ConditionFalseActivity");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        public ObservableCollection<string> ResponseSections { get; set; } 
        public ObservableCollection<string> Operators { get; set; } 

        private int selectedResponseSectionIndex;
        public int SelectedResponseSectionIndex
        {
            get { return selectedResponseSectionIndex; }
            set
            {
                selectedResponseSectionIndex = value;
                OnPropertyChanged("SelectedResponseSectionIndex");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        private int selectedOperatorIndex;
        public int SelectedOperatorIndex
        {
            get { return selectedOperatorIndex; }
            set
            {
                selectedOperatorIndex = value;
                OnPropertyChanged("SelectedOperatorIndex");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        private string conditionValue;
        public string ConditionValue
        {
            get { return conditionValue; }
            set
            {
                conditionValue = value;
                OnPropertyChanged("ConditionValue");
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            HttpRequestSequenceViewModel viewModel;
            HttpRequestSequenceViewModel.RunningWorkflows.TryGetValue(context.WorkflowInstanceId, out viewModel);
            Thread.Sleep(1000);//TODO: Hack as needs to wait for thread to update responses
            var lastResponse = viewModel.Responses[viewModel.Responses.Count - 1];

           switch (SelectedOperatorIndex)
           {
               case 0:
                   Equals(context, lastResponse);
                   break;
               case 1:
                   DoesNotEqual(context, lastResponse);
                   break;
               case 2:
                   Includes(context, lastResponse);
                   break;
               case 3:
                   DoesNotInclude(context, lastResponse);
                   break;
           }
        }

        private void DoesNotInclude(NativeActivityContext context, HttpResponseItem lastResponse)
        {
            switch (SelectedResponseSectionIndex)
            {
                case 0:
                    Evaluate(context, !lastResponse.StatusCode.ToString(CultureInfo.InvariantCulture).Contains(ConditionValue));
                    break;
                case 1:
                    Evaluate(context, !lastResponse.Headers.Contains(ConditionValue));
                    break;
                case 2:
                    Evaluate(context, !lastResponse.Body.ToString().Contains(ConditionValue));
                    break;
            }
        }

        private void Includes(NativeActivityContext context, HttpResponseItem lastResponse)
        {
            switch (SelectedResponseSectionIndex)
            {
                case 0:
                    Evaluate(context, lastResponse.StatusCode.ToString(CultureInfo.InvariantCulture).Contains(ConditionValue));
                    break;
                case 1:
                    Evaluate(context, lastResponse.Headers.Contains(ConditionValue));
                    break;
                case 2:
                    Evaluate(context, lastResponse.Body.ToString().Contains(ConditionValue));
                    break;
            }
        }

        private void DoesNotEqual(NativeActivityContext context, HttpResponseItem lastResponse)
        {
            switch (SelectedResponseSectionIndex)
            {
                case 0:
                    Evaluate(context, lastResponse.StatusCode != int.Parse(ConditionValue));
                    break;
                case 1:
                    Evaluate(context, lastResponse.Headers != ConditionValue);
                    break;
                case 2:
                    Evaluate(context, lastResponse.Body.ToString() != ConditionValue);
                    break;
            }
        }

        private void Equals(NativeActivityContext context, HttpResponseItem lastResponse)
        {
            switch (SelectedResponseSectionIndex)
            {
                case 0:
                    Evaluate(context, lastResponse.StatusCode == int.Parse(ConditionValue));
                    break;
                case 1:
                    Evaluate(context, lastResponse.Headers == ConditionValue);
                    break;
                case 2:
                    Evaluate(context, lastResponse.Body.ToString() == ConditionValue);
                    break;
            }
        }
        
        private void Evaluate(NativeActivityContext context, bool expression)
        {
            if (expression)
            {
                ConditionTrue(context);
            }
            else
            {
                ConditionFalse(context);
            }
        }

        private void ConditionTrue(NativeActivityContext context)
        {
            context.ScheduleAction(ConditionTrueActivity);
        }

        private void ConditionFalse(NativeActivityContext context)
        {
            context.ScheduleAction(ConditionFalseActivity);
        }

        [DebuggerNonUserCode]
        public Activity Create(System.Windows.DependencyObject target)
        {
            return new HttpRequestIfElseActivityModel { ConditionTrueActivity = { Handler = new Sequence() }, ConditionFalseActivity = { Handler = new Sequence() } };
        }
    }
}
