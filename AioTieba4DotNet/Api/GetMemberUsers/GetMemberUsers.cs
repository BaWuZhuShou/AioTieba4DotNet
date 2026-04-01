using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetMemberUsers;

[RequireBduss]
[PythonApi("aiotieba.api.get_member_users")]
internal sealed class GetMemberUsers(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<MemberUsers> RequestAsync(string fname, int pn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("word", fname), new("pn", pn.ToString()), new("ie", "utf-8")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/bawu2/platform/listMemberInfo").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, data, cancellationToken);
        return MemberUsersMapper.FromHtml(result);
    }
}
