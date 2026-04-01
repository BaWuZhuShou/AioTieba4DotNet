#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Testing;

public sealed record ThreadReadSample(
    SafeForumFixture Forum,
    long Tid,
    int ThreadsPageNumber,
    int ThreadsPageSize,
    ThreadSortType ThreadsSort);

public sealed record CommentSourceSample(
    ThreadReadSample ThreadSample,
    long Pid,
    int PostsPageNumber,
    int PostsPageSize,
    PostSortType PostsSort,
    uint ReplyCount,
    int PreviewCommentCount);

public static class ThreadReadSampleDiscovery
{
    public static async Task<ThreadReadSample> RequireThreadSampleAsync(
        IThreadModule threadModule,
        SafeForumFixture forum,
        string operationName,
        int maxThreadPages = 5,
        int threadPageSize = 10,
        ThreadSortType threadSort = ThreadSortType.Reply,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(threadModule);
        ArgumentNullException.ThrowIfNull(forum);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (maxThreadPages <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxThreadPages));

        if (threadPageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(threadPageSize));

        for (var pageNumber = 1; pageNumber <= maxThreadPages; pageNumber++)
        {
            var threads = await threadModule.GetThreadsAsync(
                forum.ResolvedName,
                pageNumber,
                threadPageSize,
                threadSort,
                cancellationToken: cancellationToken);

            var firstThread = threads.Objs.FirstOrDefault();
            if (firstThread is not null)
                return new ThreadReadSample(forum, firstThread.Tid, pageNumber, threadPageSize, threadSort);
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: safe forum '{forum.ResolvedName}' (query '{forum.Query}') returned zero threads across {maxThreadPages} sampled page(s) with rn={threadPageSize} and sort={threadSort}.");
        return default!;
    }

    public static async Task<CommentSourceSample> RequireCommentSourceSampleAsync(
        IThreadModule threadModule,
        SafeForumFixture forum,
        string operationName,
        int maxThreadPages = 5,
        int threadPageSize = 10,
        int maxThreadsPerPage = 5,
        int postPageSize = 20,
        int previewCommentCount = 2,
        ThreadSortType threadSort = ThreadSortType.Reply,
        PostSortType postSort = PostSortType.Hot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(threadModule);
        ArgumentNullException.ThrowIfNull(forum);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (maxThreadPages <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxThreadPages));

        if (threadPageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(threadPageSize));

        if (maxThreadsPerPage <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxThreadsPerPage));

        if (postPageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(postPageSize));

        if (previewCommentCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(previewCommentCount));

        var sampledThreadCount = 0;
        List<string>? sampleErrors = null;

        for (var threadPageNumber = 1; threadPageNumber <= maxThreadPages; threadPageNumber++)
        {
            var threads = await threadModule.GetThreadsAsync(
                forum.ResolvedName,
                threadPageNumber,
                threadPageSize,
                threadSort,
                cancellationToken: cancellationToken);

            foreach (var thread in threads.Objs.Take(maxThreadsPerPage))
            {
                sampledThreadCount++;

                try
                {
                    var posts = await threadModule.GetPostsAsync(
                        thread.Tid,
                        1,
                        postPageSize,
                        postSort,
                        false,
                        true,
                        previewCommentCount,
                        true,
                        cancellationToken);
                    var candidate = posts.Objs.FirstOrDefault(post => post.ReplyNum > 0 || post.Comments.Count > 0);
                    if (candidate is not null)
                        return new CommentSourceSample(
                            new ThreadReadSample(forum, thread.Tid, threadPageNumber, threadPageSize, threadSort),
                            candidate.Pid,
                            1,
                            postPageSize,
                            postSort,
                            candidate.ReplyNum,
                            candidate.Comments.Count);
                }
                catch (TieBaServerException exception)
                {
                    sampleErrors ??= [];
                    sampleErrors.Add($"tid={thread.Tid}: {exception.Message}");
                }
            }
        }

        var errorSuffix = sampleErrors is { Count: > 0 }
            ? $" Sampled thread read errors: {string.Join(" | ", sampleErrors.Take(3))}."
            : string.Empty;
        Assert.Inconclusive(
            $"Skipping {operationName}: no post with comments was found for safe forum '{forum.ResolvedName}' (query '{forum.Query}') across {maxThreadPages} sampled page(s) and {sampledThreadCount} sampled thread(s).{errorSuffix}");
        return default!;
    }
}
