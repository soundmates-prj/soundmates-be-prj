using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IMessageBusPublisher
    {
        Task PublishAsync(string type, string payload, CancellationToken ct = default);
    }
}