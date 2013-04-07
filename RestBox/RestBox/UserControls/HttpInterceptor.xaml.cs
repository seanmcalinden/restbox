using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Practices.Prism.Events;
using RestBox.Events;
using RestBox.ViewModels;
using System.Windows.Media;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for HttpInterceptor.xaml
    /// </summary>
    public partial class HttpInterceptor : UserControl, ITabUserControlBase
    {
        private readonly HttpInterceptorViewModel httpInterceptorViewModel;
        private readonly IEventAggregator eventAggregator;
        private readonly bool isLoading;
        public HttpInterceptor(HttpInterceptorViewModel httpInterceptorViewModel, IEventAggregator eventAggregator)
        {
            this.httpInterceptorViewModel = httpInterceptorViewModel;
            this.eventAggregator = eventAggregator;
            isLoading = true;
            DataContext = httpInterceptorViewModel;
            isLoading = false;
            InitializeComponent();
            eventAggregator.GetEvent<UpdateInterceptorUrlEvent>().Subscribe(UpdateUrl);
            eventAggregator.GetEvent<UpdateInterceptorHeadersEvent>().Subscribe(UpdateHeaders);
            eventAggregator.GetEvent<UpdateInterceptorBodyEvent>().Subscribe(UpdateBody);
            Url.Background = Brushes.White;
            Headers.Background = Brushes.White;
            Body.Background = Brushes.White;
        }

        private void UpdateBody(HttpInterceptorViewModel httpRequestViewModelToUpdate)
        {
            if (httpRequestViewModelToUpdate != httpInterceptorViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpInterceptorViewModel.Body))
            {
                new TextRange(Body.Document.ContentStart, Body.Document.ContentEnd).Text = httpInterceptorViewModel.Body;
            }
        }

        private void UpdateHeaders(HttpInterceptorViewModel httpInterceptorViewModelToUpdate)
        {
            if (httpInterceptorViewModelToUpdate != httpInterceptorViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpInterceptorViewModel.Headers))
            {
                new TextRange(Headers.Document.ContentStart, Headers.Document.ContentEnd).Text = httpInterceptorViewModel.Headers;
            }
        }

        private void UpdateUrl(HttpInterceptorViewModel httpRequestViewModelToUpdate)
        {
            if (httpRequestViewModelToUpdate != httpInterceptorViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpInterceptorViewModel.Url))
            {
                new TextRange(Url.Document.ContentStart, Url.Document.ContentEnd).Text = httpInterceptorViewModel.Url;
            }
        }

        public string FilePath { get; set; }

        private void OnUrlKeyUp(object sender, KeyEventArgs e)
        {
            if (Url.Document == null) return;
            var documentRange = new TextRange(Url.Document.ContentStart, Url.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right
                && e.Key != Key.F5)
            {
                if (!httpInterceptorViewModel.IsDirty)
                {
                    httpInterceptorViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(httpInterceptorViewModel, true));
                }
            }
        }

        private void OnHeadersKeyUp(object sender, KeyEventArgs e)
        {
            if (Headers.Document == null) return;
            var documentRange = new TextRange(Headers.Document.ContentStart, Headers.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right
                && e.Key != Key.F5)
            {
                if (!httpInterceptorViewModel.IsDirty)
                {
                    httpInterceptorViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(httpInterceptorViewModel, true));
                }
            }
        }

        private void OnBodykeyUp(object sender, KeyEventArgs e)
        {
            if (Body.Document == null) return;
            var documentRange = new TextRange(Body.Document.ContentStart, Body.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right
                && e.Key != Key.F5)
            {
                if (!httpInterceptorViewModel.IsDirty)
                {
                    httpInterceptorViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(httpInterceptorViewModel, true));
                }
            }
        }

        private void UrlTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Url.Document == null) return;
            httpInterceptorViewModel.SetUrl(new TextRange(Url.Document.ContentStart, Url.Document.ContentEnd).Text);
            var documentRange = new TextRange(Url.Document.ContentStart, Url.Document.ContentEnd);
            documentRange.ClearAllProperties();

            TextPointer navigator = Url.Document.ContentStart;
            while (navigator.CompareTo(Url.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    //CheckNonHeaderWordsInRun((Run)navigator.Parent);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private void HeadersTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Headers.Document == null) return;
            httpInterceptorViewModel.SetRequestHeaders(new TextRange(Headers.Document.ContentStart, Headers.Document.ContentEnd).Text);
            var documentRange = new TextRange(Headers.Document.ContentStart, Headers.Document.ContentEnd);
            documentRange.ClearAllProperties();

            TextPointer navigator = Headers.Document.ContentStart;
            while (navigator.CompareTo(Headers.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    //CheckHeaderWordsInRun((Run)navigator.Parent);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            //FormatHeaders();
        }

        private void BodyTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Body.Document == null) return;
            httpInterceptorViewModel.SetRequestBody(new TextRange(Body.Document.ContentStart, Body.Document.ContentEnd).Text);

            var documentRange = new TextRange(Body.Document.ContentStart, Body.Document.ContentEnd);
            documentRange.ClearAllProperties();

            TextPointer navigator = Body.Document.ContentStart;
            while (navigator.CompareTo(Body.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    //CheckNonHeaderWordsInRun((Run)navigator.Parent);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            //FormatBody();
        }

        private void MakeDirtyComboEvent(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            if (!httpInterceptorViewModel.IsDirty)
            {
                httpInterceptorViewModel.IsDirty = true;
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(httpInterceptorViewModel, true));
            }
        }

        private void OnSelectIntellisenseItem(object sender, KeyEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
