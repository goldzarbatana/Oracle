using System.Threading;
using System.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Security
{
    public sealed class TwilioSmsGateway : MonoBehaviour, ISmsGateway
    {
        [Inject]
        private AppConfig appConfig;

        public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            Debug.Log($"[TwilioSmsGateway] SMS to {phoneNumber}: {message} (stub). SID: {appConfig?.TwilioAccountSid}");
            return Task.CompletedTask;
        }
    }
}
