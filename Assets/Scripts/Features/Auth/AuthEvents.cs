namespace TimeAura.Features.Auth
{
    public readonly struct AuthCompletedEvent
    {
        public AuthCompletedEvent(AuthFlowResult result)
        {
            Result = result;
        }

        public AuthFlowResult Result { get; }
    }
}
