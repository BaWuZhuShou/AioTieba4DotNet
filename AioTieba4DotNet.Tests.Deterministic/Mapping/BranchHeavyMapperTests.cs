#nullable enable
using System;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Forums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class BranchHeavyMapperTests
{
    [TestMethod]
    public void UserForumInfoMapper_MapsNestedObjectsAndDefaultsMissingFields()
    {
        var mapped = UserForumInfoMapper.FromTbData(new JObject
        {
            ["user_info"] = new JObject
            {
                ["id"] = 42,
                ["name"] = "user",
                ["name_show"] = "User Show",
                ["portrait"] = "tb.1.user?012345678901"
            },
            ["user_forum_info"] = new JObject
            {
                ["is_follow"] = 1,
                ["follow_days"] = 7,
                ["sign_days"] = 8,
                ["thread_num"] = 9,
                ["day_post_num"] = 10,
                ["member_no"] = 11,
                ["day_sign_no"] = 12,
                ["level_id"] = 13,
                ["level_name"] = "Lv13",
                ["cur_score"] = 14,
                ["levelup_score"] = 15,
                ["role_name"] = "Moderator",
                ["identify"] = "badge",
                ["high_light_sign_days"] = 16
            },
            ["forum_info"] = new JObject
            {
                ["forum_name"] = "csharp",
                ["forum_avatar"] = "avatar.png"
            }
        });
        var fallback = UserForumInfoMapper.FromTbData(new JObject());

        Assert.AreEqual(42L, mapped.User?.UserId);
        Assert.AreEqual("tb.1.user", mapped.User?.Portrait);
        Assert.IsTrue(mapped.IsFollow);
        Assert.AreEqual(7, mapped.FollowDays);
        Assert.AreEqual(16, mapped.HighLightSignDays);
        Assert.AreEqual("csharp", mapped.Fname);
        Assert.AreEqual("avatar.png", mapped.SmallAvatar);
        Assert.IsFalse(fallback.IsFollow);
        Assert.AreEqual(string.Empty, fallback.Fname);
    }

    [TestMethod]
    public void UserForumInfoMapper_HandlesPartialNullishNestedObjects()
    {
        var mapped = UserForumInfoMapper.FromTbData(new JObject
        {
            ["user_info"] = new JObject
            {
                ["id"] = 7,
                ["portrait"] = "tb.1.partial",
                ["name"] = "partial-user",
                ["is_like"] = 1
            },
            ["user_forum_info"] = new JObject
            {
                ["is_follow"] = 0,
                ["follow_days"] = 0,
                ["sign_days"] = 2,
                ["thread_num"] = 3,
                ["day_post_num"] = 4,
                ["member_no"] = 5,
                ["day_sign_no"] = 6,
                ["level_id"] = 7,
                ["level_name"] = JValue.CreateNull(),
                ["cur_score"] = 8,
                ["levelup_score"] = 9,
                ["role_name"] = JValue.CreateNull(),
                ["identify"] = JValue.CreateNull(),
                ["high_light_sign_days"] = 10
            },
            ["forum_info"] = new JObject
            {
                ["forum_name"] = JValue.CreateNull(),
                ["forum_avatar"] = JValue.CreateNull()
            }
        });

        Assert.AreEqual(7L, mapped.User.UserId);
        Assert.AreEqual("partial-user", mapped.User.NickNameNew);
        Assert.IsTrue(mapped.User.IsLike);
        Assert.IsFalse(mapped.IsFollow);
        Assert.AreEqual(2, mapped.SignDays);
        Assert.AreEqual(10, mapped.HighLightSignDays);
        Assert.AreEqual(string.Empty, mapped.LevelName);
        Assert.AreEqual(string.Empty, mapped.RoleName);
        Assert.AreEqual(string.Empty, mapped.Identify);
        Assert.AreEqual(string.Empty, mapped.Fname);
        Assert.AreEqual(string.Empty, mapped.SmallAvatar);
    }

    [TestMethod]
    public void UserForumInfoMapper_HandlesPresentObjectsWithMissingFields()
    {
        var mapped = UserForumInfoMapper.FromTbData(new JObject
        {
            ["user_info"] = new JObject
            {
                ["id"] = 9,
                ["name"] = "present-user"
            },
            ["user_forum_info"] = new JObject(),
            ["forum_info"] = new JObject()
        });

        Assert.AreEqual(9L, mapped.User.UserId);
        Assert.AreEqual("present-user", mapped.User.NickNameNew);
        Assert.AreEqual(string.Empty, mapped.User.UserName);
        Assert.IsFalse(mapped.IsFollow);
        Assert.AreEqual(0, mapped.FollowDays);
        Assert.AreEqual(0, mapped.SignDays);
        Assert.AreEqual(0, mapped.ThreadNum);
        Assert.AreEqual(0, mapped.DayPostNum);
        Assert.AreEqual(0, mapped.MemberRank);
        Assert.AreEqual(0, mapped.DaySignRank);
        Assert.AreEqual(0, mapped.Level);
        Assert.AreEqual(string.Empty, mapped.LevelName);
        Assert.AreEqual(0, mapped.Exp);
        Assert.AreEqual(0, mapped.LevelupExp);
        Assert.AreEqual(string.Empty, mapped.RoleName);
        Assert.AreEqual(string.Empty, mapped.Identify);
        Assert.AreEqual(0, mapped.HighLightSignDays);
        Assert.AreEqual(string.Empty, mapped.Fname);
        Assert.AreEqual(string.Empty, mapped.SmallAvatar);
    }

    [TestMethod]
    public void UserInfoPanelMapper_HandlesVipPortraitAndAgeBranches()
    {
        var vipMale = UserInfoPanelMapper.FromTbData(new JObject
        {
            ["portrait"] = "tb.1.user?012345678901",
            ["name"] = "user",
            ["show_nickname"] = "New",
            ["name_show"] = "Old",
            ["gender"] = "male",
            ["tb_age"] = "-",
            ["vipInfo"] = new JObject { ["v_status"] = 3 },
            ["post_num"] = "1.2万",
            ["followed_count"] = "3.4万"
        });
        var normalFemale = UserInfoPanelMapper.FromTbData(new JObject
        {
            ["portrait"] = "plain-portrait",
            ["gender"] = "female",
            ["tb_age"] = "2.5",
            ["vipInfo"] = JValue.CreateNull(),
            ["post_num"] = "12",
            ["followed_count"] = "34"
        });

        Assert.AreEqual("tb.1.user", vipMale.Portrait);
        Assert.IsTrue(vipMale.IsVip);
        Assert.AreEqual(Gender.Male, vipMale.Gender);
        Assert.AreEqual(0F, vipMale.Age);
        Assert.AreEqual(12000, vipMale.PostNum);
        Assert.AreEqual(34000, vipMale.FanNum);
        Assert.AreEqual(Gender.Female, normalFemale.Gender);
        Assert.IsFalse(normalFemale.IsVip);
        Assert.AreEqual(2.5F, normalFemale.Age);
        Assert.AreEqual("plain-portrait", normalFemale.Portrait);
    }

    [TestMethod]
    public void UserInfoPanelMapper_HandlesMissingFieldsAndNonObjectVipInfo()
    {
        var mapped = UserInfoPanelMapper.FromTbData(new JObject
        {
            ["vipInfo"] = "unexpected",
            ["post_num"] = JValue.CreateNull(),
            ["followed_count"] = JValue.CreateNull()
        });

        Assert.AreEqual(string.Empty, mapped.Portrait);
        Assert.AreEqual(string.Empty, mapped.UserName);
        Assert.AreEqual(string.Empty, mapped.NickNameNew);
        Assert.AreEqual(string.Empty, mapped.NickNameOld);
        Assert.AreEqual(Gender.Female, mapped.Gender);
        Assert.AreEqual(0F, mapped.Age);
        Assert.IsFalse(mapped.IsVip);
        Assert.AreEqual(0, mapped.PostNum);
        Assert.AreEqual(0, mapped.FanNum);
    }

    [TestMethod]
    public void AtMessageMapper_HandlesMissingFieldsAndNonObjectReplyer()
    {
        var mapped = AtMessageMapper.FromTbData(new JObject
        {
            ["content"] = JValue.CreateNull(),
            ["replyer"] = "unexpected",
            ["is_floor"] = 0,
            ["is_first_post"] = 0
        });

        Assert.AreEqual(string.Empty, mapped.Content);
        Assert.AreEqual(string.Empty, mapped.Fname);
        Assert.AreEqual(0L, mapped.ThreadId);
        Assert.AreEqual(0L, mapped.PostId);
        Assert.IsNull(mapped.Replyer);
        Assert.IsFalse(mapped.IsFloor);
        Assert.IsFalse(mapped.IsFirstPost);
        Assert.AreEqual(0L, mapped.Time);
    }

    [TestMethod]
    public void ExactSearchesMapper_MapsPostsPageAndNullGuards()
    {
        var mapped = ExactSearchesMapper.FromTbData(new JObject
        {
            ["post_list"] = new JArray
            {
                new JObject
                {
                    ["content"] = "body",
                    ["title"] = "title",
                    ["fname"] = "forum",
                    ["tid"] = 11,
                    ["pid"] = 22,
                    ["author"] = new JObject { ["name_show"] = "Author" },
                    ["is_floor"] = 1,
                    ["time"] = 1711111111
                }
            },
            ["page"] = new JObject
            {
                ["page_size"] = 20,
                ["current_page"] = 2,
                ["total_page"] = 3,
                ["total_count"] = 40,
                ["has_more"] = 1,
                ["has_prev"] = 0
            }
        });
        var fallback = ExactSearchesMapper.FromTbData(new JObject());

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual("body", mapped[0].Text);
        Assert.AreEqual("Author", mapped[0].ShowName);
        Assert.IsTrue(mapped[0].IsComment);
        Assert.AreEqual(2, mapped.Page.CurrentPage);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsFalse(mapped.Page.HasPrevious);
        Assert.AreEqual(0, fallback.Count);
        Assert.AreEqual(0, fallback.Page.TotalCount);
        Assert.ThrowsExactly<ArgumentNullException>(() => ExactSearchesMapper.FromTbData(null!));
    }

    [TestMethod]
    public void ExactSearchesMapper_HandlesMissingAuthorAndBooleanPagingFallbacks()
    {
        var mapped = ExactSearchesMapper.FromTbData(new JObject
        {
            ["post_list"] = new JArray
            {
                new JObject
                {
                    ["is_floor"] = 0,
                    ["author"] = new JObject()
                }
            },
            ["page"] = new JObject
            {
                ["has_more"] = 0,
                ["has_prev"] = 1
            }
        });

        Assert.AreEqual(string.Empty, mapped[0].Text);
        Assert.AreEqual(string.Empty, mapped[0].Title);
        Assert.AreEqual(string.Empty, mapped[0].Fname);
        Assert.AreEqual(0L, mapped[0].Tid);
        Assert.AreEqual(0L, mapped[0].Pid);
        Assert.AreEqual(string.Empty, mapped[0].ShowName);
        Assert.IsFalse(mapped[0].IsComment);
        Assert.IsFalse(mapped.Page.HasMore);
        Assert.IsTrue(mapped.Page.HasPrevious);
        Assert.IsFalse(mapped[0].Equals(new ExactSearch { Pid = 1 }));
    }

    [TestMethod]
    public void BlocksMapper_MapsPageFallbacksAndBooleanTokenShapes()
    {
        var fromBoolean = BlocksMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["content"] = "<li><a attr-uid=\"42\" attr-un=\"target-user\" attr-nn=\"Target\" attr-blockday=\"3\"></a></li>",
                ["page"] = new JObject
                {
                    ["size"] = 20,
                    ["pn"] = 2,
                    ["total_page"] = 5,
                    ["total_count"] = 91,
                    ["have_next"] = true
                }
            }
        });
        var fromInteger = BlocksMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["content"] = string.Empty,
                ["page"] = new JObject
                {
                    ["pn"] = 1,
                    ["have_next"] = 0
                }
            }
        });

        Assert.AreEqual(1, fromBoolean.Count);
        Assert.AreEqual(42L, fromBoolean[0].UserId);
        Assert.AreEqual("target-user", fromBoolean[0].UserName);
        Assert.AreEqual("Target", fromBoolean[0].NickNameOld);
        Assert.IsTrue(fromBoolean.Page.HasMore);
        Assert.IsTrue(fromBoolean.Page.HasPrevious);
        Assert.AreEqual(0, fromInteger.Count);
        Assert.IsFalse(fromInteger.Page.HasMore);
        Assert.IsFalse(fromInteger.Page.HasPrevious);
        Assert.ThrowsExactly<ArgumentNullException>(() => BlocksMapper.FromTbData(null!));
    }

    [TestMethod]
    public void BlocksMapper_HandlesMissingPayloadAndUnexpectedPagingTokens()
    {
        var missingPayload = BlocksMapper.FromTbData(new JObject());
        var unexpectedToken = BlocksMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["page"] = new JObject
                {
                    ["have_next"] = "unexpected"
                }
            }
        });

        Assert.AreEqual(0, missingPayload.Count);
        Assert.AreEqual(0, missingPayload.Page.CurrentPage);
        Assert.IsFalse(missingPayload.Page.HasMore);
        Assert.IsFalse(unexpectedToken.Page.HasMore);
        Assert.IsFalse(unexpectedToken.Page.HasPrevious);
    }

    [TestMethod]
    public void BawuPermMapper_HandlesSwitchTypesUnknownPermsAndNullArrays()
    {
        var mapped = BawuPermMapper.FromTbData(new JObject
        {
            ["perm_setting"] = new JObject
            {
                ["category_user"] = new JArray
                {
                    new JObject { ["switch"] = "1", ["perm"] = 5 },
                    new JObject { ["switch"] = 0, ["perm"] = 4 },
                    new JObject { ["switch"] = true, ["perm"] = 99 }
                },
                ["category_thread"] = new JArray
                {
                    new JObject { ["switch"] = false, ["perm"] = 3 },
                    new JObject { ["switch"] = "yes", ["perm"] = 2 }
                }
            }
        });
        var fallback = BawuPermMapper.FromTbData(new JObject { ["perm_setting"] = new JObject { ["category_user"] = new JObject() } });

        Assert.AreEqual(BawuPermType.UnblockAppeal | BawuPermType.RecoverAppeal, mapped.Permissions);
        Assert.AreEqual(BawuPermType.None, fallback.Permissions);
        Assert.ThrowsExactly<ArgumentNullException>(() => BawuPermMapper.FromTbData(null!));
    }

    [TestMethod]
    public void BawuPermMapper_CoversNullSwitchTokens_MissingPerms_AndAllEnabledFlags()
    {
        var mapped = BawuPermMapper.FromTbData(new JObject
        {
            ["perm_setting"] = new JObject
            {
                ["category_user"] = new JArray
                {
                    new JObject { ["switch"] = JValue.CreateNull(), ["perm"] = 4 },
                    new JObject { ["switch"] = true },
                    new JObject { ["switch"] = true, ["perm"] = 4 },
                    new JObject { ["switch"] = true, ["perm"] = 5 }
                },
                ["category_thread"] = new JArray
                {
                    new JObject { ["switch"] = true, ["perm"] = 2 },
                    new JObject { ["switch"] = true, ["perm"] = 3 }
                }
            }
        });

        Assert.AreEqual(BawuPermType.RecoverAppeal | BawuPermType.Recover | BawuPermType.Unblock |
                        BawuPermType.UnblockAppeal, mapped.Permissions);
    }

    [TestMethod]
    public void BawuBlacklistUsersMapper_HandlesMalformedRows_AndNullInput()
    {
        var mapped = BawuBlacklistUsersMapper.FromTbData("""
                                               <div class="breadcrumbs"><em>1</em></div>
                                               <div class="tbui_pagination"><ul><li class="active">1</li><li>(1)</li></ul></div>
                                               <table><tbody>
                                               <tr>
                                                 <td class="left_cell"><a data-user-name="target-user" data-user-id="42" href="/home/main?id=tb.1.target&amp;fr=home">target-user</a></td>
                                               </tr>
                                               <tr>
                                                 <td class="left_cell"><a data-user-name="" data-user-id="43" href="/home/main?id=tb.1.blank">blank</a></td>
                                               </tr>
                                               <tr>
                                                 <td class="left_cell"><a data-user-name="bad-id" data-user-id="oops" href="/home/main?id=tb.1.badid">bad-id</a></td>
                                               </tr>
                                               <tr>
                                                 <td class="left_cell"><a data-user-name="missing-href" data-user-id="44">missing</a></td>
                                               </tr>
                                               </tbody></table>
                                               """);

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual(42L, mapped[0].UserId);
        Assert.AreEqual("tb.1.target", mapped[0].Portrait);
        Assert.ThrowsExactly<ArgumentNullException>(() => BawuBlacklistUsersMapper.FromTbData(null!));
    }

    [TestMethod]
    public void BawuUserLogsMapper_HandlesShortRows_DurationFallbacks_AndNullInput()
    {
        var mapped = BawuUserLogsMapper.FromTbData("""
                                               <div class="breadcrumbs"><em>2</em></div>
                                               <div class="tbui_pagination"><ul><li class="active">1</li><li>(1)</li></ul></div>
                                               <table><tbody>
                                               <tr>
                                                 <td><a href="/home/main?id=tb.1.target#/">target</a></td>
                                                 <td>封禁</td>
                                                 <td>7 天</td>
                                                 <td>operator-user</td>
                                                 <td>2026-03-30 11:22</td>
                                               </tr>
                                               <tr>
                                                 <td><a href="/home/main?id=tb.1.target#/">target</a></td>
                                                 <td>封禁</td>
                                                 <td>3小时</td>
                                                 <td>operator-user</td>
                                                 <td>2026-03-30 11:22</td>
                                               </tr>
                                               <tr>
                                                 <td><a href="/home/main?id=tb.1.short#/">short</a></td>
                                                 <td>封禁</td>
                                                 <td>7 天</td>
                                                 <td>operator-user</td>
                                               </tr>
                                               </tbody></table>
                                               """);

        Assert.AreEqual(2, mapped.Count);
        Assert.AreEqual(7, mapped[0].OperationDurationDays);
        Assert.AreEqual(0, mapped[1].OperationDurationDays);
        Assert.AreEqual("tb.1.target", mapped[0].UserPortrait);
        Assert.ThrowsExactly<ArgumentNullException>(() => BawuUserLogsMapper.FromTbData(null!));
    }

    [TestMethod]
    public void RecoverUserMapper_HandlesNullInputAndPortraitNormalization()
    {
        var empty = RecoverUserMapper.FromTbData(null, "nickname");
        var mapped = RecoverUserMapper.FromTbData(new JObject
        {
            ["portrait"] = "tb.1.recover?from=app",
            ["user_name"] = "recover-user",
            ["show_nickname"] = "Recover Nick"
        }, "show_nickname");
        var noQuery = RecoverUserMapper.FromTbData(new JObject
        {
            ["portrait"] = "tb.1.raw",
            ["user_name"] = "raw-user"
        }, "missing");

        Assert.AreEqual(string.Empty, empty.UserName);
        Assert.AreEqual(string.Empty, empty.Portrait);
        Assert.AreEqual("tb.1.recover", mapped.Portrait);
        Assert.AreEqual("recover-user", mapped.UserName);
        Assert.AreEqual("Recover Nick", mapped.NickNameNew);
        Assert.AreEqual("tb.1.raw", noQuery.Portrait);
        Assert.AreEqual(string.Empty, noQuery.NickNameNew);
    }

    [TestMethod]
    public void AppealsMapper_MapsBooleanAndIntegerHasMoreBranches()
    {
        var fromBoolean = AppealsMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["appeal_list"] = new JArray
                {
                    new JObject
                    {
                        ["appeal_id"] = "1001",
                        ["appeal_reason"] = "reason",
                        ["appeal_time"] = "1711700000",
                        ["punish_reason"] = "spam",
                        ["punish_start_time"] = "1711600000",
                        ["punish_day_num"] = 3,
                        ["operate_man"] = "moderator",
                        ["user"] = new JObject
                        {
                            ["id"] = 42,
                            ["portrait"] = "tb.1.target?foo=bar",
                            ["name"] = "target-user",
                            ["name_show"] = "Target"
                        }
                    }
                },
                ["has_more"] = true
            }
        });
        var fromInteger = AppealsMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["appeal_list"] = new JArray
                {
                    new JObject
                    {
                        ["appeal_id"] = "bad",
                        ["appeal_time"] = "bad",
                        ["punish_start_time"] = "bad"
                    }
                },
                ["has_more"] = 0
            }
        });

        Assert.AreEqual(1, fromBoolean.Count);
        Assert.IsTrue(fromBoolean.HasMore);
        Assert.AreEqual(1001L, fromBoolean[0].AppealId);
        Assert.AreEqual("tb.1.target", fromBoolean[0].Portrait);
        Assert.AreEqual("Target", fromBoolean[0].NickName);
        Assert.IsFalse(fromInteger.HasMore);
        Assert.AreEqual(0L, fromInteger[0].AppealId);
        Assert.AreEqual(0L, fromInteger[0].AppealTime);
        Assert.AreEqual(0L, fromInteger[0].PunishTime);
        Assert.ThrowsExactly<ArgumentNullException>(() => AppealsMapper.FromTbData(null!));
    }

    [TestMethod]
    public void AppealsMapper_HandlesMissingPayloadStringHasMoreAndUserFallbacks()
    {
        var missingPayload = AppealsMapper.FromTbData(new JObject());
        var fallback = AppealsMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["appeal_list"] = new JArray
                {
                    new JObject
                    {
                        ["appeal_reason"] = "fallback"
                    }
                },
                ["has_more"] = "unexpected"
            }
        });

        Assert.AreEqual(0, missingPayload.Count);
        Assert.IsFalse(missingPayload.HasMore);
        Assert.AreEqual(1, fallback.Count);
        Assert.IsFalse(fallback.HasMore);
        Assert.AreEqual(0L, fallback[0].UserId);
        Assert.AreEqual(string.Empty, fallback[0].Portrait);
        Assert.AreEqual(string.Empty, fallback[0].UserName);
        Assert.AreEqual(string.Empty, fallback[0].NickName);
        Assert.AreEqual("fallback", fallback[0].AppealReason);
    }
}
