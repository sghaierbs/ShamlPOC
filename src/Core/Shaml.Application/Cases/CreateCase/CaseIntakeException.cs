namespace Shaml.Application.Cases.CreateCase;

public sealed class CaseIntakeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}