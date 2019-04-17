using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;

namespace auditlog
{
    public class DownloadAction : ICommandAction
    {
        public string Name => "download";

        private IAuditLogProvider AuditLog { get; }

        public DownloadAction(IAuditLogProvider auditLog, string searchString)
        {
            PublishTargets = targets;
            AuditLog = auditLog;

            CreatePublishers();
        }

        public async Task PerformAsync()
        {
            Console.WriteLine($"Downloading the log from {AuditLog.Source}");
            Stopwatch watch =  Stopwatch.StartNew();
            AuditLogQueryResult result;
            long eventCount = 0;
            int batchSize = 1000;
            string continuationToken = null;
            do
            {
                // query the log and store to the local cache
                result = await AuditLog.QueryLogAsync(batchSize: batchSize, continuationToken: continuationToken);
                HandleEntries(result.DecoratedAuditLogEntries);
                continuationToken = result.ContinuationToken;
                // Count how many entries have been downloaded
                eventCount += result.DecoratedAuditLogEntries.Count();

            } while (result.HasMore);

            Console.WriteLine($"Published {eventCount} events in {watch.Elapsed}");
        }

        private void HandleEntries(IEnumerable<DecoratedAuditLogEntry> entries)
        {
            
        }
    }
}
