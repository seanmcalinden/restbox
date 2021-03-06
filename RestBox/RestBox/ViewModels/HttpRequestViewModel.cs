﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public class HttpRequestViewModel : ViewModelBase<HttpRequestViewModel>, ISave
    {
        #region Declarations

        private readonly IHttpRequestService httpRequestService;
        private readonly IEventAggregator eventAggregator;
        private readonly IFileService fileService;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly KeyGesture saveKeyGesture;
        private readonly KeyGesture runHttpRequestKeyGesture; 

        #endregion

        #region Constructor

        public HttpRequestViewModel(IHttpRequestService httpRequestService, IEventAggregator eventAggregator,
                                    IFileService fileService, IMainMenuApplicationService mainMenuApplicationService,
                                    IJsonSerializer jsonSerializer)
        {
            this.httpRequestService = httpRequestService;
            this.eventAggregator = eventAggregator;
            this.fileService = fileService;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.jsonSerializer = jsonSerializer;
            RequestVerbs = new ObservableCollection<ComboBoxItem>
                               {
                                   new ComboBoxItem {Content = "GET", IsSelected = true},
                                   new ComboBoxItem {Content = "POST"},
                                   new ComboBoxItem {Content = "PUT"},
                                   new ComboBoxItem {Content = "DELETE"},
                                   new ComboBoxItem {Content = "OPTIONS"},
                                   new ComboBoxItem {Content = "HEAD"},
                                   new ComboBoxItem {Content = "TRACE"}
                               };
            RequestVerb = RequestVerbs[0];
            ResponseTabVisibility = Visibility.Collapsed;
            HeaderResponseTabVisibility = Visibility.Collapsed;
            JsonResponseTabVisibility = Visibility.Collapsed;
            WebBrowserTabVisibility = Visibility.Collapsed;
            XmlResponseTabVisibility = Visibility.Collapsed;
            TreeResponseTabVisibility = Visibility.Collapsed;
            ImageResponseTabVisibility = Visibility.Collapsed;
            RawResponseTabVisibility = Visibility.Collapsed;
            ResponseInfoVisibility = Visibility.Hidden;
            ProgressBarVisibility = Visibility.Hidden;
            StartButtonVisibility = Visibility.Visible;
            CancelButtonVisibility = Visibility.Hidden;
            IntellisenseItems = new ObservableCollection<string>();
            RequestUriColor = Brushes.White;
            RequestHeadersColor = Brushes.White;
            RequestBodyColor = Brushes.White;
            saveKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            runHttpRequestKeyGesture = new KeyGesture(Key.F5);
            eventAggregator.GetEvent<AddHttpRequestMenuItemsEvent>().Subscribe(AddHttpRequestMenuItems);
        } 

        #endregion

        #region Properties

        public ObservableCollection<ComboBoxItem> RequestVerbs { get; set; }
        public ObservableCollection<string> IntellisenseItems { get; set; }

        public bool IsDirty { get; set; }

        private string requestUrl;
        public string RequestUrl
        {
            get { return requestUrl; }
            set
            {
                requestUrl = value;
                eventAggregator.GetEvent<UpdateRequestUrlEvent>().Publish(this);
                OnPropertyChanged(x => x.RequestUrl);
            }
        }

        private ComboBoxItem requestVerb;
        public ComboBoxItem RequestVerb
        {
            get { return requestVerb; }
            set
            {
                requestVerb = value;
                OnPropertyChanged(x => x.RequestVerb);
                RequestVerbString = value.Content.ToString();
            }
        }

        public string RequestVerbString { get; set; }

        private string requestHeaders;
        public string RequestHeaders
        {
            get { return requestHeaders; }
            set
            {
                requestHeaders = value;
                eventAggregator.GetEvent<UpdateRequestHeadersEvent>().Publish(this);
                OnPropertyChanged(x => x.RequestHeaders);
            }
        }

        private string requestBody;

        public string RequestBody
        {
            get { return requestBody; }
            set
            {
                requestBody = value;
                eventAggregator.GetEvent<UpdateRequestBodyEvent>().Publish(this);
                OnPropertyChanged(x => x.RequestBody);
            }
        }

        private SolidColorBrush requestUriColor;

        public SolidColorBrush RequestUriColor
        {
            get { return requestUriColor; }
            set
            {
                requestUriColor = value;
                OnPropertyChanged(x => x.RequestUriColor);
            }
        }

        private SolidColorBrush requestHeadersColor;

        public SolidColorBrush RequestHeadersColor
        {
            get { return requestHeadersColor; }
            set
            {
                requestHeadersColor = value;
                OnPropertyChanged(x => x.RequestHeadersColor);
            }
        }

        private SolidColorBrush requestBodyColor;

        public SolidColorBrush RequestBodyColor
        {
            get { return requestBodyColor; }
            set
            {
                requestBodyColor = value;
                OnPropertyChanged(x => x.RequestBodyColor);
            }
        }

        public int ResponseStatusCode { get; set; }
        public string ResponseContentType { get; set; }
        public string ResponseHeaders { get; set; }

        public object ResponseBody { get; set; }
        public string ResponseBodyType { get; set; }
        public string ResponseReasonPhrase { get; set; }
        public string RequestStart { get; set; }
        public string ResponseReceived { get; set; }
        public string RequestTime { get; set; }

        private string headerResponse;

        public string HeaderResponse
        {
            get { return headerResponse; }
            set
            {
                headerResponse = value;
                OnPropertyChanged(x => x.HeaderResponse);
                eventAggregator.GetEvent<UpdateResponseHeadersEvent>().Publish(this);
            }
        }


        private string imageResponse;

        public string ImageResponse
        {
            get { return imageResponse; }
            set
            {
                imageResponse = value;
                OnPropertyChanged(x => x.ImageResponse);
            }
        }

        private string jsonResponse;

        public string JsonResponse
        {
            get { return jsonResponse; }
            set
            {
                jsonResponse = value;
                OnPropertyChanged(x => x.JsonResponse);
                eventAggregator.GetEvent<UpdateJsonResponseEvent>().Publish(this);
            }
        }

        private string xmlResponse;

        public string XmlResponse
        {
            get { return xmlResponse; }
            set
            {
                xmlResponse = value;
                OnPropertyChanged(x => x.XmlResponse);
                eventAggregator.GetEvent<UpdateXmlResponseEvent>().Publish(this);
            }
        }

        private string rawResponse;

        public string RawResponse
        {
            get { return rawResponse; }
            set
            {
                rawResponse = value;
                OnPropertyChanged(x => x.RawResponse);
                eventAggregator.GetEvent<UpdateResponseRawBodyEvent>().Publish(this);
            }
        }

        private string responseInfo;

        public string ResponseInfo
        {
            get { return responseInfo; }
            set
            {
                responseInfo = value;
                OnPropertyChanged(x => x.ResponseInfo);
            }
        }

        private BitmapImage imageSource;

        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set
            {
                imageSource = value;
                OnPropertyChanged(x => x.ImageSource);
            }
        }

        private Visibility responseTabVisibility;

        public Visibility ResponseTabVisibility
        {
            get { return responseTabVisibility; }
            set
            {
                responseTabVisibility = value;
                OnPropertyChanged(x => x.ResponseTabVisibility);
            }
        }

        private bool responseTabSelected;

        public bool ResponseTabSelected
        {
            get { return responseTabSelected; }
            set
            {
                responseTabSelected = value;
                OnPropertyChanged(x => x.ResponseTabSelected);
            }
        }

        private Visibility responseInfoVisibility;

        public Visibility ResponseInfoVisibility
        {
            get { return responseInfoVisibility; }
            set
            {
                responseInfoVisibility = value;
                OnPropertyChanged(x => x.ResponseInfoVisibility);
            }
        }

        private Visibility headerResponseTabVisibility;

        public Visibility HeaderResponseTabVisibility
        {
            get { return headerResponseTabVisibility; }
            set
            {
                headerResponseTabVisibility = value;
                OnPropertyChanged(x => x.HeaderResponseTabVisibility);
            }
        }

        private Visibility jsonResponseTabVisibility;

        public Visibility JsonResponseTabVisibility
        {
            get { return jsonResponseTabVisibility; }
            set
            {
                jsonResponseTabVisibility = value;
                OnPropertyChanged(x => x.JsonResponseTabVisibility);
            }
        }

        private Visibility webBrowserTabVisibility;
        public Visibility WebBrowserTabVisibility
        {
            get { return webBrowserTabVisibility; }
            set
            {
                webBrowserTabVisibility = value;
                OnPropertyChanged(x => x.WebBrowserTabVisibility);
            }
        }
        

        private bool jsonResponseTabSelected;

        public bool JsonResponseTabSelected
        {
            get { return jsonResponseTabSelected; }
            set
            {
                jsonResponseTabSelected = value;
                OnPropertyChanged(x => x.JsonResponseTabSelected);
            }
        }

        private Visibility xmlResponseTabVisibility;

        public Visibility XmlResponseTabVisibility
        {
            get { return xmlResponseTabVisibility; }
            set
            {
                xmlResponseTabVisibility = value;
                OnPropertyChanged(x => x.XmlResponseTabVisibility);
            }
        }

        private bool xmlResponseTabSelected;

        public bool XmlResponseTabSelected
        {
            get { return xmlResponseTabSelected; }
            set
            {
                xmlResponseTabSelected = value;
                OnPropertyChanged(x => x.XmlResponseTabSelected);
            }
        }

        private Visibility treeResponseTabVisibility;
        public Visibility TreeResponseTabVisibility
        {
            get { return treeResponseTabVisibility; }
            set
            {
                treeResponseTabVisibility = value;
                OnPropertyChanged(x => x.TreeResponseTabVisibility);
            }
        }

        private bool treeResponseTabSelected;
        public bool TreeResponseTabSelected
        {
            get { return treeResponseTabSelected; }
            set
            {
                treeResponseTabSelected = value;
                OnPropertyChanged(x => x.TreeResponseTabSelected);
            }
        }

        private Visibility rawResponseTabVisibility;

        public Visibility RawResponseTabVisibility
        {
            get { return rawResponseTabVisibility; }
            set
            {
                rawResponseTabVisibility = value;
                OnPropertyChanged(x => x.RawResponseTabVisibility);
            }
        }

        private bool rawResponseTabSelected;

        public bool RawResponseTabSelected
        {
            get { return rawResponseTabSelected; }
            set
            {
                rawResponseTabSelected = value;
                OnPropertyChanged(x => x.RawResponseTabSelected);
            }
        }

        private Visibility imageResponseTabVisibility;

        public Visibility ImageResponseTabVisibility
        {
            get { return imageResponseTabVisibility; }
            set
            {
                imageResponseTabVisibility = value;
                OnPropertyChanged(x => x.ImageResponseTabVisibility);
            }
        }

        private bool imageResponseTabSelected;

        public bool ImageResponseTabSelected
        {
            get { return imageResponseTabSelected; }
            set
            {
                imageResponseTabSelected = value;
                OnPropertyChanged(x => x.ImageResponseTabSelected);
            }
        }

        private XmlDocument treeResponse;
        public XmlDocument TreeResponse
        {
            get { return treeResponse; }
            set
            {
                treeResponse = value;
                OnPropertyChanged(x => x.TreeResponse);
            }
        }

        private bool webBrowserTabSelected;

        public bool WebBrowserTabSelected
        {
            get { return webBrowserTabSelected; }
            set
            {
                webBrowserTabSelected = value;
                OnPropertyChanged(x => x.WebBrowserTabSelected);
            }
        }

        private string requestErrorMessage;

        public string RequestErrorMessage
        {
            get { return requestErrorMessage; }
            set
            {
                requestErrorMessage = value;
                OnPropertyChanged(x => x.RequestErrorMessage);
            }
        }

        private Visibility progressBarVisibility;

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                progressBarVisibility = value;
                OnPropertyChanged(x => x.ProgressBarVisibility);
            }
        }

        private bool isProgressBarEnabled;

        public bool IsProgressBarEnabled
        {
            get { return isProgressBarEnabled; }
            set
            {
                isProgressBarEnabled = value;
                OnPropertyChanged(x => x.IsProgressBarEnabled);
                if (value)
                {
                    ProgressBarVisibility = Visibility.Visible;
                }
                else
                {
                    ProgressBarVisibility = Visibility.Hidden;
                }
            }
        }

        private Visibility startButtonVisibility;
        public Visibility StartButtonVisibility
        {
            get { return startButtonVisibility; }
            set
            {
                startButtonVisibility = value;
                OnPropertyChanged(x => x.StartButtonVisibility);
            }
        }

        private Visibility cancelButtonVisibility;
        public Visibility CancelButtonVisibility
        {
            get { return cancelButtonVisibility; }
            set
            {
                cancelButtonVisibility = value;
                OnPropertyChanged(x => x.CancelButtonVisibility);
            }
        }

        #endregion

        #region EventHandlers

        private void AddHttpRequestMenuItems(HttpRequestViewModel httpRequestViewModel)
        {
            if (httpRequestViewModel != this)
            {
                return;
            }

            eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);

            var fileMenu = mainMenuApplicationService.Get("file");
            var saveHttpRequest = new MenuItem { Header = "Save Http Request", InputGestureText = "Ctrl+S" };
            var saveHttpRequestAs = new MenuItem { Header = "Save Http Request As..." };

            saveHttpRequest.Command = new DelegateCommand(SetupSaveRequest);
            saveHttpRequestAs.Command = new DelegateCommand(SetupSaveRequestAs);

            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequest, 3);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequestAs, 4);

            var requests = mainMenuApplicationService.Get("requests");
            var runRequestMenu = mainMenuApplicationService.GetChild(requests, "requestsRun");
            runRequestMenu.Command = MakeRequestCommand;
            runRequestMenu.IsEnabled = true;

            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = saveKeyGesture, Command = saveHttpRequest.Command });
            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = runHttpRequestKeyGesture, Command = MakeRequestCommand });
            eventAggregator.GetEvent<UpdateToolBarEvent>().Publish(new List<ToolBarItemData>
                {
                    new ToolBarItemData{ Command = saveHttpRequest.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.Save},
                    new ToolBarItemData{ Command = MakeRequestCommand, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.Run}
                });
        } 

        #endregion

        #region Commands

        public ICommand MakeRequestCommand
        {
            get { return new DelegateCommand(SetupRequest); }
        }
        
        public ICommand ExecuteHttpRequestCommand
        {
            get { return new DelegateCommand(ExecuteRequest); }
        }

        private ICommand cancelHttpRequestCommand;
        public ICommand CancelHttpRequestCommand
        {
            get { return cancelHttpRequestCommand; }
            set { cancelHttpRequestCommand = value; OnPropertyChanged(x => x.CancelHttpRequestCommand); }
        }

        #endregion

        #region Command Handlers

        private void SetupRequest()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
                                                                       {
                                                                           Action = MakeRequest,
                                                                           UserControlType = typeof(HttpRequest),
                                                                           DataContext = this
                                                                       });
        }

        private void MakeRequest(string contentId, object httpRequest)
        {
            if (((HttpRequest)httpRequest).DataContext == this)
            {
                ExecuteRequest();
            }
        }

        private void ExecuteRequest()
        {
            try
            {
                bool isValidRequest = true;
                if (string.IsNullOrWhiteSpace(RequestUrl))
                {
                    RequestUriColor = Brushes.Pink;
                    isValidRequest = false;
                }
                else
                {
                    RequestUriColor = Brushes.White;
                }
                if ((RequestVerbString == "POST" || RequestVerbString == "PUT") &&
                    string.IsNullOrWhiteSpace(RequestBody))
                {
                    RequestBodyColor = Brushes.MistyRose;
                }
                else
                {
                    RequestBodyColor = Brushes.White;
                }

                if (!isValidRequest)
                {
                    return;
                }

                StartButtonVisibility = Visibility.Hidden;
                CancelButtonVisibility = Visibility.Visible;
                IsProgressBarEnabled = true;
                RequestErrorMessage = null;
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

                var tokenSource = new CancellationTokenSource();

                CancelHttpRequestCommand = new DelegateCommand(tokenSource.Cancel);

                httpRequestService.BeginExecuteRequest(new HttpRequestItem
                    {
                        Url = RequestUrl,
                        Body = RequestBody,
                        Headers = RequestHeaders,
                        Verb = RequestVerbString
                    }, requestEnvironmentSettings, DisplayHttpResponse, ShowRequestError, tokenSource.Token);
            }
            catch (Exception ex)
            {
                RequestErrorMessage = GetBestErrorMessage(ex);
                IsProgressBarEnabled = false;
            }
        }

        private void SetupSaveRequestAs()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = SaveAs,
                UserControlType = typeof(HttpRequest),
                DataContext = this
            });
        }

        private void SetupSaveRequest()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = Save,
                UserControlType = typeof(HttpRequest),
                DataContext = this
            });
        }

        public void SaveAs(string id, object content)
        {
            if (((HttpRequest)content).DataContext != this)
            {
                return;
            }

            var httpRequest = content as HttpRequest;

            var httpRequestViewModel = httpRequest.DataContext as HttpRequestViewModel;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = SystemFileTypes.HttpRequest.FilterText,
                FileName = Path.GetFileName(httpRequest.FilePath) ?? SystemFileTypes.HttpRequest.UntitledFileName,
                Title = SystemFileTypes.HttpRequest.SaveAsTitle
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                var httpRequestFile = new HttpRequestItemFile
                {
                    Url = httpRequestViewModel.RequestUrl,
                    Verb = httpRequestViewModel.RequestVerb.Content.ToString(),
                    Headers = httpRequestViewModel.RequestHeaders,
                    Body = httpRequestViewModel.RequestBody
                };

                var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                                   saveFileDialog.FileName);

                if (!id.StartsWith("StandaloneNewItem"))
                {
                    var requestExists = Solution.Current.HttpRequestFiles.Any(x => x.Id == id);
                    if (!requestExists)
                    {
                        Solution.Current.HttpRequestFiles.Add(
                            new File { Id = id, RelativeFilePath = relativePath, Name = title });
                    }
                    else
                    {
                        var existingHttpRequestFile =
                            Solution.Current.HttpRequestFiles.First(x => x.Id == id);
                        existingHttpRequestFile.Name = title;
                        existingHttpRequestFile.RelativeFilePath = relativePath;
                    }

                    fileService.SaveSolution();

                    fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(httpRequestFile));

                    httpRequest.FilePath = relativePath;
                    eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                    {
                        Id = id,
                        Title = title
                    });
                    eventAggregator.GetEvent<UpdateHttpRequestFileItemEvent>().Publish(new File
                    {
                        Id = id,
                        Name = title,
                        RelativeFilePath =
                            relativePath
                    });
                }
                else
                {
                    httpRequest.FilePath = saveFileDialog.FileName;
                    eventAggregator.GetEvent<UpdateHttpRequestFileItemEvent>().Publish(new File
                    {
                        Id = id,
                        Name = title,
                        RelativeFilePath =
                            relativePath
                    });
                    eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                    {
                        Id = id,
                        Title = title
                    });
                    fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(httpRequestFile));
                }

                Keyboard.ClearFocus();
                eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
                IsDirty = false;
            }
        }

        public void Save(string id, object content)
        {
            if (!(content is HttpRequest))
            {
                return;
            }

            if (((HttpRequest)content).DataContext != this)
            {
                return;
            }


            var httpRequest = content as HttpRequest;

            var httpRequestViewModel = httpRequest.DataContext as HttpRequestViewModel;

            if (string.IsNullOrWhiteSpace(httpRequest.FilePath))
            {
                SaveAs(id, content);
                return;
            }

            var httpRequestFile = new HttpRequestItemFile
            {
                Url = httpRequestViewModel.RequestUrl,
                Verb = httpRequestViewModel.RequestVerb.Content.ToString(),
                Headers = httpRequestViewModel.RequestHeaders,
                Body = httpRequestViewModel.RequestBody
            };

            string filePath;

            if (!id.StartsWith("StandaloneNewItem"))
            {
                filePath = fileService.GetFilePath(Solution.Current.FilePath, httpRequest.FilePath);
            }
            else
            {
                filePath = httpRequest.FilePath;
            }

            fileService.SaveFile(filePath, jsonSerializer.ToJsonString(httpRequestFile));

            eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
            Keyboard.ClearFocus();
            httpRequestViewModel.IsDirty = false;
        }

        #endregion

        #region Public Methods

        public void SetRequestUrl(string requestUrlField)
        {
            requestUrl = requestUrlField;
        }

        public void SetRequestHeaders(string requestHeadersField)
        {
            requestHeaders = requestHeadersField;
        }

        public void SetRequestBody(string requestBodyField)
        {
            requestBody = requestBodyField;
        }

        public void ShowRequestError(string errorMessage)
        {
            IsProgressBarEnabled = false;
            CancelButtonVisibility = Visibility.Hidden;
            StartButtonVisibility = Visibility.Visible;
            eventAggregator.GetEvent<ShowErrorEvent>().Publish(new KeyValuePair<string, string>("Request Error",
                                                                                                errorMessage));
        }

        public void DisplayHttpResponse(Uri requestUri, List<RequestEnvironmentSetting> requestEnvironmentSettings, HttpResponseItem httpResponseItem)
        {
            RequestUrl = httpResponseItem.CallingRequest.Url;
            RequestVerb = RequestVerbs.First(x => x.Content.ToString().ToLower() == httpResponseItem.CallingRequest.Verb.ToLower());
            RequestHeaders = httpResponseItem.CallingRequest.Headers;
            RequestBody = httpResponseItem.CallingRequest.Body;

            ResponseContentType = httpResponseItem.ContentType;
            ResponseHeaders = httpResponseItem.Headers;
            ResponseBody = httpResponseItem.Body;
            ResponseStatusCode = httpResponseItem.StatusCode;
            ResponseReasonPhrase = httpResponseItem.ReasonPhrase;
            RequestStart = httpResponseItem.RequestStart.ToShortDateString() + " " + httpResponseItem.RequestStart.ToLongTimeString();
            ResponseReceived = httpResponseItem.ResponseReceived.ToShortDateString() + " " + httpResponseItem.ResponseReceived.ToLongTimeString();
            RequestTime = httpResponseItem.TotalRequestSeconds.ToString(CultureInfo.InvariantCulture) + " secs";
            DisplayHttpResponse(requestUri, requestEnvironmentSettings);

        }

        public string RemoveScriptTags(string content)
        {
            var regex = new Regex(@"<script[^>]*>.*?<\/script>", RegexOptions.Singleline);
            return regex.Replace(content, string.Empty);
        }

        #endregion

        #region Helpers

        private void DisplayHttpResponse(Uri requestUri, List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            StartButtonVisibility = Visibility.Visible;
            CancelButtonVisibility = Visibility.Hidden;
            IsProgressBarEnabled = false;
            if (ResponseHeaders == null)
            {
                return;
            }

            ResponseTabVisibility = Visibility.Visible;
            ResponseTabSelected = true;

            ResponseInfo = ReplaceEnvironmentTokens(GetResponseInfo(requestUri), requestEnvironmentSettings);
            HeaderResponse = ResponseHeaders;

            ResponseInfoVisibility = Visibility.Visible;
            HeaderResponseTabVisibility = Visibility.Visible;
            JsonResponseTabVisibility = Visibility.Collapsed;
            XmlResponseTabVisibility = Visibility.Collapsed;
            ImageResponseTabVisibility = Visibility.Collapsed;
            RawResponseTabVisibility = Visibility.Collapsed;

            JsonResponse = null;
            XmlResponse = null;
            ImageSource = null;
            RawResponse = null;

            if (ResponseContentType == null)
            {
                return;
            }

            if (ResponseContentType.ToLower().Contains("json"))
            {
                var bodyString = ResponseBody.ToString();
                JsonResponse = GetFormattedJson(bodyString);
                RawResponse = bodyString;
                JsonResponseTabVisibility = Visibility.Visible;
                RawResponseTabVisibility = Visibility.Visible;
                JsonResponseTabSelected = true;
                return;
            }

            if (ResponseContentType.ToLower().Contains("xml"))
            {
                var bodyString = ResponseBody.ToString();
                XmlResponse = IndentXMLString(bodyString);
                RawResponse = bodyString;
                XmlResponseTabVisibility = Visibility.Visible;
                RawResponseTabVisibility = Visibility.Visible;
                XmlResponseTabSelected = true;
                return;
            }

            if (ResponseContentType.ToLower().Contains("html"))
            {
                var bodyString = ResponseBody.ToString();
                RawResponse = bodyString;
                RawResponseTabVisibility = Visibility.Visible;
                RawResponseTabSelected = true;
                return;
            }

            if (ResponseContentType.ToLower().Contains("image"))
            {
                using (var memoryStream = new MemoryStream(ResponseBody as byte[]))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.DecodePixelWidth =
                        int.Parse(Math.Round(bitmapImage.Width, 0).ToString(CultureInfo.InvariantCulture));
                    bitmapImage.DecodePixelHeight =
                        int.Parse(Math.Round(bitmapImage.Height, 0).ToString(CultureInfo.InvariantCulture));
                    ImageSource = bitmapImage;
                    ImageResponseTabVisibility = Visibility.Visible;
                    ImageResponseTabSelected = true;
                }
                return;
            }

            RawResponse = ResponseBody.ToString();
            RawResponseTabVisibility = Visibility.Visible;
            RawResponseTabSelected = true;
        }

        private string ReplaceEnvironmentTokens(string value, List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            if (requestEnvironmentSettings == null || requestEnvironmentSettings.Count == 0)
            {
                return value;
            }

            foreach (var requestEnvironmentSetting in requestEnvironmentSettings)
            {
                value = Regex.Replace(value, "env." + requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue, RegexOptions.IgnoreCase);
            }
            return value;
        }

        private string GetResponseInfo(Uri requestUri)
        {
            var sb = new StringBuilder();
            sb.AppendLine(RequestVerb.Content + " " + requestUri);
            sb.AppendLine(ResponseStatusCode + " " + ResponseReasonPhrase);
            sb.AppendLine("Time Taken: " + RequestTime);
            sb.AppendLine("Request Start: " + RequestStart);
            sb.AppendLine("Response Received: " + ResponseReceived);
            return sb.ToString();
        }

        public string GetFormattedJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
        }

       private string IndentXMLString(string xml)
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var xtw = new XmlTextWriter(ms, Encoding.Unicode))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);
                    xtw.Formatting = System.Xml.Formatting.Indented;
                    doc.WriteContentTo(xtw);
                    xtw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var sr = new StreamReader(ms))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return string.Empty;
            }
        }

        private string GetBestErrorMessage(Exception ex)
        {
            var message = ex.ToString();
            var currentException = ex.InnerException;
            while (currentException != null)
            {
                message = currentException.Message;
                currentException = currentException.InnerException;
            }

            return message;
        } 

        #endregion
    }
}