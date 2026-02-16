using System;
using System.Text.Json;

namespace api.Helpers
{
    public static class JsonElementExtensions
    {
        public static string? GetPropertyOrNull(this JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        public static decimal? GetDecimalOrNull(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result))
                    return result;

                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }

        public static bool? GetBoolOrNull(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.True) return true;
                if (value.ValueKind == JsonValueKind.False) return false;

                if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }

        public static DateTime? ParseDateOrNull(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }

        public static int? GetIntOrNull(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
                    return result;

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }
    }
}
