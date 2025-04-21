namespace Campus_SMS.Models
{
    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public required string Text { get; set; }

        public ChatMessage(string text) => Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}
