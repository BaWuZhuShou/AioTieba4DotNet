using System.Globalization;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserProtoMapping
{
    internal static string NormalizePortrait(string? portrait)
    {
        var value = portrait ?? string.Empty;
        var suffixIndex = value.IndexOf('?', StringComparison.Ordinal);
        return suffixIndex >= 0 ? value[..suffixIndex] : value;
    }

    internal static long ParseTiebaUid(string? tiebaUid)
    {
        return long.TryParse(tiebaUid, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    internal static float ParseTbAge(string? tbAge)
    {
        return float.TryParse(tbAge, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    internal static List<string> MapIconNames(User data)
    {
        return data.Iconinfo.Where(static icon => !string.IsNullOrEmpty(icon.Name))
            .Select(static icon => icon.Name)
            .ToList();
    }

    internal static bool MapIsVip(User data)
    {
        return data.NewTshowIcon.Count != 0 || data.VipInfo is { VStatus: not 0 };
    }

    internal static bool MapIsGod(User data)
    {
        return data.NewGodData is { Status: not 0 };
    }

    internal static PrivLike MapPrivLike(User.Types.PrivSets? privSets)
    {
        return privSets is { Like: not 0 } ? (PrivLike)privSets.Like : PrivLike.Public;
    }

    internal static PrivReply MapPrivReply(User.Types.PrivSets? privSets)
    {
        return privSets is { Reply: not 0 } ? (PrivReply)privSets.Reply : PrivReply.All;
    }
}
