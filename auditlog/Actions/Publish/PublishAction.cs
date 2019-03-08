using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;

namespace auditlog
{
    public class PublishAction : ICommandAction
    {
        public string Name => "publish";

        private List<PublishTargetSettings> PublishTargets;

        private List<AuditLogPublisher> Publishers;

        private IAuditLogProvider AuditLog { get; }

        public PublishAction(List<PublishTargetSettings> targets, IAuditLogProvider auditLog)
        {
            PublishTargets = targets;
            AuditLog = auditLog;

            CreatePublishers();
        }

        /// <summary>
        /// Construct and log the targets
        /// </summary>
        private void CreatePublishers()
        {
            //TODO: this should be done with DI
            Publishers = new List<AuditLogPublisher>();
            AddService<SplunkPublisher>(SplunkPublisher.SplunkPublisherName);
            AddService<EventHubPublisher>(EventHubPublisher.EventHubPublisherName);
            Publishers.ForEach(p => Console.WriteLine($"Publishing to {p?.PublisherName}"));
        }

        private void AddService<T>(string publisherName) where T : AuditLogPublisher
        {
            var target = PublishTargets.FirstOrDefault(p => p.Name == publisherName);
            if (target != null)
            {
                Publishers.Add((T)Activator.CreateInstance(typeof(T), target));
            }
        }

        public async Task PerformAsync()
        {
            Console.WriteLine($"Publishing the log from {AuditLog.Source}");
            Stopwatch watch =  Stopwatch.StartNew();
            AuditLogQueryResult result;
            long eventCount = 0;
            int batchSize = 1000;
            string continuationToken = null;
            do
            {
                // query the log and publish on each batch
                result = await AuditLog.QueryLogAsync(batchSize: batchSize, continuationToken: continuationToken);
                AddEntriesToPublish(result.DecoratedAuditLogEntries);
                continuationToken = result.ContinuationToken;
                // Count how many entries have been delivered
                eventCount += result.DecoratedAuditLogEntries.Count();

                // Publish to all providers
                PublishEntries();
            } while (result.HasMore);

            Console.WriteLine($"Published {eventCount} events in {watch.Elapsed}");
        }

        /// <summary>
        /// Process the log
        /// use the patter to process async
        /// https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
        /// </summary>
        /// <returns></returns>
        public async Task PerformOldAsync()
        {
            // // init the publishers
            // await CreatePublishersAsync();

            // query
            BlockingCollection<DecoratedAuditLogEntry> dataItems = new BlockingCollection<DecoratedAuditLogEntry>(100);
            AuditLogQueryResult result;
            do
            {
                result = await AuditLog.QueryLogAsync(0, null);
                // Blocks if numbers.Count == dataItems.BoundedCapacity
                foreach (DecoratedAuditLogEntry entry in result.DecoratedAuditLogEntries)
                {
                    //TODO: is there a better way to add a batch?
                    // i.e. AddRange()
                    dataItems.Add(entry);
                }

                // Let consumer know we are done.
                dataItems.CompleteAdding();
            } while (result.HasMore);

            // A simple blocking consumer with no cancellation.
            int entryCount = 0;
            while (!dataItems.IsCompleted)
            {
                DecoratedAuditLogEntry entry = null;
                // Blocks if number.Count == 0
                // IOE means that Take() was called on a completed collection.
                // Some other thread can call CompleteAdding after we pass the
                // IsCompleted check but before we call Take.
                // In this example, we can simply catch the exception since the
                // loop will break on the next iteration.
                try
                {
                    entry = dataItems.Take();
                }
                catch (InvalidOperationException) { }

                if (entry != null)
                {
                    // AddEntriesToPublish(entry);
                }

                if (++entryCount % 100 == 0)
                {
                    // publish once every 100 entries
                    PublishEntries();
                }
            }

            PublishEntries();
            Console.WriteLine("\r\nNo more items to take.");
        }

        private void PublishEntries()
        {
            Task[] publisherTasks = Publishers.Select(p => p.PublishAsync()).ToArray();
            Task.WaitAll(publisherTasks);
        }

        private void AddEntriesToPublish(IEnumerable<DecoratedAuditLogEntry> entries)
        {
            foreach (AuditLogPublisher target in Publishers)
            {
                //transform
                target.Add(entries);
            }
        }
    }
}
