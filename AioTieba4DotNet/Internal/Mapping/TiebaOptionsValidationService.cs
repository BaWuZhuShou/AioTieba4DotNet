using AioTieba4DotNet.Contracts;
using Microsoft.Extensions.Options;

namespace AioTieba4DotNet.Internal.Mapping;

internal sealed class TiebaOptionsValidationService : IValidateOptions<TiebaOptions>
{
    public ValidateOptionsResult Validate(string? name, TiebaOptions options)
    {
        if (!string.IsNullOrEmpty(name) && name != Options.DefaultName) return ValidateOptionsResult.Skip;

        var validationErrors = TiebaOptionsValidator.GetValidationErrors(options);
        return validationErrors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(validationErrors);
    }
}
