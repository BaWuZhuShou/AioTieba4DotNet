using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumMapper
{
    internal static Forum FromTbData(IDictionary<string, object> dataMap)

        {

            var fid = Convert.ToInt64(dataMap["id"]);

            var fname = dataMap["name"] as string ?? "";

            var category = dataMap["first_class"] as string ?? "";

            var subcategory = dataMap["second_class"] as string ?? "";

            var smallAvatar = dataMap["avatar"] as string ?? "";

            var slogan = dataMap["slogan"] as string ?? "";

            var memberNum = Convert.ToInt32(dataMap["member_num"]);

            var postNum = Convert.ToInt32(dataMap["post_num"]);

            var threadNum = Convert.ToInt32(dataMap["thread_num"]);

            var hasBaWu = dataMap.ContainsKey("managers");



            return new Forum

            {

                Fid = fid,

                Fname = fname,

                Category = category,

                Subcategory = subcategory,

                SmallAvatar = smallAvatar,

                Slogan = slogan,

                MemberNum = memberNum,

                PostNum = postNum,

                ThreadNum = threadNum,

                HasBaWu = hasBaWu

            };

        }
}
