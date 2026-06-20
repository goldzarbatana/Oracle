namespace TimeAura.Core
{
    public enum ContractRealm
    {
        Ether,     // Time Bank (Horas)
        Material   // Freelance (Fiat/Real Money)
    }

    /// <summary>
    /// Event triggered when a contract between two Masters is successfully created and sealed (Horas locked).
    /// </summary>
    public readonly struct ContractCreatedEvent
    {
        public readonly string SessionId;
        public readonly string InitiatorId;
        public readonly string RecipientId;
        public readonly ContractRealm Realm;
        public readonly int LockedMinutes;
        public readonly float FiatAmount;
        public readonly string Terms;

        public ContractCreatedEvent(string sessionId, string initiatorId, string recipientId, ContractRealm realm, int lockedMinutes, float fiatAmount, string terms)
        {
            SessionId = sessionId;
            InitiatorId = initiatorId;
            RecipientId = recipientId;
            Realm = realm;
            LockedMinutes = lockedMinutes;
            FiatAmount = fiatAmount;
            Terms = terms;
        }
    }

    /// <summary>
    /// Event triggered when a dispute is raised in a Harmony Session.
    /// </summary>
    public readonly struct DisputeRaisedEvent
    {
        public readonly string SessionId;
        public readonly string RaisedByUserId;
        public readonly string Reason;

        public DisputeRaisedEvent(string sessionId, string raisedByUserId, string reason)
        {
            SessionId = sessionId;
            RaisedByUserId = raisedByUserId;
            Reason = reason;
        }
    }

    /// <summary>
    /// Event triggered when the client receives a voice command or mystical text instruction.
    /// </summary>
    public readonly struct VoiceCommandReceivedEvent
    {
        public readonly string CommandText;

        public VoiceCommandReceivedEvent(string commandText)
        {
            CommandText = commandText;
        }
    }

    /// <summary>
    /// Event triggered when the player triggers a specific newbie/tutorial state or onboarding check.
    /// </summary>
    public readonly struct NewbieStateTriggeredEvent
    {
        public readonly string StateName;
        public readonly string Details;

        public NewbieStateTriggeredEvent(string stateName, string details)
        {
            StateName = stateName;
            Details = details;
        }
    }

    /// <summary>
    /// System event representing a parsed action received from the Firebase Cloud AI Orchestrator.
    /// </summary>
    public readonly struct CloudActionEvent
    {
        public readonly string Type;
        public readonly string Payload; // JSON payload

        public CloudActionEvent(string type, string payload)
        {
            Type = type;
            Payload = payload;
        }
    }

    /// <summary>
    /// Event triggered when a system message (like a judge's verdict) should be injected into the local active chat.
    /// </summary>
    public readonly struct SystemMessageEvent
    {
        public readonly string Text;

        public SystemMessageEvent(string text)
        {
            Text = text;
        }
    }

    /// <summary>
    /// Event triggered when a global prophecy (dynamic quest) is received from the Orchestrator.
    /// </summary>
    public readonly struct GlobalProphecyEvent
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly string RecommendedAction;
        public readonly float BonusMultiplier;

        public GlobalProphecyEvent(string id, string title, string description, string recommendedAction, float bonusMultiplier)
        {
            Id = id;
            Title = title;
            Description = description;
            RecommendedAction = recommendedAction;
            BonusMultiplier = bonusMultiplier;
        }
    }

    /// <summary>
    /// Event triggered when the AI Orchestrator finds an autonomous closed loop match (e.g. A->B->C->A).
    /// </summary>
    public readonly struct AutonomousMatchFoundEvent
    {
        public readonly string MatchDescription;
        public readonly string OracleMessage;

        // Triple-agent loop details
        public readonly string UserANickname;
        public readonly string UserAAvatar;
        public readonly string RoleA;

        public readonly string UserBNickname;
        public readonly string UserBAvatar;
        public readonly string RoleB;

        public readonly string UserCNickname;
        public readonly string UserCAvatar;
        public readonly string RoleC;

        public AutonomousMatchFoundEvent(
            string matchDescription, 
            string oracleMessage,
            string userANickname = "", string userAAvatar = "", string roleA = "",
            string userBNickname = "", string userBAvatar = "", string roleB = "",
            string userCNickname = "", string userCAvatar = "", string roleC = "")
        {
            MatchDescription = matchDescription;
            OracleMessage = oracleMessage;
            UserANickname = userANickname;
            UserAAvatar = userAAvatar;
            RoleA = roleA;
            UserBNickname = userBNickname;
            UserBAvatar = userBAvatar;
            RoleB = roleB;
            UserCNickname = userCNickname;
            UserCAvatar = userCAvatar;
            RoleC = roleC;
        }
    }
}
