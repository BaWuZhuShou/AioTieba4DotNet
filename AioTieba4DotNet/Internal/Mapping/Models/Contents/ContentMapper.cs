using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ContentMapper
{
    internal static Content FromTbData(ThreadInfo.Types.OriginThreadInfo threadInfo)

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
                    var text = FragTextMapper.FromTbData(content);

                    texts.Add(text);

                    frags.Add(text);
                }
            },
            {
                [1], content =>

                {
                    var link = FragLinkMapper.FromTbData(content);

                    links.Add(link);

                    frags.Add(link);
                }
            },
            {
                [2], content =>

                {
                    var at = FragAtMapper.FromTbData(content);

                    ats.Add(at);

                    frags.Add(at);
                }
            },
            {
                [11], content =>

                {
                    var emoji = FragEmojiMapper.FromTbData(content);

                    emojis.Add(emoji);

                    frags.Add(emoji);
                }
            },
            {
                [3, 20], content =>

                {
                    var image = FragImageMapper.FromTbData(content);

                    images.Add(image);

                    frags.Add(image);
                }
            },
            {
                [10], content =>

                {
                    voice = FragVoiceMapper.FromTbData(content);

                    if (voice != null) frags.Add(voice);
                }
            },
            {
                [35, 36, 37], content =>

                {
                    var tiebaPlus = FragTiebaPlusMapper.FromTbData(content);

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
            video = FragVideoMapper.FromTbData(threadInfo.VideoInfo);

            frags.Add(video);
        }


        if (threadInfo.VoiceInfo is { Count: > 0 })

        {
            voice = FragVoiceMapper.FromTbData(threadInfo.VoiceInfo[0]);

            frags.Add(voice);
        }


        SetFragsIndex(frags);


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


    internal static Content FromTbData(ThreadInfo? threadInfo)

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
                    var text = FragTextMapper.FromTbData(content);

                    texts.Add(text);

                    frags.Add(text);
                }
            },
            {
                [1], content =>

                {
                    var link = FragLinkMapper.FromTbData(content);

                    links.Add(link);

                    frags.Add(link);
                }
            },
            {
                [2], content =>

                {
                    var at = FragAtMapper.FromTbData(content);

                    ats.Add(at);

                    frags.Add(at);
                }
            },
            {
                [11], content =>

                {
                    var emoji = FragEmojiMapper.FromTbData(content);

                    emojis.Add(emoji);

                    frags.Add(emoji);
                }
            },
            {
                [3, 20], content =>

                {
                    var image = FragImageMapper.FromTbData(content);

                    images.Add(image);

                    frags.Add(image);
                }
            },
            {
                [35, 36, 37], content =>

                {
                    var tiebaPlus = FragTiebaPlusMapper.FromTbData(content);

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
            video = FragVideoMapper.FromTbData(threadInfo.VideoInfo);

            frags.Add(video);
        }


        if (threadInfo.VoiceInfo is { Count: > 0 })

        {
            voice = FragVoiceMapper.FromTbData(threadInfo.VoiceInfo[0]);

            frags.Add(voice);
        }


        SetFragsIndex(frags);


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


    internal static Content FromTbData(PostInfoList.Types.PostInfoContent postInfoContent)

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
                    var text = FragTextMapper.FromTbData(content);

                    texts.Add(text);

                    frags.Add(text);
                }
            },
            {
                [1], content =>

                {
                    var link = FragLinkMapper.FromTbData(content);

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

                    frags.Add(voice);
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


        SetFragsIndex(frags);


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


    internal static Content FromTbData(PostInfoList dataRes)

    {
        var content = FromTbData(dataRes.FirstPostContent);


        if (dataRes.Media is { Count: > 0 })

            foreach (var image in dataRes.Media.Where(m => m.Type != 5).Select(FragImageMapper.FromTbData))

            {
                content.Images.Add(image);

                content.Frags.Add(image);
            }


        if (dataRes.VideoInfo is { VideoWidth: > 0 })

        {
            var video = FragVideoMapper.FromTbData(dataRes.VideoInfo);

            content.Frags.Add(video);

            content.Video = video;
        }


        if (dataRes.VoiceInfo is { Count: > 0 })

        {
            var voice = FragVoiceMapper.FromTbData(dataRes.VoiceInfo[0]);

            content.Frags.Add(voice);

            content.Voice = voice;
        }


        SetFragsIndex(content.Frags);


        return content;
    }


    internal static Content FromTbData(IEnumerable<PbContent>? contentProtos)

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
                    var text = FragTextMapper.FromTbData(content);

                    texts.Add(text);

                    frags.Add(text);

                    break;
                }

                case 2:

                {
                    var at = FragAtMapper.FromTbData(content);

                    ats.Add(at);

                    frags.Add(at);

                    break;
                }

                case 11:

                {
                    var emoji = FragEmojiMapper.FromTbData(content);

                    emojis.Add(emoji);

                    frags.Add(emoji);

                    break;
                }

                case 3:

                case 20:

                {
                    var image = FragImageMapper.FromTbData(content);

                    images.Add(image);

                    frags.Add(image);

                    break;
                }

                case 35:

                case 36:

                case 37:

                {
                    var tiebaPlus = FragTiebaPlusMapper.FromTbData(content);

                    tiebaPluses.Add(tiebaPlus);

                    frags.Add(tiebaPlus);

                    break;
                }

                case 1:

                {
                    var link = FragLinkMapper.FromTbData(content);

                    links.Add(link);

                    frags.Add(link);

                    break;
                }

                case 10:

                {
                    voice = FragVoiceMapper.FromTbData(content);

                    if (voice != null) frags.Add(voice);

                    break;
                }

                case 5:

                {
                    video = FragVideoMapper.FromTbData(content);

                    if (video != null) frags.Add(video);

                    break;
                }
            }
        }


        SetFragsIndex(frags);


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

    private static void SetFragsIndex(List<IFrag> frags)

    {
        for (var i = 0; i < frags.Count; i++) frags[i].Index = i;
    }
}
