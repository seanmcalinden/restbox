using System;
using System.Collections.Generic;
using System.Threading;
using Fiddler;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class ProxyService : IProxyService
    {
        static Proxy oSecureEndpoint;
        static string sSecureEndpointHostname = "localhost";
        static int iSecureEndpointPort = 7777;
        private bool started;

        private List<HttpRequestItem> interceptors;

        public ProxyService()
        {
            started = false;
            interceptors = new List<HttpRequestItem>();
        }

        public void AddInterceptor(HttpRequestItem httpRequestItemInterceptor)
        {
            interceptors.Add(httpRequestItemInterceptor);
        }

        public void RemoveInterceptor(HttpRequestItem httpRequestItemInterceptor)
        {
            interceptors.Remove(httpRequestItemInterceptor);

            if (interceptors.Count == 0)
            {
                Shutdown();
            }
        }

        public void Start(int port = 0)
        {
            if (started)
            {
                return;
            }
            var oFCSF = FiddlerCoreStartupFlags.Default;
            oFCSF = (oFCSF & ~FiddlerCoreStartupFlags.DecryptSSL);
            CONFIG.IgnoreServerCertErrors = true;
            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

            if (!CertMaker.rootCertExists()) { if (!CertMaker.createRootCert()) { throw new Exception("Unable to create cert for FiddlerCore."); } }
            if (!CertMaker.rootCertIsTrusted())
            {
                if (!CertMaker.trustRootCert())
                {
                    throw new Exception("Unable to install FiddlerCore's cert.");
                }
            }

            FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
            {
                if (oS.oRequest.headers.HTTPMethod != "CONNECT")
                {
                    foreach (var httpRequestItem in interceptors)
                    {
                        if (oS.fullUrl == httpRequestItem.Url && oS.oRequest.headers.HTTPMethod == httpRequestItem.Verb)
                        {
                            oS.utilCreateResponseAndBypassServer();
                            oS.oResponse.headers.HTTPResponseStatus = "200 Ok";
                            oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                            oS.oResponse["Cache-Control"] = "private, max-age=0";
                            oS.utilSetResponseBody(httpRequestItem.Body);
                            oS.bBufferResponse = true;
                        }
                    }
                }
            };

            FiddlerApplication.Startup(8877, oFCSF);
            started = true;
            oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);
        }

        public void Shutdown()
        {
            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
            FiddlerApplication.Shutdown();
            Thread.Sleep(500);
        }
    }
}
