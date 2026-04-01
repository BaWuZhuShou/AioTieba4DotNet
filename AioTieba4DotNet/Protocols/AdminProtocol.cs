using AioTieba4DotNet.Api.AddBaWu;
using AioTieba4DotNet.Api.AddBawuBlacklist;
using AioTieba4DotNet.Api.DelBawu;
using AioTieba4DotNet.Api.DelBawuBlacklist;
using AioTieba4DotNet.Api.GetBawuBlacklist;
using AioTieba4DotNet.Api.GetBawuInfo;
using AioTieba4DotNet.Api.GetBawuPerm;
using AioTieba4DotNet.Api.GetBawuPostlogs;
using AioTieba4DotNet.Api.GetBawuUserlogs;
using AioTieba4DotNet.Api.GetBlocks;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetUnblockAppeals;
using AioTieba4DotNet.Api.HandleUnblockAppeals;
using AioTieba4DotNet.Api.SetBawuPerm;
using AioTieba4DotNet.Api.Unblock;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;
using BlockApi = AioTieba4DotNet.Api.Block.Block;

namespace AioTieba4DotNet.Protocols;

internal sealed class AdminProtocol(TiebaOperationDispatcher dispatcher, ForumInfoCache cache) : IAdminProtocol
{
    private readonly ForumInfoCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<bool> AddBaWuAsync(string fname, string userName, BawuType bawuType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateRequiredText(userName, nameof(userName));
        ValidateEnum(bawuType, nameof(bawuType));

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true);
        var fid = await ResolveForumIdAsync(nameof(AddBaWuAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(AddBaWuAsync),
                capabilities,
                (session, ct) => new AddBaWu(session.HttpCore).RequestAsync((long)fid, userName,
                    BawuTypeWireMapper.ToWireValue(bawuType), ct)),
            cancellationToken);
    }

    public async Task<bool> DelBaWuAsync(string fname, string portrait, BawuType bawuType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateRequiredText(portrait, nameof(portrait));
        ValidateEnum(bawuType, nameof(bawuType));

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true);
        var fid = await ResolveForumIdAsync(nameof(DelBaWuAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelBaWuAsync),
                capabilities,
                (session, ct) => new DelBaWu(session.HttpCore).RequestAsync((long)fid, portrait,
                    BawuTypeWireMapper.ToWireValue(bawuType), ct)),
            cancellationToken);
    }

    public async Task<bool> AddBawuBlacklistAsync(string fname, long userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(AddBawuBlacklistAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new AddBawuBlacklist(session.HttpCore).RequestAsync(fname, userId, ct)),
            cancellationToken);
    }

    public async Task<bool> DelBawuBlacklistAsync(string fname, long userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelBawuBlacklistAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new DelBawuBlacklist(session.HttpCore).RequestAsync(fname, userId, ct)),
            cancellationToken);
    }

    public async Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BawuBlacklistUsers>(
                nameof(GetBawuBlacklistAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetBawuBlacklist(session.HttpCore).RequestAsync(fname, pn, ct)),
            cancellationToken);
    }

    public async Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);

        var fid = await ResolveForumIdAsync(nameof(GetBawuInfoAsync), TiebaOperationCapabilities.HttpOnly(), fname,
            cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BawuInfo>(
                nameof(GetBawuInfoAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetBawuInfo(session.HttpCore, session.WsCore).RequestHttpAsync(fid, ct),
                (session, ct) => new GetBawuInfo(session.HttpCore, session.WsCore).RequestWsAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<BawuPerm> GetBawuPermAsync(string fname, string portrait,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateRequiredText(portrait, nameof(portrait));

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true);
        var fid = await ResolveForumIdAsync(nameof(GetBawuPermAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BawuPerm>(
                nameof(GetBawuPermAsync),
                capabilities,
                (session, ct) => new GetBawuPerm(session.HttpCore).RequestAsync(fid, portrait, ct)),
            cancellationToken);
    }

    public async Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateRequiredText(portrait, nameof(portrait));

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true);
        var fid = await ResolveForumIdAsync(nameof(SetBawuPermAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetBawuPermAsync),
                capabilities,
                (session, ct) => new SetBawuPerm(session.HttpCore).RequestAsync(fid, portrait, permissions, ct)),
            cancellationToken);
    }

    public async Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);

        var query = options ?? new BawuPostLogQueryOptions();
        ValidatePageNumber(query.PageNumber);
        ValidateEnum(query.SearchType, nameof(query.SearchType));
        ValidateOperationType(query.OperationType, nameof(query.OperationType));
        ValidateDateRange(query.StartTime, query.EndTime);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BawuPostLogs>(
                nameof(GetBawuPostLogsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetBawuPostlogs(session.HttpCore).RequestAsync(fname, query.PageNumber,
                    query.SearchValue, query.SearchType, query.StartTime, query.EndTime, query.OperationType, ct)),
            cancellationToken);
    }

    public async Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);

        var query = options ?? new BawuUserLogQueryOptions();
        ValidatePageNumber(query.PageNumber);
        ValidateEnum(query.SearchType, nameof(query.SearchType));
        ValidateOperationType(query.OperationType, nameof(query.OperationType));
        ValidateDateRange(query.StartTime, query.EndTime);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BawuUserLogs>(
                nameof(GetBawuUserLogsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetBawuUserlogs(session.HttpCore).RequestAsync(fname, query.PageNumber,
                    query.SearchValue, query.SearchType, query.StartTime, query.EndTime, query.OperationType, ct)),
            cancellationToken);
    }

    public async Task<Appeals> GetUnblockAppealsAsync(string fname, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true);
        var fid = await ResolveForumIdAsync(nameof(GetUnblockAppealsAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Appeals>(
                nameof(GetUnblockAppealsAsync),
                capabilities,
                (session, ct) => new GetUnblockAppeals(session.HttpCore).RequestAsync(fid, pn, rn, ct)),
            cancellationToken);
    }

    public async Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateAppealIds(appealIds);

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true);
        var fid = await ResolveForumIdAsync(nameof(HandleUnblockAppealsAsync), capabilities, fname,
            cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(HandleUnblockAppealsAsync),
                capabilities,
                (session, ct) => new HandleUnblockAppeals(session.HttpCore).RequestAsync(fid, appealIds, refuse, ct)),
            cancellationToken);
    }

    public async Task<Blocks> GetBlocksAsync(string fname, string userName, int pn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true);
        var fid = await ResolveForumIdAsync(nameof(GetBlocksAsync), capabilities, fname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Blocks>(
                nameof(GetBlocksAsync),
                capabilities,
                (session, ct) => new GetBlocks(session.HttpCore).RequestAsync(fid, userName, pn, ct)),
            cancellationToken);
    }

    public async Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        ValidateRequiredText(portrait, nameof(portrait));
        ValidatePositive(day, nameof(day), "Block days must be positive.");

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(BlockAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new BlockApi(session.HttpCore).RequestAsync(fid, portrait, day, reason, ct)),
            cancellationToken);
    }

    public async Task<bool> BlockAsync(string fname, string portrait, int day, string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true);
        var fid = await ResolveForumIdAsync(nameof(BlockAsync), capabilities, fname, cancellationToken);
        return await BlockAsync(fid, portrait, day, reason, cancellationToken);
    }

    public async Task<bool> UnblockAsync(ulong fid, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UnblockAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Unblock(session.HttpCore).RequestAsync(fid, userId, ct)),
            cancellationToken);
    }

    public async Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);

        var capabilities = TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true);
        var fid = await ResolveForumIdAsync(nameof(UnblockAsync), capabilities, fname, cancellationToken);
        return await UnblockAsync(fid, userId, cancellationToken);
    }

    private async Task<ulong> ResolveForumIdAsync(string operationName, TiebaOperationCapabilities capabilities,
        string fname, CancellationToken cancellationToken)
    {
        var forumId = _cache.GetForumId(fname);
        if (forumId != 0)
            return forumId;

        await dispatcher.EnsureCanExecuteAsync(operationName, capabilities, cancellationToken);

        forumId = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ulong>(
                $"{operationName}ResolveFid",
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetFid(session.HttpCore).RequestAsync(fname, ct)),
            cancellationToken);

        if (forumId != 0)
            _cache.SetForumName(forumId, fname);

        return forumId;
    }

    private static void ValidateForumId(ulong fid)
    {
        if (fid == 0)
            throw new ArgumentOutOfRangeException(nameof(fid), fid, "Forum id must be positive.");
    }

    private static void ValidateForumName(string fname)
    {
        if (string.IsNullOrWhiteSpace(fname))
            throw new ArgumentException("Forum name must not be empty.", nameof(fname));
    }

    private static void ValidateRequiredText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", paramName);
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive.");
    }

    private static void ValidatePositive(int value, string paramName, string message)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, message);
    }

    private static void ValidatePageNumber(int pn)
    {
        if (pn <= 0)
            throw new ArgumentOutOfRangeException(nameof(pn), pn, "Page number must be positive.");
    }

    private static void ValidatePageSize(int rn)
    {
        if (rn <= 0)
            throw new ArgumentOutOfRangeException(nameof(rn), rn, "Page size must be positive.");
    }

    private static void ValidateOperationType(int operationType, string paramName)
    {
        if (operationType < 0)
            throw new ArgumentOutOfRangeException(paramName, operationType, "Operation type must not be negative.");
    }

    private static void ValidateDateRange(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        if (startTime.HasValue && endTime.HasValue && endTime.Value < startTime.Value)
            throw new ArgumentOutOfRangeException(nameof(endTime), endTime,
                "End time must be greater than or equal to start time.");
    }

    private static void ValidateAppealIds(IReadOnlyList<long> appealIds)
    {
        ArgumentNullException.ThrowIfNull(appealIds);
        if (appealIds.Count == 0)
            throw new ArgumentException("Appeal ids must not be empty.", nameof(appealIds));

        if (appealIds.Any(static appealId => appealId <= 0))
            throw new ArgumentOutOfRangeException(nameof(appealIds), appealIds,
                "All appeal ids must be positive.");
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Enum value is not supported.");
    }
}

internal static class BawuTypeWireMapper
{
    internal static string ToWireValue(BawuType bawuType) => bawuType switch
    {
        BawuType.Manager => "assist",
        BawuType.ImageEditor => "picadmin",
        BawuType.VoiceEditor => "voiceadmin",
        _ => throw new ArgumentOutOfRangeException(nameof(bawuType), bawuType, "Enum value is not supported.")
    };

    internal static bool TryFromWireValue(string value, out BawuType bawuType)
    {
        switch (value)
        {
            case "assist":
                bawuType = BawuType.Manager;
                return true;
            case "picadmin":
                bawuType = BawuType.ImageEditor;
                return true;
            case "voiceadmin":
                bawuType = BawuType.VoiceEditor;
                return true;
            default:
                bawuType = default;
                return false;
        }
    }
}
