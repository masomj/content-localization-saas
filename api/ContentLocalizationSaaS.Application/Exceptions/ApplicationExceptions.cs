using FluentValidation.Results;

namespace ContentLocalizationSaaS.Application.Exceptions;

public sealed class ResourceNotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.")
{
    public string Resource { get; } = resource;
    public object Key { get; } = key;
}

/// <summary>
/// Thrown when a billing operation is attempted in an environment where billing is switched off
/// (e.g. the shared dev environment). Mapped to 403 by the API exception middleware.
/// </summary>
public sealed class BillingDisabledException()
    : Exception("Billing is disabled in this environment.");

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static RequestValidationException FromFailures(IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).Distinct().ToArray());

        return new RequestValidationException(errors);
    }
}
