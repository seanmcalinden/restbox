using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public class HttpRequestViewModel : ViewModelBase<HttpRequestViewModel>
    {
        private readonly IHttpRequestService httpRequestService;
        private readonly IEventAggregator eventAggregator;
        private readonly IFileService fileService;

        public HttpRequestViewModel(IHttpRequestService httpRequestService, IEventAggregator eventAggregator, IFileService fileService)
        {
            this.httpRequestService = httpRequestService;
            this.eventAggregator = eventAggregator;
            this.fileService = fileService;
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
            XmlResponseTabVisibility = Visibility.Collapsed;
            ImageResponseTabVisibility = Visibility.Collapsed;
            RawResponseTabVisibility = Visibility.Collapsed;
            ResponseInfoVisibility = Visibility.Hidden;
            ProgressBarVisibility = Visibility.Hidden;
            IntellisenseItems = new ObservableCollection<string>();
            eventAggregator.GetEvent<MakeRequestEvent>().Subscribe(MakeRequest);
        }

        private void MakeRequest(bool obj)
        {
            ExecuteRequest();
        }

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
            set { headerResponse = value; OnPropertyChanged(x => x.HeaderResponse); }
        }


        private string imageResponse;
        public string ImageResponse
        {
            get { return imageResponse; }
            set { imageResponse = value; OnPropertyChanged(x => x.ImageResponse); }
        }

        private string jsonResponse;
        public string JsonResponse
        {
            get { return jsonResponse; }
            set { jsonResponse = value; OnPropertyChanged(x => x.JsonResponse); }
        }

        private string xmlResponse;
        public string XmlResponse
        {
            get { return xmlResponse; }
            set { xmlResponse = value; OnPropertyChanged(x => x.XmlResponse); }
        }

        private string rawResponse;
        public string RawResponse
        {
            get { return rawResponse; }
            set { rawResponse = value; OnPropertyChanged(x => x.RawResponse); }
        }

        private string responseInfo;
        public string ResponseInfo
        {
            get { return responseInfo; }
            set { responseInfo = value; OnPropertyChanged(x => x.ResponseInfo); }
        }

        private BitmapImage imageSource;
        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set { imageSource = value; OnPropertyChanged(x => x.ImageSource); }
        }

        private Visibility responseTabVisibility;
        public Visibility ResponseTabVisibility
        {
            get { return responseTabVisibility; }
            set { responseTabVisibility = value; OnPropertyChanged(x => x.ResponseTabVisibility); }
        }

        private bool responseTabSelected;
        public bool ResponseTabSelected
        {
            get { return responseTabSelected; }
            set { responseTabSelected = value; OnPropertyChanged(x => x.ResponseTabSelected); }
        }

        private Visibility responseInfoVisibility;
        public Visibility ResponseInfoVisibility
        {
            get { return responseInfoVisibility; }
            set { responseInfoVisibility = value; OnPropertyChanged(x => x.ResponseInfoVisibility); }
        }

        private Visibility headerResponseTabVisibility;
        public Visibility HeaderResponseTabVisibility
        {
            get { return headerResponseTabVisibility; }
            set { headerResponseTabVisibility = value; OnPropertyChanged(x => x.HeaderResponseTabVisibility); }
        }

        private Visibility jsonResponseTabVisibility;
        public Visibility JsonResponseTabVisibility
        {
            get { return jsonResponseTabVisibility; }
            set { jsonResponseTabVisibility = value; OnPropertyChanged(x => x.JsonResponseTabVisibility); }
        }

        private bool jsonResponseTabSelected;
        public bool JsonResponseTabSelected
        {
            get { return jsonResponseTabSelected; }
            set { jsonResponseTabSelected = value; OnPropertyChanged(x => x.JsonResponseTabSelected); }
        }

        private Visibility xmlResponseTabVisibility;
        public Visibility XmlResponseTabVisibility
        {
            get { return xmlResponseTabVisibility; }
            set { xmlResponseTabVisibility = value; OnPropertyChanged(x => x.XmlResponseTabVisibility); }
        }

        private bool xmlResponseTabSelected;
        public bool XmlResponseTabSelected
        {
            get { return xmlResponseTabSelected; }
            set { xmlResponseTabSelected = value; OnPropertyChanged(x => x.XmlResponseTabSelected); }
        }

        private Visibility rawResponseTabVisibility;
        public Visibility RawResponseTabVisibility
        {
            get { return rawResponseTabVisibility; }
            set { rawResponseTabVisibility = value; OnPropertyChanged(x => x.RawResponseTabVisibility); }
        }

        private bool rawResponseTabSelected;
        public bool RawResponseTabSelected
        {
            get { return rawResponseTabSelected; }
            set { rawResponseTabSelected = value; OnPropertyChanged(x => x.RawResponseTabSelected); }
        }

        private Visibility imageResponseTabVisibility;
        public Visibility ImageResponseTabVisibility
        {
            get { return imageResponseTabVisibility; }
            set { imageResponseTabVisibility = value; OnPropertyChanged(x => x.ImageResponseTabVisibility); }
        }

        private bool imageResponseTabSelected;
        public bool ImageResponseTabSelected
        {
            get { return imageResponseTabSelected; }
            set { imageResponseTabSelected = value; OnPropertyChanged(x => x.ImageResponseTabSelected); }
        }

        private string requestErrorMessage;
        public string RequestErrorMessage
        {
            get { return requestErrorMessage; }
            set { requestErrorMessage = value; OnPropertyChanged(x => x.RequestErrorMessage); }
        }

        private Visibility progressBarVisibility;
        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set { 
                progressBarVisibility = value; 
                OnPropertyChanged(x => x.ProgressBarVisibility); 
            }
        }

        private bool isProgressBarEnabled;
        public bool IsProgressBarEnabled
        {
            get { return isProgressBarEnabled; }
            set { 
                isProgressBarEnabled = value; 
                OnPropertyChanged(x => x.IsProgressBarEnabled); 
                if(value)
                {
                    ProgressBarVisibility = Visibility.Visible;
                }
                else
                {
                    ProgressBarVisibility = Visibility.Hidden;
                }
            }
        }

        public ICommand ExecuteHttpRequestCommand
        {
            get
            {
                return new DelegateCommand(ExecuteRequest);
            }
        }

        private void ExecuteRequest()
        {
            try
            {
                IsProgressBarEnabled = true;
                RequestErrorMessage = null;
                var requestEnvironment = ServiceLocator.Current.GetInstance<RequestEnvironmentsViewModel>();
                var requestEnvironmentSettings = new List<RequestEnvironmentSetting>();
                if(requestEnvironment.SelectedRequestEnvironment != null)
                {
                    var requestEnvironmentSettingFile =
                    fileService.Load<RequestEnvironmentSettingFile>(fileService.GetFilePath(Solution.Current.FilePath,
                                                                      requestEnvironment.SelectedRequestEnvironment.RelativeFilePath));

                    requestEnvironmentSettings = requestEnvironmentSettingFile.RequestEnvironmentSettings;
                }
                Task.Factory.StartNew(() => httpRequestService.ExecuteRequest(this, requestEnvironmentSettings));
            }
            catch (Exception ex)
            {
                RequestErrorMessage = GetBestErrorMessage(ex);
                IsProgressBarEnabled = false;
            }
        }
        
        private string ReplaceEnvironmentTokens(string value, List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            if (requestEnvironmentSettings == null || requestEnvironmentSettings.Count == 0)
            {
                return value;
            }

            foreach (var requestEnvironmentSetting in requestEnvironmentSettings)
            {
                value = value.Replace("env." + requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
            }
            return value;
        }

        public void ShowRequestError(string errorMessage)
        {
            IsProgressBarEnabled = false;
            eventAggregator.GetEvent<ShowErrorEvent>().Publish(new KeyValuePair<string, string>("Request Error", errorMessage));
        }

        public void DisplayHttpResponse(List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            IsProgressBarEnabled = false;
            if (ResponseHeaders == null)
            {
                return;
            }

            ResponseTabVisibility = Visibility.Visible;
            ResponseTabSelected = true;

            ResponseInfo = ReplaceEnvironmentTokens(GetResponseInfo(), requestEnvironmentSettings);
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
                    bitmapImage.DecodePixelWidth = int.Parse(Math.Round(bitmapImage.Width, 0).ToString(CultureInfo.InvariantCulture));
                    bitmapImage.DecodePixelHeight = int.Parse(Math.Round(bitmapImage.Height, 0).ToString(CultureInfo.InvariantCulture));
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

        public string RemoveScriptTags(string content)
        {
            var regex = new Regex(@"<script[^>]*>.*?<\/script>", RegexOptions.Singleline);
            return regex.Replace(content, string.Empty);
        }

        private string GetResponseInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine(RequestVerb.Content + " " + RequestUrl);
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

        private static string IndentXMLString(string xml)
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
    }
}
