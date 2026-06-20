using System;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// ChatMessage - A single message in the Harmony Channel.
    /// </summary>
    [Serializable]
    public enum ChatMessageType
    {
        Text,       // Standard message
        Material,   // Visual Material (image)
        HorasOffer, // Master proposed a Horas exchange
        System      // Session open/close event
    }

    [Serializable]
    public class ChatMessage
    {
        public string MessageId;
        public string SenderId;
        public string Text;
        public string ImageUrl; // URL for artifact display
        public DateTime Timestamp;
        public ChatMessageType Type;

        public ChatMessage(string senderId, string text, ChatMessageType type = ChatMessageType.Text, string imageUrl = null)
        {
            MessageId = Guid.NewGuid().ToString();
            SenderId = senderId;
            Text = text;
            ImageUrl = imageUrl;
            Timestamp = DateTime.UtcNow;
            Type = type;
        }

        public string FormattedTime => Timestamp.ToLocalTime().ToString("HH:mm");
    }
}
