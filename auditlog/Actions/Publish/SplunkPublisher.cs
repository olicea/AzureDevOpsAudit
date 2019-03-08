using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;
using Newtonsoft.Json;

namespace auditlog
{
    public class SplunkPublisher : AuditLogPublisher, IDisposable
    {
        private HttpClient httpClient;
        private HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    httpClient = new HttpClient(SplunkClientHandler);
                }

                return httpClient;
            }
        }

        private HttpClientHandler httpClientHandler;

        /// <summary>
        /// Return a HttpClientHandler with a custom certificate validation callback since HttpEventCollector doesn't return a valid certificate.
        /// </summary>
        /// <returns>HttpClientHandler</returns>
        private HttpClientHandler SplunkClientHandler
        {
            get
            {
                if (httpClientHandler == null)
                {
                    httpClientHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) =>
                        {
                            if (sslPolicyErrors == SslPolicyErrors.None)
                            {
                                return true;
                            }

                            return certificate is X509Certificate2 cert
                                    && (cert.Issuer?.IndexOf(c_splunkOnPremCN, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        cert.Issuer?.IndexOf(c_splunkHostedCN, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    };
                }

                return httpClientHandler;
            }
        }

        public SplunkPublisher(PublishTargetSettings settings) : base(settings)
        {
        }

        protected override async Task SendToProvider(List<DecoratedAuditLogEntry> entries)
        {
            HttpRequestMessage message = GetRequestMessage(entries);
            var result = await HttpClient.SendAsync(message);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"The response failed, {result}");
            }
        }

        private HttpRequestMessage GetRequestMessage(List<DecoratedAuditLogEntry> entries)
        {
            StringBuilder payload = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(payload))
            {
                foreach (DecoratedAuditLogEntry e in entries)
                {
                    // get time since Unix epoch
                    double epochTime = (e.Timestamp - s_unixEpoch).TotalSeconds;

                    var eventWrapper = new
                    {
                        @event = e,
                        time = epochTime.ToString(c_timeFormat)
                    };

                    JsonSerializer.CreateDefault().Serialize(stringWriter, eventWrapper);
                }
            }

            // Ensure the url contains the event collector url
            string url = Settings.Url.EndsWith(c_eventCollectorPath) ? Settings.Url : Settings.Url + c_eventCollectorPath;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(c_schemeAuthorizationHeader, Settings.Token);
            request.Content = new StringContent(payload.ToString());

            return request;
        }

        void IDisposable.Dispose()
        {
            Debug.Assert(EventsToPublish.Any(), $"There are undelivered events on {PublisherName}");

            httpClient?.Dispose();
            httpClientHandler?.Dispose();
        }

        public override string PublisherName => SplunkPublisherName ;
        public static string SplunkPublisherName => "Splunk";
        private const string c_splunkOnPremCN = "CN=SplunkCommonCA";
        private const string c_splunkHostedCN = "CN=Splunk Cloud Certificate Authority";
        private const string c_timeFormat = "#.000";
        private const string c_sendEventToSplunk = "SendEventToSplunk";
        private const string c_schemeAuthorizationHeader = "Splunk";
        private const string c_eventCollectorPath = "/services/collector/event";

        private static readonly DateTime s_unixEpoch = new DateTime(1970, 1, 1);
    }
}