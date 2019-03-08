using System.Threading.Tasks;

namespace auditlog
{
    public interface ICommandAction
    {
        string Name { get; }

        Task PerformAsync();
    }
}
