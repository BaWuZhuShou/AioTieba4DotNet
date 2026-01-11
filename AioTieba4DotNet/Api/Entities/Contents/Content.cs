namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
///     内容碎片列表
/// </summary>
public class Content
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => string.Concat(Frags.Select(f => f.Text));

    /// <summary>
    ///     纯文本碎片列表
    /// </summary>
    public List<FragText> Texts { get; init; } = [];

    /// <summary>
    ///     表情碎片列表
    /// </summary>
    public List<FragEmoji> Emojis { get; init; } = [];

    /// <summary>
    ///     图像碎片列表
    /// </summary>
    public List<FragImage> Images { get; init; } = [];

    /// <summary>
    ///     @碎片列表
    /// </summary>
    public List<FragAt> Ats { get; init; } = [];

    /// <summary>
    ///     链接碎片列表
    /// </summary>
    public List<FragLink> Links { get; init; } = [];

    /// <summary>
    ///     贴吧plus碎片列表
    /// </summary>
    public List<FragTiebaPlus> TiebaPluses { get; init; } = [];

    /// <summary>
    ///     视频碎片
    /// </summary>
    public FragVideo? Video { get; set; }

    /// <summary>
    ///     音频碎片
    /// </summary>
    public FragVoice? Voice { get; set; }

    /// <summary>
    ///     所有原始碎片
    /// </summary>
    public List<IFrag> Frags { get; init; } = [];

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo">Protobuf 主题帖信息数据</param>
    /// <returns>内容碎片列表实体</returns>
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
                [0, 4, 9, 18, 27, 44], content =>
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
                [2], content =>
                {
                    var at = FragAt.FromTbData(content);
                    ats.Add(at);
                    frags.Add(at);
                }
            },
            {
                [11], content =>
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
                [10], content =>
                {
                    voice = FragVoice.FromTbData(content);
                    if (voice != null) frags.Add(voice);
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
            frags.Add(video);
        }

        if (threadInfo.VoiceInfo is { Count: > 0 })
        {
            voice = FragVoice.FromTbData(threadInfo.VoiceInfo[0]);
            frags.Add(voice);
        }

        return new Content
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video
        };
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo"></param>
    /// <returns>Content</returns>
    public static Content FromTbData(ThreadInfo? threadInfo)
    {
        if (threadInfo == null)
            return new Content
            {
                Texts = [],
                Emojis = [],
                Images = [],
                Ats = [],
                Links = [],
                TiebaPluses = [],
                Frags = [],
                Voice = new FragVoice(),
                Video = new FragVideo()
            };

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
                [0, 4, 9, 18, 27, 44], content =>
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
                [2], content =>
                {
                    var at = FragAt.FromTbData(content);
                    ats.Add(at);
                    frags.Add(at);
                }
            },
            {
                [11], content =>
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
        foreach (var content in threadInfo!.FirstPostContent)
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
            frags.Add(video);
        }

        if (threadInfo.VoiceInfo is { Count: > 0 })
        {
            voice = FragVoice.FromTbData(threadInfo.VoiceInfo[0]);
            frags.Add(voice);
        }

        return new Content
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video
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
        FragVideo? video = null;
        FragVoice? voice = null;
        var typeHandlers = new Dictionary<uint[], Action<PostInfoList.Types.PostInfoContent.Types.Abstract>>
        {
            {
                [0, 4, 9, 18, 27, 44], content =>
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
                [2, 11], content =>
                {
                    var emoji = new FragEmoji { Id = content.Text, Desc = "" };
                    emojis.Add(emoji);
                    frags.Add(emoji);
                }
            },
            {
                [10], content =>
                {
                    voice = new FragVoice
                    {
                        Md5 = content.VoiceMd5,
                        Duration = (int)(double.TryParse(content.DuringTime, out var d) ? d / 1000 : 0)
                    };
                    if (voice != null) frags.Add(voice);
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

        return new Content
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video
        };
    }

    /// <summary>
    ///     从用户内容列表数据转换
    /// </summary>
    /// <param name="dataRes"></param>
    /// <returns>Content</returns>
    public static Content FromTbData(PostInfoList dataRes)
    {
        var content = FromTbData(dataRes.FirstPostContent);

        if (dataRes.Media is { Count: > 0 })
            foreach (var image in dataRes.Media.Where(m => m.Type != 5).Select(FragImage.FromTbData))
            {
                content.Images.Add(image);
                content.Frags.Add(image);
            }

        if (dataRes.VideoInfo is { VideoWidth: > 0 })
        {
            var video = FragVideo.FromTbData(dataRes.VideoInfo);
            content.Frags.Add(video);
            content.Video = video;
        }

        if (dataRes.VoiceInfo is { Count: > 0 })
        {
            var voice = FragVoice.FromTbData(dataRes.VoiceInfo[0]);
            content.Frags.Add(voice);
            content.Voice = voice;
        }

        return content;
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="contentProtos"></param>
    /// <returns>Content</returns>
    public static Content FromTbData(IEnumerable<PbContent>? contentProtos)
    {
        if (contentProtos == null)
            return new Content
            {
                Texts = [],
                Emojis = [],
                Images = [],
                Ats = [],
                Links = [],
                TiebaPluses = [],
                Frags = [],
                Voice = new FragVoice(),
                Video = new FragVideo()
            };

        var texts = new List<FragText>();
        var emojis = new List<FragEmoji>();
        var images = new List<FragImage>();
        var ats = new List<FragAt>();
        var links = new List<FragLink>();
        var tiebaPluses = new List<FragTiebaPlus>();
        var frags = new List<IFrag>();
        FragVideo? video = null;
        FragVoice? voice = null;

        foreach (var content in contentProtos)
        {
            var type = content.Type;
            switch (type)
            {
                case 0:
                case 4:
                case 9:
                case 18:
                case 27:
                case 44:
                {
                    var text = FragText.FromTbData(content);
                    texts.Add(text);
                    frags.Add(text);
                    break;
                }
                case 2:
                {
                    var at = FragAt.FromTbData(content);
                    ats.Add(at);
                    frags.Add(at);
                    break;
                }
                case 11:
                {
                    var emoji = FragEmoji.FromTbData(content);
                    emojis.Add(emoji);
                    frags.Add(emoji);
                    break;
                }
                case 3:
                case 20:
                {
                    var image = FragImage.FromTbData(content);
                    images.Add(image);
                    frags.Add(image);
                    break;
                }
                case 35:
                case 36:
                case 37:
                {
                    var tiebaPlus = FragTiebaPlus.FromTbData(content);
                    tiebaPluses.Add(tiebaPlus);
                    frags.Add(tiebaPlus);
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
                {
                    voice = FragVoice.FromTbData(content);
                    if (voice != null) frags.Add(voice);
                    break;
                }
                case 5:
                {
                    video = FragVideo.FromTbData(content);
                    if (video != null) frags.Add(video);
                    break;
                }
            }
        }

        return new Content
        {
            Texts = texts,
            Emojis = emojis,
            Images = images,
            Ats = ats,
            Links = links,
            TiebaPluses = tiebaPluses,
            Frags = frags,
            Voice = voice,
            Video = video
        };
    }

    /// <summary>
    ///     格式设置
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
