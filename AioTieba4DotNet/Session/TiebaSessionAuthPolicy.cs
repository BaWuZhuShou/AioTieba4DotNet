using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Session;

internal static class TiebaSessionAuthPolicy
{
    internal static TiebaAuthenticationException CreateMissingCredentialsException(string operationName) =>
        new($"Operation '{operationName}' requires an authenticated session with BDUSS.");

    internal static TiebaConfigurationException CreateMissingSessionStateException(string operationName,
        string stateName) =>
        new($"Operation '{operationName}' requires session state '{stateName}' to be initialized.");
}
