namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// 内容碎片列表
/// </summary>
public class Content
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text
    {
        get { return Texts.Aggregate("", (current, t) => current + t.Text); }
    }

    /// <summary>
    /// 纯文本碎片列表
    /// </summary>
    public List<FragText> Texts { get; private init; } = [];

    /// <summary>
    /// 表情碎片列表
    /// </summary>
    public List<FragEmoji> Emojis { get; init; } = [];

    /// <summary>
    /// 图像碎片列表
    /// </summary>
    public List<FragImage> Images { get; init; } = [];

    /// <summary>
    /// @碎片列表
    /// </summary>
    public List<FragAt> Ats { get; init; } = [];

    /// <summary>
    /// 链接碎片列表
    /// </summary>
    public List<FragLink> Links { get; init; } = [];

    /// <summary>
    /// 贴吧plus碎片列表
    /// </summary>
    public List<FragTiebaPlus> TiebaPluses { get; init; } = [];

    /// <summary>
    /// 视频碎片
    /// </summary>
    public FragVideo? Video { get; init; }

    /// <summary>
    /// 音频碎片
    /// </summary>
    public FragVoice? Voice { get; init; }

    /// <summary>
    /// 所有原始碎片
    /// </summary>
    public List<IFrag> Frags { get; init; } = [];

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo"></param>
    /// <returns>Content</returns>
    public static Content FromTbData(ThreadInfo.Types.OriginThreadInfo threadInfo)
    {
        var texts = new List<FragText>();
        var emojis = new List<FragEmoji>();
        var images = new List<FragImage>();
        var ats = new List<FragAt>();
        var links = new List<FragLink>();
        var tiebaPluses = new List<FragTiebaPlus>();
        var frags = new List<IFrag>();
        FragVideo? video = null;
        FragVoice? voice = null;
        var typeHandlers = new Dictionary<uint[], Action<PbContent>>
        {
            {
                [0, 9, 18, 27], content =>
                {
                    var text = FragText.FromTbData(content);
                    texts.Add(text);
                    frags.Add(text);
                }
            },
            {
                [2, 11], content =>
                {
                    var emoji = FragEmoji.FromTbData(content);
                    emojis.Add(emoji);
                    frags.Add(emoji);
                }
            },
            {
                [3, 20], content =>
                {
                    var image = FragImage.FromTbData(content);
                    images.Add(image);
                    frags.Add(image);
                }
            },
            {
                [35, 36, 37], content =>
                {
                    var tiebaPlus = FragTiebaPlus.FromTbData(content);
                    tiebaPluses.Add(tiebaPlus);
                    frags.Add(tiebaPlus);
                }
            }
        };
        foreach (var content in threadInfo.Content)
        {
            var type = content.Type;
            // 处理字典中定义的类型
            var handled = typeHandlers
                .Where(kv => kv.Key.Contains(type))
                .Select(kv =>
                {
                    kv.Value(content);
                    return true;
                })
                .FirstOrDefault();

            if (handled) continue;

            switch (type)
            {
                case 4:
                {
                    var at = FragAt.FromTbData(content);
                    ats.Add(at);
                    frags.Add(at);
                    break;
                }
                case 1:
                {
                    var link = FragLink.FromTbData(content);
                    links.Add(link);
                    frags.Add(link);
                    break;
                }
                case 10:
                // video
                case 5:
                // outdated tiebaplus
                case 34:
                    // voice
                    break;
                default:
                    Console.WriteLine($"Unknown fragment type. type: {type}");
                    break;
            }
        }


        if (threadInfo.VideoInfo != null)
        {
            video = FragVideo.FromTbData(threadInfo.VideoInfo);
        }

        if (threadInfo.VoiceInfo is { Count: > 0 })
        {
            voice = FragVoice.FromTbData(threadInfo.VoiceInfo[0]);
        }

        return new Content()
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video,
        };
    }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo"></param>
    /// <returns>Content</returns>
    public static Content FromTbData(ThreadInfo threadInfo)
    {
        var texts = new List<FragText>();
        var emojis = new List<FragEmoji>();
        var images = new List<FragImage>();
        var ats = new List<FragAt>();
        var links = new List<FragLink>();
        var tiebaPluses = new List<FragTiebaPlus>();
        var frags = new List<IFrag>();
        var video = new FragVideo();
        var voice = new FragVoice();
        var typeHandlers = new Dictionary<uint[], Action<PbContent>>
        {
            {
                [0, 9, 18, 27], content =>
                {
                    var text = FragText.FromTbData(content);
                    texts.Add(text);
                    frags.Add(text);
                }
            },
            {
                [2, 11], content =>
                {
                    var emoji = FragEmoji.FromTbData(content);
                    emojis.Add(emoji);
                    frags.Add(emoji);
                }
            },
            {
                [3, 20], content =>
                {
                    var image = FragImage.FromTbData(content);
                    images.Add(image);
                    frags.Add(image);
                }
            },
            {
                [35, 36, 37], content =>
                {
                    var tiebaPlus = FragTiebaPlus.FromTbData(content);
                    tiebaPluses.Add(tiebaPlus);
                    frags.Add(tiebaPlus);
                }
            }
        };
        foreach (var content in threadInfo.FirstPostContent)
        {
            var type = content.Type;
            // 处理字典中定义的类型
            var handled = typeHandlers
                .Where(kv => kv.Key.Contains(type))
                .Select(kv =>
                {
                    kv.Value(content);
                    return true;
                })
                .FirstOrDefault();

            if (handled) continue;
            switch (type)
            {
                case 4:
                {
                    var at = FragAt.FromTbData(content);
                    ats.Add(at);
                    frags.Add(at);
                    break;
                }
                case 1:
                {
                    var link = FragLink.FromTbData(content);
                    links.Add(link);
                    frags.Add(link);
                    break;
                }
                case 10:
                // video
                case 5:
                // outdated tiebaplus
                case 34:
                    // voice
                    break;
                default:
                {
                    Console.WriteLine($"Unknown fragment type. type: {type}");
                    break;
                }
            }
        }

        if (threadInfo.VideoInfo != null)
        {
            video = FragVideo.FromTbData(threadInfo.VideoInfo);
        }

        if (threadInfo.VoiceInfo is { Count: > 0 })
        {
            voice = FragVoice.FromTbData(threadInfo.VoiceInfo[0]);
        }

        return new Content()
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video,
        };
    }

    public static Content FromTbData(PostInfoList.Types.PostInfoContent postInfoContent)


    {
        var postContent = postInfoContent.PostContent;
        var texts = new List<FragText>();
        var emojis = new List<FragEmoji>();
        var images = new List<FragImage>();
        var ats = new List<FragAt>();
        var links = new List<FragLink>();
        var tiebaPluses = new List<FragTiebaPlus>();
        var frags = new List<IFrag>();
        var video = new FragVideo();
        var voice = new FragVoice();
        var typeHandlers = new Dictionary<uint[], Action<PostInfoList.Types.PostInfoContent.Types.Abstract>>
        {
            {
                [0, 4], content =>
                {
                    var text = FragText.FromTbData(content);
                    texts.Add(text);
                    frags.Add(text);
                }
            },
            {
                [1], content =>
                {
                    var link = FragLink.FromTbData(content);
                    links.Add(link);
                    frags.Add(link);
                }
            },
            {
                [10], content =>
                {
                     voice = FragVoice.FromTbData(content);
                }
            }
        };
        foreach (var content in postContent)
        {
            var type = (uint)content!.Type;
            // 处理字典中定义的类型
            var handled = typeHandlers
                .Where(kv => kv.Key.Contains(type))
                .Select(kv =>
                {
                    kv.Value(content);
                    return true;
                })
                .FirstOrDefault();

            if (handled) continue;
            Console.WriteLine($"Unknown fragment type. type: {type}");

        }

        return new Content()
        {
            Texts = texts,
            Links = links,
            Frags = frags,
            Voice = voice,
        };
    }

    /// <summary>
    /// 格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        var emojisString = Emojis.Aggregate("", (current, emoji) => current + (emoji + ", "));
        var imageString = Images.Aggregate("", (current, image) => current + (image + ", "));
        var atsString = Ats.Aggregate("", (current, at) => current + (at + ", "));
        var linkString = Links.Aggregate("", (current, link) => current + (link + ", "));
        var tiebaPlusString = TiebaPluses.Aggregate("", (current, tiebaPlus) => current + (tiebaPlus + ", "));
        var fragString = Frags.Aggregate("", (current, frag) => current + (frag + ", "));

        return
            $"{nameof(Text)}: {Text},  {nameof(Emojis)}: {emojisString}, {nameof(Images)}: {imageString}, {nameof(Ats)}: {atsString}, {nameof(Links)}: {linkString}, {nameof(TiebaPluses)}: {tiebaPlusString}, {nameof(Video)}: {Video}, {nameof(Voice)}: {Voice}, {nameof(Frags)}: {fragString}";
    }
}