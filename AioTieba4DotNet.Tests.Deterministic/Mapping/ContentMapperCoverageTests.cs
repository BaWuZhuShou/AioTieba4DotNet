#nullable enable
using System.Collections.Generic;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class ContentMapperCoverageTests
{
    [TestMethod]
    public void ContentMapper_FromPbContents_MapsAllMajorFragmentKinds_AndHandlesNull()
    {
        var mapped = ContentMapper.FromTbData(new List<PbContent>
        {
            new() { Type = 0, Text = "text" },
            new() { Type = 4, Text = "text-4" },
            new() { Type = 9, Text = "text-9" },
            new() { Type = 18, Text = "text-18" },
            new() { Type = 27, Text = "text-27" },
            new() { Type = 44, Text = "text-44" },
            new() { Type = 2, Text = "@user", Uid = 42 },
            new() { Type = 11, Text = "1", C = "emoji" },
            new()
            {
                Type = 3,
                CdnSrc = "https://imgsrc.baidu.com/forum/pic/item/1234567890abcdef1234567890abcdef.jpg",
                BigCdnSrc = "big",
                OriginSrc = "origin",
                OriginSize = 55,
                Bsize = "120,80"
            },
            new()
            {
                Type = 20,
                CdnSrc = "https://imgsrc.baidu.com/forum/pic/item/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg",
                BigCdnSrc = "big2",
                OriginSrc = "origin2",
                OriginSize = 56,
                Bsize = "121,81"
            },
            new()
            {
                Type = 35,
                TiebaplusInfo =
                    new PbContent.Types.TiebaPlusInfo { Desc = "plus", JumpUrl = "https://example.com/plus" }
            },
            new()
            {
                Type = 36,
                TiebaplusInfo =
                    new PbContent.Types.TiebaPlusInfo { Desc = "plus-36", JumpUrl = "https://example.com/plus36" }
            },
            new()
            {
                Type = 37,
                TiebaplusInfo =
                    new PbContent.Types.TiebaPlusInfo { Desc = "plus-37", JumpUrl = "https://example.com/plus37" }
            },
            new() { Type = 1, Text = "link title", Link = "https://example.com/link" },
            new() { Type = 10, VoiceMd5 = "voice-md5", DuringTime = 2300 },
            new()
            {
                Type = 5,
                Link = "https://example.com/video.mp4",
                Src = "https://example.com/cover.jpg",
                DuringTime = 6,
                Width = 480,
                Height = 320,
                Count = 9
            }
        });
        var empty = ContentMapper.FromTbData((IEnumerable<PbContent>?)null);

        Assert.AreEqual(6, mapped.Texts.Count);
        Assert.AreEqual(1, mapped.Ats.Count);
        Assert.AreEqual(1, mapped.Emojis.Count);
        Assert.AreEqual(2, mapped.Images.Count);
        Assert.AreEqual(3, mapped.TiebaPluses.Count);
        Assert.AreEqual(1, mapped.Links.Count);
        Assert.AreEqual("voice-md5", mapped.Voice?.Md5);
        Assert.AreEqual(2, mapped.Voice?.Duration);
        Assert.AreEqual("https://example.com/video.mp4", mapped.Video?.Src);
        Assert.AreEqual(480U, mapped.Video?.Width);
        Assert.AreEqual(0, mapped.Frags[0].Index);
        Assert.AreEqual(mapped.Frags.Count - 1, mapped.Frags[^1].Index);
        Assert.AreEqual(0, empty.Frags.Count);
        Assert.IsNotNull(empty.Voice);
        Assert.IsNotNull(empty.Video);
    }

    [TestMethod]
    public void ContentMapper_FromPostInfoContent_MapsTextLinkEmojiAndVoiceBranches()
    {
        var postInfoContent = new PostInfoList.Types.PostInfoContent();
        postInfoContent.PostContent.Add(
            new PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 0, Text = "text" });
        postInfoContent.PostContent.Add(new PostInfoList.Types.PostInfoContent.Types.Abstract
        {
            Type = 1, Text = "link title", Link = "https://example.com/link"
        });
        postInfoContent.PostContent.Add(
            new PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 2, Text = "emoji-id" });
        postInfoContent.PostContent.Add(
            new PostInfoList.Types.PostInfoContent.Types.Abstract
            {
                Type = 10, VoiceMd5 = "voice-md5", DuringTime = "2500"
            });
        postInfoContent.PostContent.Add(
            new PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 99, Text = "unknown" });

        var mapped = ContentMapper.FromTbData(postInfoContent);

        Assert.AreEqual(1, mapped.Texts.Count);
        Assert.AreEqual(1, mapped.Links.Count);
        Assert.AreEqual(1, mapped.Emojis.Count);
        Assert.AreEqual("emoji-id", mapped.Emojis[0].Id);
        Assert.AreEqual("voice-md5", mapped.Voice?.Md5);
        Assert.AreEqual(2, mapped.Voice?.Duration);
        Assert.AreEqual(4, mapped.Frags.Count);
    }

    [TestMethod]
    public void ContentMapper_FromPostInfoList_AppendsMediaVideoAndVoice_AndReindexesFragments()
    {
        var postInfoList = new PostInfoList
        {
            FirstPostContent = { new PbContent { Type = 0, Text = "first" } },
            VideoInfo =
                new VideoInfo
                {
                    VideoUrl = "https://example.com/video.mp4",
                    ThumbnailUrl = "https://example.com/cover.jpg",
                    VideoDuration = 6,
                    VideoWidth = 320,
                    VideoHeight = 180,
                    PlayCount = 11
                },
            VoiceInfo = { new Voice { VoiceMd5 = "voice-md5", DuringTime = 3400 } },
            Media =
            {
                new Media
                {
                    Type = 1,
                    SmallPic =
                        "https://imgsrc.baidu.com/forum/pic/item/abcdefabcdefabcdefabcdefabcdefab.jpg",
                    BigPic = "big",
                    OriginPic = "origin",
                    OriginSize = 77,
                    Width = 20,
                    Height = 10
                },
                new Media
                {
                    Type = 5,
                    SmallPic =
                        "https://imgsrc.baidu.com/forum/pic/item/ffffffffffffffffffffffffffffffff.jpg",
                    Width = 99,
                    Height = 99
                }
            }
        };

        var mapped = ContentMapper.FromTbData(postInfoList);

        Assert.AreEqual(1, mapped.Images.Count);
        Assert.AreEqual("abcdefabcdefabcdefabcdefabcdefab", mapped.Images[0].Hash);
        Assert.IsNotNull(mapped.Video);
        Assert.AreEqual(320U, mapped.Video!.Width);
        Assert.IsNotNull(mapped.Voice);
        Assert.AreEqual(3, mapped.Voice!.Duration);
        Assert.AreEqual(mapped.Frags.Count - 1, mapped.Frags[^1].Index);
    }

    [TestMethod]
    public void ContentMapper_FromThreadVariants_MapsFragmentsAndNullThreadFallback()
    {
        var origin = new ThreadInfo.Types.OriginThreadInfo
        {
            VideoInfo = new VideoInfo
            {
                VideoUrl = "https://example.com/video.mp4",
                ThumbnailUrl = "https://example.com/cover.jpg",
                VideoDuration = 6,
                VideoWidth = 320,
                VideoHeight = 180,
                PlayCount = 11
            },
            VoiceInfo = { new Voice { VoiceMd5 = "voice-md5", DuringTime = 2500 } }
        };
        origin.Content.Add(new PbContent { Type = 0, Text = "origin-text" });
        origin.Content.Add(new PbContent { Type = 4, Text = "@legacy-at", Uid = 88 });
        origin.Content.Add(new PbContent { Type = 1, Text = "link title", Link = "https://example.com/link" });
        origin.Content.Add(new PbContent { Type = 2, Text = "@user", Uid = 42 });
        origin.Content.Add(new PbContent { Type = 11, Text = "1", C = "emoji" });
        origin.Content.Add(new PbContent
        {
            Type = 3,
            CdnSrc = "https://imgsrc.baidu.com/forum/pic/item/1234567890abcdef1234567890abcdef.jpg",
            BigCdnSrc = "big",
            OriginSrc = "origin",
            OriginSize = 55,
            Bsize = "120,80"
        });
        origin.Content.Add(new PbContent
        {
            Type = 35,
            TiebaplusInfo =
                new PbContent.Types.TiebaPlusInfo { Desc = "plus", JumpUrl = "https://example.com/plus" }
        });
        origin.Content.Add(new PbContent { Type = 10, Text = "ignored-video-sentinel" });
        origin.Content.Add(new PbContent { Type = 5, Text = "ignored-old-plus" });
        origin.Content.Add(new PbContent { Type = 34, Text = "ignored" });
        origin.Content.Add(new PbContent { Type = 99, Text = "unknown" });

        var thread = new ThreadInfo
        {
            VideoInfo =
                new VideoInfo
                {
                    VideoUrl = "https://example.com/video.mp4",
                    ThumbnailUrl = "https://example.com/cover.jpg",
                    VideoDuration = 6,
                    VideoWidth = 320,
                    VideoHeight = 180,
                    PlayCount = 11
                },
            VoiceInfo = { new Voice { VoiceMd5 = "voice-md5", DuringTime = 2500 } },
            FirstPostContent =
            {
                new PbContent { Type = 0, Text = "thread-text" },
                new PbContent { Type = 4, Text = "thread-text-4" },
                new PbContent { Type = 1, Text = "link title", Link = "https://example.com/link" },
                new PbContent { Type = 2, Text = "@user", Uid = 42 },
                new PbContent { Type = 11, Text = "1", C = "emoji" },
                new PbContent
                {
                    Type = 3,
                    CdnSrc = "https://imgsrc.baidu.com/forum/pic/item/1234567890abcdef1234567890abcdef.jpg",
                    BigCdnSrc = "big",
                    OriginSrc = "origin",
                    OriginSize = 55,
                    Bsize = "120,80"
                },
                new PbContent
                {
                    Type = 35,
                    TiebaplusInfo =
                        new PbContent.Types.TiebaPlusInfo { Desc = "plus", JumpUrl = "https://example.com/plus" }
                },
                new PbContent { Type = 34, Text = "ignored" },
                new PbContent { Type = 99, Text = "unknown" }
            }
        };

        var originMapped = ContentMapper.FromTbData(origin);
        var threadMapped = ContentMapper.FromTbData(thread);
        var empty = ContentMapper.FromTbData((ThreadInfo?)null);

        Assert.AreEqual("origin-text", originMapped.Texts[0].Text);
        Assert.AreEqual(1, originMapped.Links.Count);
        Assert.AreEqual(1, originMapped.Ats.Count);
        Assert.AreEqual(1, originMapped.Images.Count);
        Assert.AreEqual(1, originMapped.TiebaPluses.Count);
        Assert.IsNotNull(originMapped.Video);
        Assert.IsNotNull(originMapped.Voice);
        Assert.AreEqual("thread-text", threadMapped.Texts[0].Text);
        Assert.AreEqual(1, threadMapped.Ats.Count);
        Assert.IsNotNull(threadMapped.Video);
        Assert.IsNotNull(threadMapped.Voice);
        Assert.AreEqual(0, empty.Frags.Count);
        Assert.IsNotNull(empty.Video);
        Assert.IsNotNull(empty.Voice);
    }

    [TestMethod]
    public void ContentMapper_FromThreadVariants_IgnoresLegacyAndUnknownFragmentTypes()
    {
        var origin = new ThreadInfo.Types.OriginThreadInfo();
        origin.Content.Add(new PbContent { Type = 10, Text = "video" });
        origin.Content.Add(new PbContent { Type = 5, Text = "outdated tiebaplus" });
        origin.Content.Add(new PbContent { Type = 34, Text = "voice" });
        origin.Content.Add(new PbContent { Type = 99, Text = "unknown" });

        var mapped = ContentMapper.FromTbData(origin);

        Assert.AreEqual(0, mapped.Texts.Count);
        Assert.AreEqual(0, mapped.Emojis.Count);
        Assert.AreEqual(0, mapped.Images.Count);
        Assert.AreEqual(0, mapped.Ats.Count);
        Assert.AreEqual(0, mapped.Links.Count);
        Assert.AreEqual(0, mapped.TiebaPluses.Count);
    }
}
