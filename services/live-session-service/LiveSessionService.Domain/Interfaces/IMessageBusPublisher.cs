using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Domain.Interfaces
{
    public interface IMessageBusPublisher
    {
        Task PublishAsync(string type, string payload, CancellationToken ct = default);
    }
}