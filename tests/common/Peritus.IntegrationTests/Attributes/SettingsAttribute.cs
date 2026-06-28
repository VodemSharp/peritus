namespace Peritus.IntegrationTests.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SettingsAttribute(string key, string? value) : Attribute
{
    public string Key { get; } = key;
    public string? Value { get; } = value;
}
