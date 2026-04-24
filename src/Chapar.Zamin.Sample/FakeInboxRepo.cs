using Zamin.Extensions.MessageBus.Abstractions;
// ---------- Fake Inbox Repo ----------
public class FakeInboxRepo : IMessageInboxItemRepository
{
    private readonly HashSet<string> _ids = new();
    public bool AllowReceive(string messageId, string fromService) => _ids.Add(messageId);
    public void Receive(string messageId, string fromService, string payload) { }
}