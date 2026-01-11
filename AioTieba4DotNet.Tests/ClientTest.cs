using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestSubject(typeof(Client))]
public class ClientTest
{
    [TestMethod]
    public void TestGetThreadsByFname()
    {
        var client = new Client();
        var threads = client.GetThreads("DNF", 200);
        threads.Wait();
        Console.WriteLine(threads.Result);
    }

    [TestMethod]
    public void TestGetThreadsByFid()
    {
        var client =
            new Client { ThreadIsGood = false, ThreadSortType = ThreadSortType.Reply, ThreadRn = 10 };
        var threads = client.GetThreads("DNF", 2);
        threads.Wait();
        Console.WriteLine("page:");
        Console.WriteLine(threads.Result.Page);
        Console.WriteLine("Forum:");

        Console.WriteLine(threads.Result.Forum);
        Console.WriteLine("Count:");
        Console.WriteLine(threads.Result.Objs.Count);
        // Console.WriteLine(threads.Result.Objs);
        //
        // foreach (var thread in threads.Result.Objs)
        // {
        //     // Console.WriteLine(thread);
        //     Console.WriteLine(thread);
        //
        //     // if (thread.Tid == 9221459238)
        //     // {
        //     //     Console.WriteLine(thread);
        //     // }
        // }
    }

    [TestMethod]
    public void TestGetFname()
    {
        var client = new Client();
        var fname = client.GetFname(81570);
        fname.Wait();
        Console.WriteLine(fname.Result);
        Console.WriteLine(fname.Result);
    }

    [TestMethod]
    public void TestReqUInfo()
    {
        var input = ReqUInfo.TiebaUid; // 假设输入是一个单独的标志，例如 USER_ID

        // 判断是否属于 BASIC
        var isBasic = (input & ReqUInfo.Basic) == input;

        // 判断是否属于 ALL
        var isAll = (input & ReqUInfo.All) == input;

        Console.WriteLine("是否属于 BASIC: " + isBasic);
        Console.WriteLine("是否属于 ALL: " + isAll);
    }

    [TestMethod]
    public void TestGetUserInfoWithUserNameOrPortrait()
    {
        var client = new Client();
        var getUserInfo = client.GetUserInfo("");
        getUserInfo.Wait();
        Console.WriteLine(getUserInfo.Result);
    }

    [TestMethod]
    public void TestGetUserInfoWithUserId()
    {
        var client = new Client();
        var getUserInfo = client.GetUserInfo(1);
        getUserInfo.Wait();
        Console.WriteLine(getUserInfo.Result);
    }

    [TestMethod]
    public void TestBlock()
    {
        var client = new Client(
            "",
            "");
        var block = client.Block("", "", 1, "test");
        block.Wait();
        Console.WriteLine(block.Result);
    }
}
