using System.Threading;
using System.Threading.Tasks;
using TimeAura.Core;

namespace TimeAura.Features.Security
{
    public interface ISmsGateway : IService
    {
        Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken);
    }
}
