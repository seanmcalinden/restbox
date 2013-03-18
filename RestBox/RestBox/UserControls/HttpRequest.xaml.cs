using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.ViewModels;

namespace RestBox.UserControls
{
    /// <summary>
    /// Interaction logic for HttpRequest.xaml
    /// </summary>
    public partial class HttpRequest
    {
        private readonly HttpRequestViewModel httpRequestViewModel;
        private readonly IIntellisenseService intellisenseService;
        private readonly IEventAggregator eventAggregator;
        private readonly bool isLoading;

        public HttpRequest(HttpRequestViewModel httpRequestViewModel, IIntellisenseService intellisenseService, IEventAggregator eventAggregator)
        {
            isLoading = true;
            this.httpRequestViewModel = httpRequestViewModel;
            this.intellisenseService = intellisenseService;
            this.eventAggregator = eventAggregator;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
            DataContext = httpRequestViewModel;
            InitializeComponent();
            isLoading = false;

            eventAggregator.GetEvent<UpdateRequestUrlEvent>().Subscribe(UpdateRequestUrl);
            eventAggregator.GetEvent<UpdateRequestHeadersEvent>().Subscribe(UpdateRequestHeaders);
            eventAggregator.GetEvent<UpdateRequestBodyEvent>().Subscribe(UpdateRequestBody);
        }

        private void UpdateRequestBody(HttpRequestViewModel httpRequestViewModelToUpdate)
        {
            if (httpRequestViewModelToUpdate != httpRequestViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpRequestViewModel.RequestBody))
            {
                new TextRange(RequestBody.Document.ContentStart, RequestBody.Document.ContentEnd).Text = httpRequestViewModel.RequestBody;
            }
        }

        private void UpdateRequestHeaders(HttpRequestViewModel httpRequestViewModelToUpdate)
        {
            if (httpRequestViewModelToUpdate != httpRequestViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpRequestViewModel.RequestHeaders))
            {
                new TextRange(RequestHeaders.Document.ContentStart, RequestHeaders.Document.ContentEnd).Text = httpRequestViewModel.RequestHeaders;
            }
        }

        private void UpdateRequestUrl(HttpRequestViewModel httpRequestViewModelToUpdate)
        {
            if (httpRequestViewModelToUpdate != httpRequestViewModel)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(httpRequestViewModel.RequestUrl))
            {
                new TextRange(RequestUrl.Document.ContentStart, RequestUrl.Document.ContentEnd).Text = httpRequestViewModel.RequestUrl;
            }
        }

        public string FilePath { get; set; }
        
        private void MakeDirtyComboEvent(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            if (!httpRequestViewModel.IsDirty)
            {
                httpRequestViewModel.IsDirty = true;
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
            }
        }

        private RichTextBox CurrentTextBox { get; set; }


        private void OnRequestUrlKeyUp(object sender, KeyEventArgs e)
        {
            if (RequestUrl.Document == null) return;
            var documentRange = new TextRange(RequestUrl.Document.ContentStart, RequestUrl.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right
                && e.Key != Key.F5)
            {
                if (!httpRequestViewModel.IsDirty)
                {
                    httpRequestViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
                }
            }

            CurrentTextBox = sender as RichTextBox;

            var caret = CurrentTextBox.CaretPosition;

            var currentLinePosition = caret.GetLineStartPosition(0);
            var lineText = new TextRange(currentLinePosition, caret);

            var suggestions = new List<string>();
            if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("env."))
            {
                suggestions = intellisenseService.GetEnvironmentIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }
            if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("ext."))
            {
                suggestions = intellisenseService.GetRequestExtesnionsIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }

            if ((CurrentTextBox == null) || string.IsNullOrWhiteSpace(lineText.Text) || suggestions.Count == 0)
            {
                IntellisensePopup.IsOpen = false;
                return;
            }

            var intellisenseItems = httpRequestViewModel.IntellisenseItems;
            intellisenseItems.Clear();

            if (suggestions.Count == 0)
            {
                return;
            }

            foreach (var suggestion in suggestions)
            {
                intellisenseItems.Add(suggestion);
            }

            ShowMethodsPopup(CurrentTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward));
        }

        private void OnHeadersKeyUp(object sender, KeyEventArgs e)
        {
            if (RequestHeaders.Document == null) return;
            var documentRange = new TextRange(RequestHeaders.Document.ContentStart, RequestHeaders.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right)
            {
                if (!httpRequestViewModel.IsDirty)
                {
                    httpRequestViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
                }
            }

            CurrentTextBox = sender as RichTextBox;

            var caret = CurrentTextBox.CaretPosition;

            var currentLinePosition = caret.GetLineStartPosition(0);
            var lineText = new TextRange(currentLinePosition, caret);

            List<string> suggestions;
            if(lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("env."))
            {
                suggestions = intellisenseService.GetEnvironmentIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }
            else if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("ext."))
            {
                suggestions = intellisenseService.GetRequestExtesnionsIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }
            else
            {
                suggestions = intellisenseService.GetSuggestions(lineText.Text);
            }

            if ((CurrentTextBox == null) || string.IsNullOrWhiteSpace(lineText.Text) || suggestions.Count == 0)
            {
                IntellisensePopup.IsOpen = false;
                return;
            }

            var intellisenseItems = httpRequestViewModel.IntellisenseItems;
            intellisenseItems.Clear();

            if (suggestions.Count == 0)
            {
                return;
            }

            foreach (var suggestion in suggestions)
            {
                intellisenseItems.Add(suggestion);
            }

            ShowMethodsPopup(CurrentTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward));
        }

        private void OnRequestBodykeyUp(object sender, KeyEventArgs e)
        {
            if (RequestBody.Document == null) return;
            var documentRange = new TextRange(RequestBody.Document.ContentStart, RequestBody.Document.ContentEnd);

            if (documentRange.Text.Length > 0
                && e.Key != Key.Down
                && e.Key != Key.Up
                && e.Key != Key.Left
                && e.Key != Key.Right)
            {
                if (!httpRequestViewModel.IsDirty)
                {
                    httpRequestViewModel.IsDirty = true;
                    eventAggregator.GetEvent<IsDirtyEvent>().Publish(true);
                }
            }

            CurrentTextBox = sender as RichTextBox;

            var caret = CurrentTextBox.CaretPosition;

            var currentLinePosition = caret.GetLineStartPosition(0);
            var lineText = new TextRange(currentLinePosition, caret);

            var suggestions = new List<string>();
            if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("env."))
            {
                suggestions = intellisenseService.GetEnvironmentIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }
            if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("ext."))
            {
                suggestions = intellisenseService.GetRequestExtesnionsIntellisenseItems(lineText.Text.Substring(lineText.Text.Length - 4, 4));
            }

            if ((CurrentTextBox == null) || string.IsNullOrWhiteSpace(lineText.Text) || suggestions.Count == 0)
            {
                IntellisensePopup.IsOpen = false;
                return;
            }

            var intellisenseItems = httpRequestViewModel.IntellisenseItems;
            intellisenseItems.Clear();

            if (suggestions.Count == 0)
            {
                return;
            }

            foreach (var suggestion in suggestions)
            {
                intellisenseItems.Add(suggestion);
            }

            ShowMethodsPopup(CurrentTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward));
        }

        private void ShowMethodsPopup(Rect placementRect)
        {
            IntellisensePopup.PlacementTarget = CurrentTextBox;
            IntellisensePopup.PlacementRectangle = placementRect;
            IntellisensePopup.IsOpen = true;
            //IntellisensePopup.PreviewKeyDown += IntellisensePopupOnPreviewKeyDown;
            CurrentTextBox.PreviewKeyDown += CurrentTextBoxOnPreviewKeyDown;
        }

        private void IntellisensePopupOnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Key)
            {
                case Key.Down:
                case Key.Up:
                    IntellisenseItems.Focus();
                    break;
                default:
                    CurrentTextBox.Focus();
                    break;
            }
        }

        private void CurrentTextBoxOnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Key)
            {
                case Key.Down:
                    IntellisenseItems.Focus();
                    break;
            }
        }

        private void OnSelectIntellisenseItem(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                IntellisensePopup.IsOpen = false;
                var caret = CurrentTextBox.CaretPosition;
                var currentLinePosition = caret.GetLineStartPosition(0);
                var lineText = new TextRange(currentLinePosition, caret);
                var selectedItemString = IntellisenseItems.SelectedItem.ToString();
                CurrentTextBox.BeginChange();
                if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("env."))
                {
                    CurrentTextBox.CaretPosition.InsertTextInRun(selectedItemString);
                    CurrentTextBox.CaretPosition = CurrentTextBox.CaretPosition.GetPositionAtOffset(IntellisenseItems.SelectedItem.ToString().Length, LogicalDirection.Forward);
                }
                else if (lineText.Text.Length >= 4 && lineText.Text.Substring(lineText.Text.Length - 4, 4).StartsWith("ext."))
                {
                    CurrentTextBox.CaretPosition.InsertTextInRun(string.Format("{0}(\"\", 0)", selectedItemString));
                    CurrentTextBox.CaretPosition = CurrentTextBox.CaretPosition.GetPositionAtOffset(IntellisenseItems.SelectedItem.ToString().Length + 2, LogicalDirection.Forward);
                }
                else
                {
                    int offsetPosition = 0;
                    for (var i = lineText.Text.Length - 1; i > 0; i--)
                    {
                        if (lineText.Text[i] == ':' || lineText.Text[i] == ',')
                        {
                            offsetPosition = i;
                            break;
                        }
                    }

                    if (offsetPosition == 0)
                    {
                        lineText.Text = "";
                        currentLinePosition.InsertTextInRun(selectedItemString);
                       CurrentTextBox.CaretPosition =
                            currentLinePosition.GetPositionAtOffset(
                                IntellisenseItems.SelectedItem.ToString().Length + 1, LogicalDirection.Forward);
                    }
                    else
                    {
                        var newWordPosition = currentLinePosition.GetPositionAtOffset(offsetPosition + 4, LogicalDirection.Forward);
                        var deleteLineText = new TextRange(newWordPosition, caret);
                        deleteLineText.Text = "";
                        newWordPosition.InsertTextInRun(selectedItemString);
                        CurrentTextBox.CaretPosition =
                            currentLinePosition.GetPositionAtOffset(offsetPosition + 4 +
                                IntellisenseItems.SelectedItem.ToString().Length + 1, LogicalDirection.Forward);
                    }
                }
                CurrentTextBox.Focus();
                CurrentTextBox.EndChange();
               break;
            
                case Key.Escape:
                    IntellisensePopup.IsOpen = false;
                    CurrentTextBox.Focus();
                    break;
            }
        }

        private void HeadersTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RequestHeaders.Document == null) return;
            httpRequestViewModel.SetRequestHeaders(new TextRange(RequestHeaders.Document.ContentStart, RequestHeaders.Document.ContentEnd).Text);
            var documentRange = new TextRange(RequestHeaders.Document.ContentStart, RequestHeaders.Document.ContentEnd); 
            documentRange.ClearAllProperties();

            TextPointer navigator = RequestHeaders.Document.ContentStart;
            while (navigator.CompareTo(RequestHeaders.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward); 
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    CheckHeaderWordsInRun((Run)navigator.Parent);
                } 
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            FormatHeaders();
        }

        private struct TextHighlightTag
        {
            public TextPointer StartPosition;
            public TextPointer EndPosition;
        }

        readonly List<TextHighlightTag> environmentTags = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> environmentSettings = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> missingEnvironmentTags = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> extensionTags = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> requestExtensionTags = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> missingRequestExtensionTags = new List<TextHighlightTag>();
        readonly List<TextHighlightTag> headerTags = new List<TextHighlightTag>();
        private void FormatHeaders()
        {
            RequestHeaders.TextChanged -= HeadersTextChanged;

            for (int i = 0; i < environmentTags.Count; i++)
            {
                var range = new TextRange(environmentTags[i].StartPosition, environmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Teal));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentTags.Clear();

            for (int i = 0; i < environmentSettings.Count; i++)
            {
                var range = new TextRange(environmentSettings[i].StartPosition, environmentSettings[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentSettings.Clear();

            for (int i = 0; i < missingEnvironmentTags.Count; i++)
            {
                var range = new TextRange(missingEnvironmentTags[i].StartPosition, missingEnvironmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingEnvironmentTags.Clear();

            for (int i = 0; i < extensionTags.Count; i++)
            {
                var range = new TextRange(extensionTags[i].StartPosition, extensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.CadetBlue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            extensionTags.Clear();

            for (int i = 0; i < requestExtensionTags.Count; i++)
            {
                var range = new TextRange(requestExtensionTags[i].StartPosition, requestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Brown));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            requestExtensionTags.Clear();

            for (int i = 0; i < missingRequestExtensionTags.Count; i++)
            {
                var range = new TextRange(missingRequestExtensionTags[i].StartPosition, missingRequestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingRequestExtensionTags.Clear();

            for (int i = 0; i < headerTags.Count; i++)
            {
                var range = new TextRange(headerTags[i].StartPosition, headerTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Green));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            headerTags.Clear();

            RequestHeaders.TextChanged += HeadersTextChanged;
        }

        private void FormatUrl()
        {
            RequestUrl.TextChanged -= UrlTextChanged;

            for (int i = 0; i < environmentTags.Count; i++)
            {
                var range = new TextRange(environmentTags[i].StartPosition, environmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Teal));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentTags.Clear();

            for (int i = 0; i < environmentSettings.Count; i++)
            {
                var range = new TextRange(environmentSettings[i].StartPosition, environmentSettings[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentSettings.Clear();

            for (int i = 0; i < missingEnvironmentTags.Count; i++)
            {
                var range = new TextRange(missingEnvironmentTags[i].StartPosition, missingEnvironmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingEnvironmentTags.Clear();

            for (int i = 0; i < extensionTags.Count; i++)
            {
                var range = new TextRange(extensionTags[i].StartPosition, extensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.CadetBlue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            extensionTags.Clear();

            for (int i = 0; i < requestExtensionTags.Count; i++)
            {
                var range = new TextRange(requestExtensionTags[i].StartPosition, requestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Brown));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            requestExtensionTags.Clear();

            for (int i = 0; i < missingRequestExtensionTags.Count; i++)
            {
                var range = new TextRange(missingRequestExtensionTags[i].StartPosition, missingRequestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingRequestExtensionTags.Clear();

            RequestUrl.TextChanged += UrlTextChanged;
        }

        private void FormatBody()
        {
            RequestBody.TextChanged -= BodyTextChanged;

            for (int i = 0; i < environmentTags.Count; i++)
            {
                var range = new TextRange(environmentTags[i].StartPosition, environmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Teal));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentTags.Clear();

            for (int i = 0; i < environmentSettings.Count; i++)
            {
                var range = new TextRange(environmentSettings[i].StartPosition, environmentSettings[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            environmentSettings.Clear();

            for (int i = 0; i < missingEnvironmentTags.Count; i++)
            {
                var range = new TextRange(missingEnvironmentTags[i].StartPosition, missingEnvironmentTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingEnvironmentTags.Clear();

            for (int i = 0; i < extensionTags.Count; i++)
            {
                var range = new TextRange(extensionTags[i].StartPosition, extensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.CadetBlue));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            extensionTags.Clear();

            for (int i = 0; i < requestExtensionTags.Count; i++)
            {
                var range = new TextRange(requestExtensionTags[i].StartPosition, requestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Brown));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            requestExtensionTags.Clear();

            for (int i = 0; i < missingRequestExtensionTags.Count; i++)
            {
                var range = new TextRange(missingRequestExtensionTags[i].StartPosition, missingRequestExtensionTags[i].EndPosition);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            missingRequestExtensionTags.Clear();

            RequestBody.TextChanged += BodyTextChanged;
        }

        private void CheckHeaderWordsInRun(Run run)
        {
            string text = run.Text;

            int sIndex = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if(text[i] == ':')
                {
                    if (i > 0)
                    {
                        int eIndex = i - 1;
                        string word = text.Substring(sIndex, eIndex - sIndex + 1);
                        var tag = new TextHighlightTag();
                        tag.StartPosition = run.ContentStart.GetPositionAtOffset(sIndex, LogicalDirection.Forward);
                        tag.EndPosition = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Backward);
                        headerTags.Add(tag);
                    }
                    sIndex = i + 1;
                }
                else if(i > 0 && text[i - 1] == '.')
                {
                    if(i >= 4)
                    {
                        string word = text.Substring(i - 4, 4);
                        if(word == "env." || word == "ext.")
                        {
                            var tagPrefix = new TextHighlightTag();
                            tagPrefix.StartPosition = run.ContentStart.GetPositionAtOffset(i - 4, LogicalDirection.Forward);
                            tagPrefix.EndPosition = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Backward);
                            if (word == "env.")
                            {
                                environmentTags.Add(tagPrefix);
                            }
                            else
                            {
                                extensionTags.Add(tagPrefix);
                            }

                            // get following word
                            for (int j = i; j <= text.Length; j++)
                            {
                                if(text[i] == ' ')
                                {
                                    break;
                                }
                                var settingsWord = text.Substring(i, j - i);
                                if(settingsWord.Length == 0)
                                {
                                    continue;
                                }
                                if (intellisenseService.CheckKey(word, settingsWord))
                                {
                                    var tag = new TextHighlightTag();
                                    tag.StartPosition = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Forward);
                                    tag.EndPosition = run.ContentStart.GetPositionAtOffset(i + (j - i), LogicalDirection.Backward);
                                    if (word == "env.")
                                    {
                                        environmentSettings.Add(tag);
                                    }
                                    else
                                    {
                                        requestExtensionTags.Add(tag);
                                    }
                                    break;
                                }
                                if (j == text.Length)
                                {
                                    var tag = new TextHighlightTag();
                                    tag.StartPosition = run.ContentStart.GetPositionAtOffset(i,
                                                                                             LogicalDirection
                                                                                                 .Forward);
                                    tag.EndPosition = run.ContentStart.GetPositionAtOffset(i + (j - i),
                                                                                           LogicalDirection.
                                                                                               Backward);
                                    if (word == "env.")
                                    {
                                        missingEnvironmentTags.Add(tag);
                                    }
                                    else
                                    {
                                        missingRequestExtensionTags.Add(tag);
                                    }
                                }
                            }
                        }
                    }
                    sIndex = i + 1;
                }
            }
        }

        private void CheckUrlWordsInRun(Run run)
        {
            var text = run.Text;

            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && text[i - 1] == '.')
                {
                    if (i >= 4)
                    {
                        string word = text.Substring(i - 4, 4);
                        if (word == "env." || word == "ext.")
                        {
                            var tagPrefix = new TextHighlightTag();
                            tagPrefix.StartPosition = run.ContentStart.GetPositionAtOffset(i - 4, LogicalDirection.Forward);
                            tagPrefix.EndPosition = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Backward);
                            
                            if(word == "env.")
                            {
                                environmentTags.Add(tagPrefix);
                            }
                            else
                            {
                                extensionTags.Add(tagPrefix);
                            }

                            // get following word
                            for (int j = i; j <= text.Length; j++)
                            {
                                if (text[i] == ' ')
                                {
                                    break;
                                }
                                var settingsWord = text.Substring(i, j - i);
                                if (settingsWord.Length == 0)
                                {
                                    continue;
                                }
                                if (intellisenseService.CheckKey(word, settingsWord))
                                {
                                    var tag = new TextHighlightTag();
                                    tag.StartPosition = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Forward);
                                    tag.EndPosition = run.ContentStart.GetPositionAtOffset(i + (j - i), LogicalDirection.Backward);
                                    if (word == "env.")
                                    {
                                        environmentSettings.Add(tag);
                                    }
                                    else
                                    {
                                        requestExtensionTags.Add(tag);
                                    }
                                    
                                    break;
                                }
                                if (j == text.Length)
                                {
                                    var tag = new TextHighlightTag
                                                  {
                                                      StartPosition = run.ContentStart.GetPositionAtOffset(i,
                                                                                                           LogicalDirection
                                                                                                               .Forward),
                                                      EndPosition = run.ContentStart.GetPositionAtOffset(i + (j - i),
                                                                                                         LogicalDirection
                                                                                                             .
                                                                                                             Backward)
                                                  };
                                    if (word == "env.")
                                    {
                                        missingEnvironmentTags.Add(tag);
                                    }
                                    else
                                    {
                                        missingRequestExtensionTags.Add(tag);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UrlTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RequestUrl.Document == null) return;
            httpRequestViewModel.SetRequestUrl(new TextRange(RequestUrl.Document.ContentStart, RequestUrl.Document.ContentEnd).Text);
            var documentRange = new TextRange(RequestUrl.Document.ContentStart, RequestUrl.Document.ContentEnd);
            documentRange.ClearAllProperties();

            TextPointer navigator = RequestUrl.Document.ContentStart;
            while (navigator.CompareTo(RequestUrl.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    CheckUrlWordsInRun((Run)navigator.Parent);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            FormatUrl();
        }

        private void BodyTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RequestBody.Document == null) return;
            httpRequestViewModel.SetRequestBody(new TextRange(RequestBody.Document.ContentStart, RequestBody.Document.ContentEnd).Text);

            var documentRange = new TextRange(RequestBody.Document.ContentStart, RequestBody.Document.ContentEnd);
            documentRange.ClearAllProperties();

            TextPointer navigator = RequestBody.Document.ContentStart;
            while (navigator.CompareTo(RequestBody.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    CheckUrlWordsInRun((Run)navigator.Parent);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            FormatBody();
        }
    }
}
