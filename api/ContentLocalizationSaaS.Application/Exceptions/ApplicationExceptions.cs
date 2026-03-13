using FluentValidation.Results;

namespace ContentLocalizationSaaS.Application.Exceptions;

public sealed class ResourceNotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.")
{
    public string Resource { get; } = resource;
    public object Key { get; } = key;
}

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
