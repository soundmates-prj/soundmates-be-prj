using System.Text.Json;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Reusable helper for extracting properties from JSON events
    /// </summary>
    public static class EventPropertyExtractor
    {
        public static Guid GetGuidProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && 
                    prop.ValueKind == JsonValueKind.String && 
                    Guid.TryParse(prop.GetString(), out var guid))
                {
                    return guid;
                }
            }
            throw new JsonException($"Required Guid property not found. Tried: {string.Join(", ", propertyNames)}");
        }

        public static Guid? GetNullableGuidProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var guid))
                        return guid;
                    if (prop.ValueKind == JsonValueKind.Null)
                        return null;
                }
            }
            return null;
        }

        public static string GetStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? string.Empty;
                }
            }
            throw new JsonException($"Required string property not found. Tried: {string.Join(", ", propertyNames)}");
        }

        public static string? GetOptionalStringProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }
            return null;
        }

        public static bool GetBooleanProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.True) return true;
                    if (prop.ValueKind == JsonValueKind.False) return false;
                }
            }
            return false;
        }

        public static DateTime? GetDateTimeProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var str = prop.GetString();
                        if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, out var dt))
                            return dt;
                    }
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        // Unix timestamp (seconds)
                        var unix = prop.GetInt64();
                        return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
                    }
                    else if (prop.ValueKind == JsonValueKind.Null)
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        public static JsonElement GetDataElement(JsonElement root)
        {
            if (root.TryGetProperty("data", out var dataElement)) return dataElement;
            if (root.TryGetProperty("user", out var userElement)) return userElement;
            if (root.TryGetProperty("Data", out var dataElement2)) return dataElement2;
            if (root.TryGetProperty("User", out var userElement2)) return userElement2;
            return root;
        }

        public static (string? firstName, string? lastName) ParseName(JsonElement element)
        {
            var firstName = GetOptionalStringProperty(element, "firstName", "FirstName", "first_name");
            var lastName = GetOptionalStringProperty(element, "lastName", "LastName", "last_name");

            // Backward compatibility: split fullName if firstName/lastName not found
            if (string.IsNullOrEmpty(firstName) && 
                element.TryGetProperty("fullName", out var fullNameProp) && 
                fullNameProp.ValueKind == JsonValueKind.String)
            {
                var fullName = fullNameProp.GetString();
                if (!string.IsNullOrEmpty(fullName))
                {
                    var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    firstName = parts.Length > 0 ? parts[0] : string.Empty;
                    lastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }

            return (firstName, lastName);
        }
    }
}
