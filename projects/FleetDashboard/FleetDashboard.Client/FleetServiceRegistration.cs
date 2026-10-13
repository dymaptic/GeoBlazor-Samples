using FleetDashboard.Client.Streaming;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetDashboard.Client;

public static class FleetServiceRegistration
{
    /// <summary>The configuration key and value that select the in-browser simulator.</summary>
    public const string SourceKey = "Fleet:Source";
    public const string BrowserSource = "Browser";

    /// <summary>Registers the frame source the dashboard reads. By default that is the SignalR hub on the
    /// host that served the page. With <c>Fleet:Source</c> set to <c>Browser</c> it is the in-browser
    /// simulator instead, and no hub connection is registered, so the client can be hosted statically.
    /// Requires a <see cref="TimeProvider"/> in the container.</summary>
    public static IServiceCollection AddFleetFrameSource(this IServiceCollection services,
        IConfiguration configuration, Uri baseAddress)
    {
        if (string.Equals(configuration[SourceKey], BrowserSource, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFleetFrameSource, BrowserFleetFrameSource>();
            return services;
        }

        services.AddScoped(sp => new HubConnectionBuilder()
            .WithUrl(new Uri(baseAddress, "fleet"))
            // A visible first reconnect delay: the default zero-delay attempt flashes past too fast
            // for the audience to see the "Reconnecting" state before recovery.
            .WithAutomaticReconnect([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build());
        services.AddScoped<IFleetHubConnection>(sp =>
            new HubConnectionAdapter(sp.GetRequiredService<HubConnection>()));
        services.AddScoped<IFleetFrameSource, SignalRFleetFrameSource>();
        return services;
    }
}
