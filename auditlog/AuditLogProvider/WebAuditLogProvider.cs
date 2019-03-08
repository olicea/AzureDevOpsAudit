using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;
using Newtonsoft.Json;

namespace auditlog
{
    /// <summary>
    /// Implements the logic to access the Azure DevOps Log using web calls
    /// 
    /// https://docs.microsoft.com/en-us/rest/api/vsts/?view=vsts-rest-5.0 
    /// 
    /// </summary>
    internal class WebAuditLogProvider : IAuditLogProvider, IDisposable
    {
        private readonly IAuditLogSettingsProvider SettingProvider;
        private AuditLogSettings auditLogSettings;

        public string Source => auditLogSettings.Url;

        public WebAuditLogProvider()
        {
        }

        private HttpClient httpClient;
        private HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    httpClient = new HttpClient();

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", auditLogSettings.PAT))));
                }
                return httpClient;
            }
        }

        public WebAuditLogProvider(IAuditLogSettingsProvider settingProvider)
        {
            SettingProvider = settingProvider;
        }

        public async Task<AuditLogQueryResult> QueryLogAsync(int batchSize, string continuationToken)
        {
            auditLogSettings = await SettingProvider.GetSettingsAsync();

            using (HttpResponseMessage response = await HttpClient.GetAsync(auditLogSettings.Url + "/_api/auditlog/querylog"))
            {
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AuditLogQueryResult>(responseBody);
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}