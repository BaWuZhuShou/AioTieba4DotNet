#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using ThreadPost = AioTieba4DotNet.Models.Threads.Post;
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;

namespace AioTieba4DotNet.Tests.Coverage;

[TestClass]
public sealed class Task18TinyBranchFamilyCoverageTests
{
    [TestMethod]
    public void TinyBranchFamilyMappers_CoverFallbackAndBranchPaths()
    {
        var bawuMapped = BawuInfoMapper.FromTbData(new GetBawuInfoResIdl.Types.DataRes
        {
            BawuTeamInfo = new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam
            {
                TotalNum = 1,
                BawuTeamList =
                {
                    new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes
                    {
                        RoleName = "吧主",
                        RoleInfo =
                        {
                            new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes.
                                Types.BawuRoleInfoPub
                                {
                                    UserId = 42,
                                    Portrait = "tb.1.bawu?012345678901",
                                    UserName = "bawu-user",
                                    NameShow = "Bawu User",
                                    UserLevel = 7
                                }
                        }
                    }
                }
            }
        });
        var bawuFallback = BawuInfoMapper.FromTbData(new GetBawuInfoResIdl.Types.DataRes());

        var forumDetailTrue = ForumDetailMapper.FromTbData(new GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 7356044,
                ForumName = "csharp",
                Lv1Name = "programming",
                Avatar = "small.png",
                AvatarOrigin = "origin.png",
                Slogan = "safe forum",
                MemberCount = 11,
                ThreadCount = 22
            },
            ElectionTab = new GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "已有吧主"
            }
        });
        var forumDetailFalse = ForumDetailMapper.FromTbData(new GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 1,
                ForumName = "plain",
                Lv1Name = "cat",
                Avatar = "avatar.png",
                AvatarOrigin = "origin.png",
                Slogan = "plain forum",
                MemberCount = 1,
                ThreadCount = 2
            },
            ElectionTab = new GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "暂无吧主"
            }
        });
        var forumDetailNull = ForumDetailMapper.FromTbData(new GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 2,
                ForumName = "null-election",
                Lv1Name = "cat",
                Avatar = "avatar-null.png",
                AvatarOrigin = "origin-null.png",
                Slogan = "null forum",
                MemberCount = 3,
                ThreadCount = 4
            }
        });

        var notifyParsed = WsNotifyMapper.FromTbData(new PushNotifyResIdl.Types.PusherMsg
        {
            Data = new PushNotifyResIdl.Types.PusherMsg.Types.PusherMsgInfo
            {
                GroupId = 100,
                MsgId = 200,
                Type = 3,
                Et = "1711111111",
                GroupType = 5
            }
        });
        var notifyFallback = WsNotifyMapper.FromTbData(new PushNotifyResIdl.Types.PusherMsg
        {
            Data = new PushNotifyResIdl.Types.PusherMsg.Types.PusherMsgInfo
            {
                GroupId = 101,
                MsgId = 201,
                Type = 4,
                Et = "unexpected",
                GroupType = 6
            }
        });

        var oldUsersMapped = BlacklistOldUsersMapper.FromTbData(new UserMuteQueryResIdl.Types.DataRes
        {
            MuteUser =
            {
                new UserMuteQueryResIdl.Types.DataRes.Types.MuteUser
                {
                    UserId = 1,
                    UserName = "old-user",
                    MuteTime = 7,
                    Portrait = "tb.1.old?012345678901",
                    NameShow = "Old User"
                },
                new UserMuteQueryResIdl.Types.DataRes.Types.MuteUser
                {
                    UserId = 2,
                    UserName = "raw-user",
                    MuteTime = 8,
                    Portrait = "tb.1.raw",
                    NameShow = "Raw User"
                }
            },
            Page = new Page { CurrentPage = 2, HasMore = 1, HasPrev = 0 }
        });
        var oldUsersFallback = BlacklistOldUsersMapper.FromTbData(new UserMuteQueryResIdl.Types.DataRes
        {
            MuteUser =
            {
                new UserMuteQueryResIdl.Types.DataRes.Types.MuteUser
                {
                    UserId = 3,
                    UserName = "fallback-user",
                    MuteTime = 9,
                    Portrait = "tb.1.fallback",
                    NameShow = "Fallback User"
                }
            }
        });

        var selfFollowMapped = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray
            {
                new JObject { ["forum_id"] = 7356044UL, ["forum_name"] = "csharp", ["level_id"] = 7 }
            },
            ["page"] = new JObject { ["cur_page"] = 2, ["total_page"] = 4 }
        });
        var selfFollowFallback = SelfFollowForumsV1Mapper.FromTbData(new JObject());
        var selfFollowPartialPageFallback = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray(), ["page"] = new JObject()
        });

        Assert.AreEqual(1, bawuMapped.All.Count);
        Assert.AreEqual(1, bawuMapped.Admins.Count);
        Assert.AreEqual(0, bawuMapped.Managers.Count);
        Assert.AreEqual(42L, bawuMapped.All[0].UserId);
        Assert.AreEqual("tb.1.bawu?012345678901", bawuMapped.All[0].Portrait);
        Assert.AreEqual(7, bawuMapped.All[0].Level);
        Assert.AreEqual(0, bawuFallback.All.Count);

        Assert.IsTrue(forumDetailTrue.HasBaWu);
        Assert.IsFalse(forumDetailFalse.HasBaWu);
        Assert.IsFalse(forumDetailNull.HasBaWu);
        Assert.AreEqual("csharp", forumDetailTrue.Fname);
        Assert.AreEqual("plain", forumDetailFalse.Fname);
        Assert.AreEqual("null-election", forumDetailNull.Fname);

        Assert.AreEqual(1711111111L, notifyParsed.CreateTime);
        Assert.AreEqual(0L, notifyFallback.CreateTime);
        Assert.AreEqual(100L, notifyParsed.GroupId);
        Assert.AreEqual(5, notifyParsed.GroupType);

        Assert.AreEqual(2, oldUsersMapped.Count);
        Assert.AreEqual(2, oldUsersMapped.Page.CurrentPage);
        Assert.IsTrue(oldUsersMapped.Page.HasMore);
        Assert.IsFalse(oldUsersMapped.Page.HasPrevious);
        Assert.AreEqual("tb.1.old", oldUsersMapped[0].Portrait);
        Assert.AreEqual(7, oldUsersMapped[0].UntilTime);
        Assert.AreEqual("tb.1.raw", oldUsersMapped[1].Portrait);
        Assert.AreEqual(8, oldUsersMapped[1].UntilTime);
        Assert.AreEqual(1, oldUsersFallback.Count);
        Assert.AreEqual(0, oldUsersFallback.Page.CurrentPage);
        Assert.IsFalse(oldUsersFallback.Page.HasMore);
        Assert.IsFalse(oldUsersFallback.Page.HasPrevious);

        Assert.AreEqual(1, selfFollowMapped.Count);
        Assert.AreEqual(7356044UL, selfFollowMapped[0].Fid);
        Assert.AreEqual("csharp", selfFollowMapped[0].Fname);
        Assert.AreEqual(7, selfFollowMapped[0].Level);
        Assert.AreEqual(2, selfFollowMapped.Page.CurrentPage);
        Assert.AreEqual(4, selfFollowMapped.Page.TotalPage);
        Assert.IsTrue(selfFollowMapped.Page.HasMore);
        Assert.IsTrue(selfFollowMapped.Page.HasPrevious);

        Assert.AreEqual(0, selfFollowFallback.Count);
        Assert.AreEqual(0, selfFollowFallback.Page.CurrentPage);
        Assert.AreEqual(0, selfFollowFallback.Page.TotalPage);
        Assert.IsFalse(selfFollowFallback.Page.HasMore);
        Assert.IsFalse(selfFollowFallback.Page.HasPrevious);

        Assert.AreEqual(0, selfFollowPartialPageFallback.Count);
        Assert.AreEqual(0, selfFollowPartialPageFallback.Page.CurrentPage);
        Assert.AreEqual(0, selfFollowPartialPageFallback.Page.TotalPage);
        Assert.IsFalse(selfFollowPartialPageFallback.Page.HasMore);
        Assert.IsFalse(selfFollowPartialPageFallback.Page.HasPrevious);
    }

    [TestMethod]
    public async Task TinyBranchFamilyLoginAndRecoverPage_CoverLoginParseAndPageFallbacks()
    {
        var successCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user":{"id":42,"portrait":"tb.1.login?012345678901","name":"login-user"},"anti":{"tbs":"tbs-123"}}
                              """
        };
        var success = await new Login(successCore).RequestAsync();

        var missingUserCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","anti":{"tbs":"tbs-123"}}
                              """
        };
        var missingAntiCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user":{"id":42,"portrait":"tb.1.login?012345678901","name":"login-user"}}
                              """
        };
        var missingUserException =
            await ThrowsAsync<NullReferenceException>(() => new Login(missingUserCore).RequestAsync());
        var missingAnti = await new Login(missingAntiCore).RequestAsync();

        var recoverPageNull = RecoverPageMapper.FromTbData(null);
        var recoverPageFalse = RecoverPageMapper.FromTbData(new JObject());

        Assert.AreEqual("/c/s/login", successCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(Const.MainVersion, successCore.GetAppFormValue("_client_version"));
        Assert.AreEqual(successCore.Account!.Bduss, successCore.GetAppFormValue("bdusstoken"));
        Assert.AreEqual(42L, success.User.UserId);
        Assert.AreEqual("tb.1.login", success.User.Portrait);
        Assert.AreEqual("login-user", success.User.UserName);
        Assert.AreEqual("tbs-123", success.Tbs);
        Assert.IsNotNull(missingUserException);
        Assert.AreEqual(42L, missingAnti.User.UserId);
        Assert.AreEqual("tb.1.login", missingAnti.User.Portrait);
        Assert.AreEqual("login-user", missingAnti.User.UserName);
        Assert.IsNull(missingAnti.Tbs);

        Assert.AreEqual(0, recoverPageNull.PageSize);
        Assert.AreEqual(0, recoverPageNull.CurrentPage);
        Assert.IsFalse(recoverPageNull.HasMore);
        Assert.IsFalse(recoverPageNull.HasPrevious);
        Assert.AreEqual(0, recoverPageFalse.PageSize);
        Assert.AreEqual(0, recoverPageFalse.CurrentPage);
        Assert.IsFalse(recoverPageFalse.HasMore);
        Assert.IsFalse(recoverPageFalse.HasPrevious);
    }

    [TestMethod]
    public void TinyBranchFamilyModels_CoverTextBranchesAndGuards()
    {
        var memberUsers = new MemberUsers(
            [new MemberUser { UserName = "member", Portrait = "tb.1.member", Level = 3 }],
            new MemberUsersPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false });
        var memberUsersNoMore = new MemberUsers([], new MemberUsersPage { CurrentPage = 2, TotalPage = 2 });
        var postWithSign = new ThreadPost
        {
            Content = CreateContent("post body"),
            Sign = "sign",
            Pid = 10,
            AuthorId = 11,
            Floor = 1
        };
        var postWithoutSign = new ThreadPost
        {
            Content = CreateContent("plain body"), Pid = 12, AuthorId = 13, Floor = 2
        };
        var recoverWithTitle = new RecoverInfo
        {
            Content = CreateContent("recover body"),
            Title = "recover title",
            Tid = 20,
            Pid = 21,
            User = new RecoverUser { UserName = "recover-user", Portrait = "tb.1.recover" }
        };
        var recoverWithoutTitle = new RecoverInfo
        {
            Content = CreateContent("recover plain"),
            Tid = 22,
            Pid = 23,
            User = new RecoverUser { UserName = "recover-user-2", Portrait = "tb.1.recover-2" }
        };
        var shareWithTitle = new ShareThread
        {
            Content = CreateContent("shared body"),
            Title = "shared title",
            AuthorId = 30,
            Fid = 31,
            Fname = "forum",
            Tid = 32,
            Pid = 33
        };
        var shareWithoutTitle = new ShareThread
        {
            Content = CreateContent("shared plain"),
            AuthorId = 34,
            Fid = 35,
            Fname = "forum2",
            Tid = 36,
            Pid = 37
        };
        var threadWithTitle = new ThreadModel
        {
            Content = CreateContent("thread body"),
            Title = "thread title",
            Fid = 40,
            Fname = "forum",
            Tid = 41,
            Pid = 42,
            AuthorId = 43,
            Type = 71,
            VirtualImage = new VirtualImagePf()
        };
        var threadWithoutTitle = new ThreadModel
        {
            Content = CreateContent("thread plain"),
            Fid = 44,
            Fname = "forum2",
            Tid = 45,
            Pid = 46,
            AuthorId = 47,
            Type = 70,
            VirtualImage = new VirtualImagePf()
        };
        var userThreadWithTitle = new UserThread
        {
            Contents = CreateContent("user thread body"),
            Title = "user thread title",
            Fid = 50,
            Fname = "forum",
            Tid = 51,
            Pid = 52,
            Type = 71
        };
        var userThreadWithoutTitle = new UserThread
        {
            Contents = CreateContent("user thread plain"),
            Fid = 53,
            Fname = "forum2",
            Tid = 54,
            Pid = 55,
            Type = 70
        };

        Assert.AreEqual(1, memberUsers.Count);
        Assert.IsTrue(memberUsers.HasMore);
        Assert.IsFalse(memberUsersNoMore.HasMore);
        Assert.ThrowsExactly<ArgumentNullException>(() => new MemberUsers([], null!));

        Assert.AreEqual("post body\nsign", postWithSign.Text);
        Assert.AreEqual("plain body", postWithoutSign.Text);

        Assert.AreEqual("recover title\nrecover body", recoverWithTitle.Text);
        Assert.AreEqual("recover plain", recoverWithoutTitle.Text);

        Assert.AreEqual("shared title\n" + shareWithTitle.Content.Texts, shareWithTitle.Text);
        Assert.AreEqual(shareWithoutTitle.Content.Texts.ToString() ?? string.Empty, shareWithoutTitle.Text);

        Assert.AreEqual("thread title\nthread body", threadWithTitle.Text);
        Assert.AreEqual("thread plain", threadWithoutTitle.Text);
        Assert.IsTrue(threadWithTitle.IsHelp);
        Assert.IsFalse(threadWithoutTitle.IsHelp);

        Assert.AreEqual("user thread title\nuser thread body", userThreadWithTitle.Text);
        Assert.AreEqual("user thread plain", userThreadWithoutTitle.Text);
        Assert.IsTrue(userThreadWithTitle.IsHelp);
        Assert.IsFalse(userThreadWithoutTitle.IsHelp);
    }

    [TestMethod]
    public void TinyBranchFamilyConst_CoversBase32BranchesAndVersionAccess()
    {
        Assert.AreEqual(0, Const.BASE32_LEN(0));
        Assert.AreEqual(8, Const.BASE32_LEN(1));
        Assert.AreEqual(32, Const.BASE32_LEN(20));
        Assert.AreEqual(40, Const.BASE32_LEN(21));
        Assert.AreEqual(32, Const.TbcSha1Base32Size);
        Assert.IsFalse(string.IsNullOrWhiteSpace(Const.Version));
        Assert.AreEqual("12.64.1.1", Const.MainVersion);
        Assert.AreEqual("12.35.1.0", Const.PostVersion);
    }

    private static Content CreateContent(string text)
    {
        return new Content { Texts = [new FragText { Text = text }], Frags = [new FragText { Text = text }] };
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
        throw new InvalidOperationException();
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public Account? Account { get; private set; } = new(new string('b', 192));

        public HttpClient HttpClient { get; } = new();

        public string AppFormResponse { get; init; } = string.Empty;

        public Uri? LastAppFormUri { get; private set; }

        public IReadOnlyList<KeyValuePair<string, string>>? LastAppFormData { get; private set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = data.ToArray();
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public string GetAppFormValue(string key)
        {
            return LastAppFormData!.Last(entry => entry.Key == key).Value;
        }
    }
}
