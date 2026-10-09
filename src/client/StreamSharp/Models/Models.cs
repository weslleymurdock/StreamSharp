using System.Text.Json.Serialization;

namespace StreamSharp.Models;

public class HealthCheckEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("durationMs")]
    public double DurationMs { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalDurationMs")]
    public double TotalDurationMs { get; set; }

    [JsonPropertyName("checks")]
    public IEnumerable<HealthCheckEntry> Checks { get; set; } = Enumerable.Empty<HealthCheckEntry>();

    [JsonIgnore]
    public bool IsHealthy => string.Equals(Status, "Healthy", StringComparison.OrdinalIgnoreCase);
}
