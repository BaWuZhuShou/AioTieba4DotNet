using System.Text;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ThreadsMapper
{
    internal static Threads FromTbData(FrsPageResIdl.Types.DataRes dataRes)

        {

            var forum = AioTieba4DotNet.Internal.Mapping.ForumTMapper.FromTbData(dataRes);

            var threads = dataRes.ThreadList.Select(AioTieba4DotNet.Internal.Mapping.ThreadMapper.FromTbData).ToList();

            var users = dataRes.UserList.ToDictionary(u => u.Id, AioTieba4DotNet.Internal.Mapping.UserInfoTMapper.FromTbData);

            foreach (var thread in threads)

            {

                thread.Fname = forum.Fname;

                thread.Fid = forum.Fid;

                thread.User = users.GetValueOrDefault(thread.AuthorId) ?? new UserInfoT();

            }



            return new Threads

            {

                Page = AioTieba4DotNet.Internal.Mapping.PageTMapper.FromTbData(dataRes.Page),

                Forum = forum,

                TabDictionary = dataRes.NavTabInfo.Tab.ToDictionary(p => p.TabName, p => p.TabId),

                Objs = threads

            };

        }
}
