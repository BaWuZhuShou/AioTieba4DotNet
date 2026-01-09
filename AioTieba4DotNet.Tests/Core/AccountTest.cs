using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(Account))]
public class AccountTest
{

    [TestMethod]
    public void TestXyus()
    {
        var account = new Account();
        account.AndroidId="5cae590c1ee17ff1";
        account.Uuid="6031f0e4-99b0-4c09-a21f-43af72db4426";
        Console.WriteLine(account.AndroidId);
    }
    
}