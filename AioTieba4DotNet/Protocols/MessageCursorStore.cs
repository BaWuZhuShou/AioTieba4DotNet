using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Protocols;

internal sealed class MessageCursorStore
{
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private readonly Dictionary<long, MessageCursorPair> _pairs = new();
    private bool _initialized;

    internal long PrivateGroupId { get; private set; }

    internal async Task EnsureInitializedAsync(
        Func<CancellationToken, Task<IReadOnlyList<WsMsgGroupInfo>>> loader,
        CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            Initialize(await loader(cancellationToken));
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    internal void Initialize(IReadOnlyList<WsMsgGroupInfo> groups)
    {
        foreach (var group in groups)
        {
            _pairs[group.GroupId] = new MessageCursorPair(group.LastMessageId, group.LastMessageId);
            if (group.GroupType == 6) PrivateGroupId = group.GroupId;
        }

        _initialized = true;
    }

    internal IReadOnlyList<long> GetKnownGroupIds()
    {
        return _pairs.Keys.Where(static groupId => groupId > 0).OrderBy(static groupId => groupId).ToList();
    }

    internal long GetLastMessageId(long groupId)
    {
        return _pairs.TryGetValue(groupId, out var pair) ? pair.LastMessageId : 0;
    }

    internal long GetRecordId()
    {
        if (PrivateGroupId <= 0)
            throw new TiebaProtocolException(
                "The private-message group id is not available after websocket initialization.");

        return checked(GetLastMessageId(PrivateGroupId) * 100 + 1);
    }

    internal void Update(long groupId, long messageId)
    {
        if (messageId <= 0 || groupId <= 0) return;

        if (_pairs.TryGetValue(groupId, out var pair))
        {
            pair.Update(messageId);
            return;
        }

        _pairs[groupId] = new MessageCursorPair(messageId, messageId);
    }

    internal void Update(WsMsgGroups groups)
    {
        foreach (var group in groups)
        {
            var lastMessageId = group.Messages.Count == 0 ? 0 : group.Messages.Max(static message => message.MsgId);
            Update(group.GroupId, lastMessageId);
        }
    }

    private sealed class MessageCursorPair(long lastMessageId, long currentMessageId)
    {
        internal long LastMessageId { get; private set; } = lastMessageId;

        internal long CurrentMessageId { get; private set; } = currentMessageId;

        internal void Update(long messageId)
        {
            LastMessageId = CurrentMessageId;
            CurrentMessageId = messageId;
        }
    }
}
