using AioTieba4DotNet.Contracts;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class TiebaOptionsValidator
{
    internal static void Validate(TiebaOptions? options)
    {
        var validationErrors = GetValidationErrors(options);
        if (validationErrors.Count > 0)
            throw new TiebaConfigurationException(string.Join(" ", validationErrors));
    }

    internal static IReadOnlyList<string> GetValidationErrors(TiebaOptions? options)
    {
        if (options is null) return ["TiebaOptions configuration is required."];

        List<string> validationErrors = [];

        if (string.IsNullOrWhiteSpace(options.Bduss) && !string.IsNullOrWhiteSpace(options.Stoken))
            validationErrors.Add("Stoken cannot be supplied without Bduss.");

        if (options.MaxReadRetryAttempts < 0)
            validationErrors.Add("MaxReadRetryAttempts must be greater than or equal to 0.");

        if (options.RequestTimeout != Timeout.InfiniteTimeSpan && options.RequestTimeout <= TimeSpan.Zero)
            validationErrors.Add("RequestTimeout must be positive or Timeout.InfiniteTimeSpan.");

        return validationErrors;
    }
}
