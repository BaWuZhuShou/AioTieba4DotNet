using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class TiebaOptionsValidator
{
    internal static void Validate(TiebaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Bduss) && !string.IsNullOrWhiteSpace(options.Stoken))
            throw new TiebaConfigurationException("Stoken cannot be supplied without Bduss.");

        if (options.MaxReadRetryAttempts < 0)
            throw new TiebaConfigurationException("MaxReadRetryAttempts must be greater than or equal to 0.");

        if (options.RequestTimeout != Timeout.InfiniteTimeSpan && options.RequestTimeout <= TimeSpan.Zero)
            throw new TiebaConfigurationException(
                "RequestTimeout must be positive or Timeout.InfiniteTimeSpan.");
    }
}
