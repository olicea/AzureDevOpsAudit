using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace auditlog
{
    /// <summary>
    /// uses the Azure DevOps client
    /// </summary>
    internal class AzureDevOpsAuditLogProvider : IAuditLogProvider, IDisposable
    {
        public string Source => auditLogSettings.Url;

        private AuditLogSettings auditLogSettings;

        private AuditHttpClient httpClient;
        private AuditHttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    //Prompt user for credential
                    VssConnection connection = new VssConnection(new Uri(auditLogSettings.Url), new VssBasicCredential(string.Empty, auditLogSettings.PAT));

                    //create http client and query for results
                    httpClient = connection.GetClient<AuditHttpClient>();
                }
                return httpClient;
            }
        }

        public AzureDevOpsAuditLogProvider(AuditLogSettings settings)
        {
            auditLogSettings = settings;
        }

        public async Task<AuditLogQueryResult> QueryLogAsync(int batchSize, string continuationToken)
        {
            return await HttpClient.QueryLogAsync(batchSize: batchSize, continuationToken: continuationToken);
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}