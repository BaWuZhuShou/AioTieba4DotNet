using System;
using AioTieba4DotNet.Api.Entities.Contents;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Api.GetThreads;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetThreads.GetThreads))]
public class GetThreadsTest
{

    [TestMethod]
    public void TestRequest()
    {
        var httpCore = new HttpCore();
        var getThreads = new AioTieba4DotNet.Api.GetThreads.GetThreads(httpCore);
        var requestAsync = getThreads.RequestAsync("",1,30,5,0);
        requestAsync.Wait();
        var requestAsyncResult = requestAsync.Result;
        var obj = requestAsyncResult.Objs[4];
        Console.WriteLine(requestAsyncResult.Objs.Count);
        foreach (var frag in obj.Content.Frags)
        {
            switch (frag)
            {
                case FragAt fragAt:
                    Console.WriteLine(fragAt);
                    break;
                case FragEmoji fragEmoji:
                    Console.WriteLine(fragEmoji);
                    break;
                case FragImage fragImage:
                    Console.WriteLine(fragImage.Src);
                    break;
                case FragItem fragItem:
                    Console.WriteLine(fragItem);
                    break;
                case FragLink fragLink:
                    Console.WriteLine(fragLink);
                    break;
                case FragText fragText:
                    Console.WriteLine(fragText.Text);
                    break;
                case FragTiebaPlus fragTiebaPlus:
                    Console.WriteLine(fragTiebaPlus);
                    break;
                case FragVideo fragVideo:
                    Console.WriteLine(fragVideo);
                    break;
                case FragVoice fragVoice:
                    Console.WriteLine(fragVoice);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(frag));
            }
        }
        // Console.WriteLine(requestAsyncResult);
    }
}