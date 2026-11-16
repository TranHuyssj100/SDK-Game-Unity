using System.Collections.Generic;

namespace LSDK.Firebase
{
    /// <summary>Tin nhắn FCM đã copy ra khỏi type của Firebase.</summary>
    public sealed class FirebasePushMessage
    {
        public FirebasePushMessage(string from, string messageId, string title, string body, Dictionary<string, string> data)
        {
            From = from ?? string.Empty;
            MessageId = messageId ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Data = data ?? new Dictionary<string, string>();
        }

        public string From { get; }
        public string MessageId { get; }
        public string Title { get; }
        public string Body { get; }
        public Dictionary<string, string> Data { get; }
    }
}
