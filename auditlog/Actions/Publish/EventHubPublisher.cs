using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.VisualStudio.Services.Audit.WebApi;
using Newtonsoft.Json;

namespace auditlog
{
    public class EventHubPublisher : AuditLogPublisher, IDisposable
    {
        private EventHubClient eventHubClient;
        private EventHubClient HttpClient
        {
            get
            {
                if (eventHubClient == null)
                {
                    EventHubsConnectionStringBuilder builder = new EventHubsConnectionStringBuilder(Settings.Url)
                    {
                        TransportType = TransportType.Amqp,
                        OperationTimeout = TimeSpan.FromMinutes(5)
                    };
                    eventHubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
                }

                return eventHubClient;
            }
        }

        public EventHubPublisher(PublishTargetSettings settings) : base(settings)
        {
        }

        protected override async Task SendToProvider(List<DecoratedAuditLogEntry> entries)
        {
            // Event Hub has a max message limit of 262144 bytes, we need to batch to ensure a successful delivery
            var serializer = JsonSerializer.CreateDefault();
            EventDataBatch batch = new EventDataBatch(maxMessageSizeInBytes);
            entries.ForEach(e => batch.TryAdd(new EventData(GetEventData(e))));
            await HttpClient.SendAsync(batch);
        }

        private byte[] GetEventData(DecoratedAuditLogEntry entry)
        {
            StringBuilder payload = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(payload))
            {
                JsonSerializer.CreateDefault().Serialize(stringWriter, entry);
            }
            return Encoding.UTF8.GetBytes(payload.ToString());
        }

        void IDisposable.Dispose()
        {
            Debug.Assert(EventsToPublish.Any(), $"There are undelivered events on {PublisherName}");
            eventHubClient?.Close();
            eventHubClient = null;
        }

        public override string PublisherName => EventHubPublisherName;
        public static string EventHubPublisherName => "EventHub";
        private int maxMessageSizeInBytes = 256 * 1024;
    }
}