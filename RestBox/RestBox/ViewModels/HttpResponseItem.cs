using System;
using System.Globalization;

namespace RestBox.ViewModels
{
    public class HttpResponseItem
    {
        public HttpResponseItem(int statusCode, string reasonPhrase, string headers, object body, string description, string contentType, DateTime requestStart, DateTime responseReceived, Double totalRequestSeconds, HttpRequestItem callingRequest)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            Body = body;
            Description = description;
            ContentType = contentType;
            RequestStart = requestStart;
            ResponseReceived = responseReceived;
            TotalRequestSeconds = totalRequestSeconds;
            CallingRequest = callingRequest;
        }

        public int StatusCode { get; private set; }
        public string ReasonPhrase { get; private set; }
        public string Headers { get; private set; }
        public object Body { get; private set; }
        public string Description { get; set; }
        public string ContentType { get; private set; }
        public DateTime RequestStart { get; private set; }
        public string RequestStartString {
            get { return RequestStart.Hour + ":" + RequestStart.Minute + ":" + PrintSecond(RequestStart.Second); } 
        }

        public DateTime ResponseReceived { get; private set; }
        public string ResponseReceivedString
        {
            get { return ResponseReceived.Hour + ":" + ResponseReceived.Minute + ":" + PrintSecond(ResponseReceived.Second); } 
        }
        public double TotalRequestSeconds { get; private set; }
        public string TotalRequestSecondsString
        {
            get { return Math.Round(TotalRequestSeconds, 4).ToString(CultureInfo.InvariantCulture) + " secs"; }
        }
        public HttpRequestItem CallingRequest { get; private set; }

        private string PrintSecond(int seconds)
        {
            if (seconds < 10)
            {
                return "0" + seconds;
            }
            return seconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}