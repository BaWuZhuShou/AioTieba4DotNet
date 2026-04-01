namespace AioTieba4DotNet.Session;

internal static class TiebaSessionAuthPolicy
{
    internal static TiebaAuthenticationException CreateMissingCredentialsException(string operationName)
    {
        return new TiebaAuthenticationException(
            $"Operation '{operationName}' requires an authenticated session with BDUSS.");
    }

    internal static TiebaConfigurationException CreateMissingSessionStateException(string operationName,
        string stateName)
    {
        return new TiebaConfigurationException(
            $"Operation '{operationName}' requires session state '{stateName}' to be initialized.");
    }
}
