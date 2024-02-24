namespace NationFinder.Client;

public record CommunicationResult(bool Success, string? ErrorMessage = null);

public record LayerState(bool ShowScene, bool ShowNight, bool ShowBorders,
    bool ShowWeather, bool ShowTopography);
