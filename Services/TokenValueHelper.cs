using MemoryMCP.Models;

namespace MemoryMCP.Services;

public static class TokenValueHelper
{
    public static string ComputeSearchValue(PropertyType type, int? intValue, bool? boolValue, string? stringValue, float? floatValue, DateTime? dateTimeValue)
    {
        return type switch
        {
            PropertyType.Int => intValue?.ToString() ?? string.Empty,
            PropertyType.Bool => boolValue?.ToString()?.ToLowerInvariant() ?? string.Empty,
            PropertyType.String => stringValue?.Trim().ToLowerInvariant() ?? string.Empty,
            PropertyType.Float => floatValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            PropertyType.DateTime => dateTimeValue?.ToString("O") ?? string.Empty,
            _ => string.Empty
        };
    }

    public static string FormatDisplayValue(Token token)
    {
        return token.Type switch
        {
            PropertyType.Int => token.IntValue?.ToString() ?? string.Empty,
            PropertyType.Bool => token.BoolValue?.ToString() ?? string.Empty,
            PropertyType.String => token.StringValue ?? string.Empty,
            PropertyType.Float => token.FloatValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            PropertyType.DateTime => token.DateTimeValue?.ToString("O") ?? string.Empty,
            _ => string.Empty
        };
    }

    public static TokenSummaryDto ToSummary(Token token) =>
        new(token.Id, token.Property, token.Type, FormatDisplayValue(token), token.Confidence, token.Source, token.Status, token.SupersedesTokenId, token.SupersededByTokenId);

    public static void ApplyValues(Token token, PropertyType type, int? intValue, bool? boolValue, string? stringValue, float? floatValue, DateTime? dateTimeValue)
    {
        token.Type = type;
        token.IntValue = null;
        token.BoolValue = null;
        token.StringValue = null;
        token.FloatValue = null;
        token.DateTimeValue = null;

        switch (type)
        {
            case PropertyType.Int:
                token.IntValue = intValue;
                break;
            case PropertyType.Bool:
                token.BoolValue = boolValue;
                break;
            case PropertyType.String:
                token.StringValue = stringValue;
                break;
            case PropertyType.Float:
                token.FloatValue = floatValue;
                break;
            case PropertyType.DateTime:
                token.DateTimeValue = dateTimeValue;
                break;
        }

        token.SearchValue = ComputeSearchValue(type, token.IntValue, token.BoolValue, token.StringValue, token.FloatValue, token.DateTimeValue);
    }
}
