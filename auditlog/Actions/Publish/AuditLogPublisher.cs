using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;

namespace auditlog
{
    public abstract class AuditLogPublisher
    {
        public abstract string PublisherName { get;}

        public AuditLogPublisher()
        {
            //OnEventAdded += HandleEventAdded;
        }

        protected AuditLogPublisher(PublishTargetSettings settings)
        {
            Settings = settings;
        }

        protected PublishTargetSettings Settings { get; }

        protected List<DecoratedAuditLogEntry> EventsToPublish = new List<DecoratedAuditLogEntry>();

        public void Add(DecoratedAuditLogEntry entry)
        {
            EventsToPublish.Add(entry);
        }

        public void Add(IEnumerable<DecoratedAuditLogEntry> entries)
        {
            EventsToPublish.AddRange(entries);
        }

        protected abstract Task SendToProvider(List<DecoratedAuditLogEntry> entries);

        // Published to the provider
        public async Task PublishAsync()
        {
            List<DecoratedAuditLogEntry> entries = CloneAndResetEntries();
            await SendToProvider(entries);
        }

        /// <summary>
        /// Takes a snapshot of the current events to be delivered, while the delivery happens other events added will be delivered later.
        /// </summary>
        /// <returns></returns>
        private List<DecoratedAuditLogEntry> CloneAndResetEntries()
        {
            List<DecoratedAuditLogEntry> eventsCopy;
            lock (EventsToPublish)
            {
                eventsCopy = EventsToPublish;
                EventsToPublish = new List<DecoratedAuditLogEntry>();
            }
            return eventsCopy;
        }
    }
}