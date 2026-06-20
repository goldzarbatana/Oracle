namespace TimeAura.Core
{
    public readonly struct GameStateChangedEvent
    {
        public GameStateChangedEvent(GameState state)
        {
            State = state;
        }

        public GameState State { get; }
    }

    public readonly struct LanguageChangedEvent
    {
        public readonly UnityEngine.SystemLanguage Language;
        public LanguageChangedEvent(UnityEngine.SystemLanguage lang) => Language = lang;
    }
}
