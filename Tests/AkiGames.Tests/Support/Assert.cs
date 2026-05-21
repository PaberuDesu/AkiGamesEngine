using System.Text.Json;

namespace AkiGames.Tests.Support;

internal static class Assert
{
    public static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void False(bool condition, string message = "Expected condition to be false.") =>
        True(!condition, message);

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                message ?? $"Expected {Format(expected)}, got {Format(actual)}."
            );
        }
    }

    public static void Same(object expected, object actual, string? message = null)
    {
        if (!ReferenceEquals(expected, actual))
            throw new InvalidOperationException(message ?? "Expected both references to point to the same instance.");
    }

    public static void NotSame(object expected, object actual, string? message = null)
    {
        if (ReferenceEquals(expected, actual))
            throw new InvalidOperationException(message ?? "Expected references to point to different instances.");
    }

    public static void Null(object? actual, string? message = null)
    {
        if (actual != null)
            throw new InvalidOperationException(message ?? $"Expected null, got {Format(actual)}.");
    }

    public static void NotNull(object? actual, string? message = null)
    {
        if (actual == null)
            throw new InvalidOperationException(message ?? "Expected a non-null value.");
    }

    public static void Contains(string expectedSubstring, string actual, string? message = null)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
            throw new InvalidOperationException(message ?? $"Expected text to contain {Format(expectedSubstring)}.");
    }

    public static void DoesNotContain(string expectedSubstring, string actual, string? message = null)
    {
        if (actual.Contains(expectedSubstring, StringComparison.Ordinal))
            throw new InvalidOperationException(message ?? $"Expected text not to contain {Format(expectedSubstring)}.");
    }

    public static JsonElement JsonProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            throw new InvalidOperationException($"Expected JSON property {propertyName}.");

        return value;
    }

    private static string Format<T>(T value) =>
        value?.ToString() ?? "<null>";
}
