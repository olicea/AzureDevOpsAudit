using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace auditlog
{
    public class CommandLineApplication : IServiceProvider, IDisposable
    {
        private readonly string[] CommandLineArguments;

        public CommandLineApplication(string[] args)
        {
            CommandLineArguments = args;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(CommandLineApplication))
            {
                return this;
            }

            //if (serviceType == typeof(IEnumerable<ICommandAction>))
            //{
            //    return GetOptions();
            //}

            //if (serviceType == typeof(IEnumerable<ICommandArgument>))
            //{
            //    return _parent.Arguments;
            //}

            throw new NotSupportedException();
        }

        internal async Task RunAsync()
        {
            //TODO:only publish is supported

            // initialize and fetch settings
            var publishTargets = await new FilePublisherSettingsProvider().LoadPublishersSettingsAsync();
            var auditLogSettings = await new FileAuditLogSettingsProvider().GetSettingsAsync();

            IAuditLogProvider auditLogProvider = new AzureDevOpsAuditLogProvider(auditLogSettings);
            
            await new PublishAction(publishTargets, auditLogProvider).PerformAsync();
        }

        public void Dispose()
        {
        }
    }
}