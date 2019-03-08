using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;

namespace auditlog
{
    public interface IAuditLogProvider 
    {
        string Source {get;}
        Task<AuditLogQueryResult> QueryLogAsync(int batchSize, string continuationToken);
    }
}