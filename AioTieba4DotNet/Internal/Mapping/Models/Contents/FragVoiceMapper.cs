using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragVoiceMapper
{
    internal static FragVoice FromTbData(Voice dataProto)

        {

            var md5 = dataProto.VoiceMd5;

            var duration = dataProto.DuringTime / 1000;

            return new FragVoice { Md5 = md5, Duration = duration };

        }



    internal static FragVoice FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)

        {

            var md5 = dataProto.VoiceMd5;

            var duration = int.Parse(dataProto.DuringTime) / 1000;

            return new FragVoice { Md5 = md5, Duration = duration };

        }



    internal static FragVoice FromTbData(PbContent dataProto)

        {

            return new FragVoice { Md5 = dataProto.VoiceMd5, Duration = (int)dataProto.DuringTime / 1000 };

        }
}
