using dymaptic.GeoBlazor.Sample.TokenRefresh.Client.Models;


namespace dymaptic.GeoBlazor.Sample.TokenRefresh.Client.Services;

/// <summary>
///     Shared interface allows injecting different implementations for WebAssembly and Server components.
/// </summary>
public interface IAuthService
{
    Task<TokenResponse> GetTokenAsync(bool forceRefresh);
}