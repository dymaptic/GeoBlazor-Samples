using System.Text.Json.Serialization;

namespace SpatialDispatch.Shared.Contracts;

/// <remarks>
///     Serialized by name so the API response is readable on a slide, and so it matches the text the
///     database stores in the Priority column.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<JobPriority>))]
public enum JobPriority
{
    Routine,
    Urgent
}

[JsonConverter(typeof(JsonStringEnumConverter<JobStatus>))]
public enum JobStatus
{
    Open,
    Assigned
}

/// <summary>
///     An open service job as the dispatcher sees it. Geocoding happens before this workflow, so the
///     customer location is already a stored coordinate.
/// </summary>
public sealed record Job(
    int Id,
    string Customer,
    string Summary,
    JobPriority Priority,
    string MapAnchor,
    GeoPoint Location);
