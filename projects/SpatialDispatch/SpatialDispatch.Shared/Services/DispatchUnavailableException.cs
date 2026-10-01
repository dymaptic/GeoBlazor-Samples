namespace SpatialDispatch.Shared.Services;

/// <summary>
///     Thrown when the data cannot produce a dispatch answer: no such job, no territory covering it, or
///     nobody available in the territory that does.
/// </summary>
/// <remarks>
///     This exists so the API can distinguish "the data says no" from "the query is broken." Both arrive as
///     an <see cref="InvalidOperationException" /> otherwise, and during development an untranslatable LINQ
///     expression was reported to the dashboard as a missing territory — a bug wearing a data state's
///     clothes, which is the last thing anyone wants to debug on stage.
/// </remarks>
public sealed class DispatchUnavailableException(string message) : InvalidOperationException(message);
