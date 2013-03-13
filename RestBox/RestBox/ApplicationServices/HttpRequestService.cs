using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using RestBox.Domain.Entities;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class HttpRequestService : IHttpRequestService
    {
        private readonly IFileService fileService;

        public HttpRequestService(IFileService fileService)
        {
            this.fileService = fileService;
        }

        public void ExecuteRequest(HttpRequestViewModel httpRequestViewModel, List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            HttpResponseMessage httpResponseMessage;
            DateTime startTime;
            DateTime completedTime;
            using (var client = new HttpClient())
            {
                var httpRequestMessage = new HttpRequestMessage();
                httpResponseMessage = null;
                completedTime = DateTime.UtcNow;

                httpRequestMessage.RequestUri = new Uri(ReplaceTokensTokens(httpRequestViewModel.RequestUrl, requestEnvironmentSettings));
                httpRequestMessage.Method = GetHttpMethod(httpRequestViewModel.RequestVerbString);
                var headerItems = GetHeaderItems(ReplaceTokensTokens(httpRequestViewModel.RequestHeaders, requestEnvironmentSettings));
                SetContentHeadersAndBody(httpRequestMessage, headerItems, httpRequestViewModel.RequestVerbString, ReplaceTokensTokens(httpRequestViewModel.RequestBody, requestEnvironmentSettings));
                SetHeaders(httpRequestMessage, headerItems);

                startTime = DateTime.Now;
                try
                {
                    httpResponseMessage = client.SendAsync(httpRequestMessage).Result;
                    completedTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                  new Action<string>(httpRequestViewModel.ShowRequestError), GetBestErrorMessage(ex));
                    return;
                }
              
            }

            httpRequestViewModel.ResponseStatusCode = (int) httpResponseMessage.StatusCode;
            httpRequestViewModel.ResponseReasonPhrase = httpResponseMessage.ReasonPhrase;
            httpRequestViewModel.ResponseContentType = GetContentType(httpResponseMessage);
            httpRequestViewModel.ResponseBody = SetContent(httpResponseMessage);
            httpRequestViewModel.ResponseHeaders = GetResponseHeaders(httpResponseMessage);
            httpRequestViewModel.RequestStart = startTime.ToShortDateString() + " " + startTime.ToLongTimeString();
            httpRequestViewModel.ResponseReceived = completedTime.ToShortDateString() + " " +
                                                    completedTime.ToLongTimeString();
            httpRequestViewModel.RequestTime =
                Math.Round((completedTime - startTime).TotalMinutes, 4).ToString(CultureInfo.InvariantCulture) + " secs";

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                       new Action<List<RequestEnvironmentSetting>>(httpRequestViewModel.DisplayHttpResponse), requestEnvironmentSettings);
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

        private string ReplaceTokensTokens(string value, List<RequestEnvironmentSetting> requestEnvironmentSettings)
        {
            if(requestEnvironmentSettings == null || requestEnvironmentSettings.Count == 0)
            {
                return value;
            }

            foreach (var requestEnvironmentSetting in requestEnvironmentSettings)
            {
                value = value.Replace("env." + requestEnvironmentSetting.Setting, requestEnvironmentSetting.SettingValue);
            }



            foreach (var requestExtensionFilePath in Solution.Current.RequestExtensionsFilePaths)
            {
                var extensionPath = fileService.GetFilePath(Solution.Current.FilePath, requestExtensionFilePath);
                var startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.FileName = extensionPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.RedirectStandardOutput = true;
                startInfo.Arguments = string.Empty;

                var fileName = Path.GetFileNameWithoutExtension(requestExtensionFilePath);

                var regex = new Regex(string.Format("ext.{0}", fileName));
                var matches = regex.Matches(value);

                foreach (Match match in matches)
                {
                    var endOfWordIndex = match.Index + fileName.Length + 4;
                    var characters = new StringBuilder();
                    foreach (var character in value.Substring(endOfWordIndex, value.Length - endOfWordIndex))
                    {
                        characters.Append(character);
                        if (character == ')')
                        {
                            break;
                        }
                    }

                    var argumentsGroup = characters.ToString();

                    var firstPart = value.Substring(0, match.Index);
                    var secondPart = value.Substring(endOfWordIndex + argumentsGroup.Length, value.Length - endOfWordIndex - argumentsGroup.Length);
                              
                    var argBuilder = new StringBuilder();
                    var cacheDurationBuilder = new StringBuilder();
                    bool includeArgs = false;
                    foreach(var arg in argumentsGroup.Reverse())
                    {
                        if(!includeArgs)
                        {
                            cacheDurationBuilder.Append(arg);
                        }

                        if (arg == ',')
                        {
                            includeArgs = true;
                        }

                        if(includeArgs)
                        {
                            argBuilder.Append(arg);
                        }
                    }

                    string cacheDuration = string.Empty;

                    foreach (var source in cacheDurationBuilder.ToString().Reverse())
                    {
                        cacheDuration += source;
                    }

                    cacheDuration = cacheDuration.Replace(",", string.Empty).Replace(")", string.Empty);

                    string args = string.Empty;

                    foreach (var source in argBuilder.ToString().Reverse())
                    {
                        args += source;
                    }

                    args = args.Replace("(\"", string.Empty).Replace("\",", string.Empty);

                    var cacheValue = HttpRequestExtensionCacheService.GetCacheValue(fileName, args);
                    if(cacheValue != null)
                    {
                        value = firstPart + cacheValue + secondPart;
                        continue;
                    }

                    startInfo.Arguments = args;
                    try
                    {
                        using (var exeProcess = Process.Start(startInfo))
                        {
                            using (var streamreader = exeProcess.StandardOutput)
                            {
                                var output = streamreader.ReadLine();
                                value = firstPart + output + secondPart;
                                if(cacheDuration != "0")
                                {
                                    HttpRequestExtensionCacheService.AddCacheItem(fileName, args, int.Parse(cacheDuration), output);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }

            return value;
        }

        private HttpMethod GetHttpMethod(string verb)
        {
            switch (verb)
            {
                case "GET":
                    return HttpMethod.Get;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE":
                    return HttpMethod.Delete;
                case "OPTIONS":
                    return HttpMethod.Options;
                case "HEAD":
                    return HttpMethod.Head;
                case "TRACE":
                    return HttpMethod.Trace;
            }
            throw new NotSupportedException(string.Format("The verb {0} is not supported", verb));
        }

        private List<HeaderItem> GetHeaderItems(string headers)
        {
            var headerItems = new List<HeaderItem>();

            if (headers == null)
            {
                return headerItems;
            }

            using (var sr = new StringReader(headers))
            {
                while (true)
                {
                    var headerLine = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(headerLine))
                    {
                        break;
                    }

                    var headerLineItems = headerLine.Split(':');
                    var key = headerLineItems[0].Trim();
                    var valueItems = headerLineItems[1].Trim().Split(',').Select(x => x.Trim()).ToArray();
                    headerItems.Add(new HeaderItem { Key = key, Values = valueItems });
                }
            }
            return headerItems;
        }

        private void SetHeaders(HttpRequestMessage httpRequestMessage, List<HeaderItem> headerItems)
        {
            foreach (var headerItem in headerItems.Where(x => x.IsContentHeader == false))
            {
                httpRequestMessage.Headers.Add(headerItem.Key, headerItem.Values);
            }
        }

        private object SetContent(HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.Content.Headers.ContentType == null)
            {
                return null;
            }

            if (httpResponseMessage.Content.Headers.ContentType.MediaType.ToLower().Contains("image"))
            {
                return httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
            }

            return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        private void SetContentHeadersAndBody(HttpRequestMessage httpRequestMessage, List<HeaderItem> headerItems, string verb, string body)
        {
            if (verb.ToLower() == "get" || verb.ToLower() == "delete" || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            httpRequestMessage.Content = new StringContent(body);

            foreach (var headerItem in headerItems.Where(x => x.IsContentHeader))
            {
                if (headerItem.Key.ToLower() == "content-type")
                {
                    httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(headerItem.Values[0]);
                }
                else
                {
                    httpRequestMessage.Content.Headers.Add(headerItem.Key, headerItem.Values);
                }
            }
        }

        private string GetContentType(HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.Content.Headers.ContentType == null)
            {
                return null;
            }
            return httpResponseMessage.Content.Headers.ContentType.MediaType;
        }

        private string GetResponseHeaders(HttpResponseMessage httpResponseMessage)
        {
            var sb = new StringBuilder();

            if (httpResponseMessage.Content != null && httpResponseMessage.Content.Headers != null)
            {
                foreach (var header in httpResponseMessage.Content.Headers)
                {
                    sb.AppendLine(header.Key + ": " + GetHeaderValue(header.Value));
                }
            }

            if (httpResponseMessage.Headers == null)
            {
                return string.Empty;
            }

            foreach (var header in httpResponseMessage.Headers)
            {
                sb.AppendLine(header.Key + ": " + GetHeaderValue(header.Value));
            }
            return sb.ToString();
        }

        private string GetHeaderValue(IEnumerable<string> value)
        {
            var sb = new StringBuilder();

            foreach (var val in value)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(val);
            }

            return sb.ToString();
        }

    }
}
