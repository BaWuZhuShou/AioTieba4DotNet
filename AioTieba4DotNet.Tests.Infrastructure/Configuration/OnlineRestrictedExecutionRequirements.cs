#nullable enable
using System;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure.Configuration;

public static class OnlineRestrictedExecutionRequirements
{
    public static void RequireModeration(OnlineTestEnvironment environment, string operationName)
    {
        OnlineExecutionGate.RequireRestricted(environment, operationName, OnlineExecutionCapability.Moderation);
    }

    public static void RequireAdmin(OnlineTestEnvironment environment, string operationName)
    {
        OnlineExecutionGate.RequireRestricted(environment, operationName, OnlineExecutionCapability.Admin);
    }
}
